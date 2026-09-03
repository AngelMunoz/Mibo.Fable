namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Per-element adaptive set node (docs/2026-08-05-MAPA-DESIGN.md): web port of
// ElementSetNode.fs.
//
// ASet.mapA / chooseA / filterA share one node: the mapping returns an
// adaptive value per element, cached by input element. Structural source
// changes go through the journal (existing pattern, cache maintained per op);
// element-aval changes go through a version scan of the cache, gated on the
// write generation: a scan runs only when a write landed since the last one.

/// <summary>
/// Maps every element of a set to an adaptive value (or chooses/filters, when
/// the aval's value is <c>ValueNone</c> to drop the element). Duplicate
/// output values share one reference count (refcounted set).
/// </summary>
type ElementSetNode<'T, 'U when 'T: equality and 'U: equality>
  (source: IAdaptiveSet<'T>, mapping: 'T -> aval<'U voption>) =
  let mutable state = SetNodeState.create 1
  let mutable cache = Dictionary<'T, ElementEntry<'U>>()
  let mutable initialized = false
  let mutable disposed = false
  // Self-healing flag: set when a reentrant write lands mid-scan, so the
  // next read rescans even when the generation did not move since.
  let mutable elementDirty = false
  // Write generation at the last scan. The generation gate fires when it
  // moved since.
  let mutable lastDrainWriteGen = -1L

  member private this.Register() =
    match Collections.trySetSinkRegistry(box source) with
    | ValueSome r -> r.AddSetSink(box(this :> ISetDeltaSink<'T>))
    | ValueNone -> ()

  member private this.Unregister() =
    match Collections.trySetSinkRegistry(box source) with
    | ValueSome r -> r.RemoveSetSink(box(this :> ISetDeltaSink<'T>))
    | ValueNone -> ()

  /// The dirty gate: a scan is needed when the self-healing flag is set, or
  /// when a write moved the generation since the last scan.
  member private _.NeedsElementScan() =
    elementDirty || GraphContext.Default.WriteGeneration <> lastDrainWriteGen

  member private this.EnsureInitialized() =
    if not initialized then
      // Snapshot first, register between, then load: the mapping is user
      // code that may write to the source (the transient view must not
      // be iterated while it is mutated), and the write must land in
      // the journal (register before the mapping runs). The flag is set
      // last: an exception leaves the node uninitialized.
      let snapshot = HashSet<'T>(source.GetValue())
      this.Register()

      for item in snapshot do
        let aval = mapping item
        // Version read BEFORE the force: a mid-force write leaves the
        // stored version stale, so the next scan re-forces.
        let preV = aval.Version
        let newV = aval.GetValue()
        let entry = ElementEntry.create aval preV newV

        match newV with
        | ValueSome u -> state.Set.Add u |> ignore
        | ValueNone -> ()

        cache[item] <- entry

      state.DepVersions[0] <- Collections.committedVersion source
      initialized <- true

  /// Apply the source journal with cache maintenance: removed elements drop
  /// their cache entry (unregistered) and their contribution; added elements
  /// run the mapping, force the aval, and contribute.
  member private this.DrainJournal() =
    let rems = state.Journal.Rems
    let adds = state.Journal.Adds
    let remStart = rems.Count
    let addStart = adds.Count
    let mutable i = 0
    // Suspend the append-time cross-kind cancellation while the journal
    // is replayed: a removal could shift entries under the captured
    // indexes and double-process them (see journalAppendSet).
    state.Journal.InDrain[0] <- 1
    // Consumed counts: entries before these positions were applied and
    // must never be applied again; the throwing entry (and reentrant
    // entries appended live, beyond the start bounds) survive.
    let mutable remsDone = 0
    let mutable addsDone = 0

    try
      while i < remStart do
        let x = rems.Items[i]
        let mutable entry = Unchecked.defaultof<ElementEntry<'U>>

        if cache.TryGetValue(x, &entry) then
          cache.Remove x |> ignore

          match entry.Last with
          | ValueSome old ->
            if state.Set.Remove old then
              state.Out.Rems.Append old
          | ValueNone -> ()

        i <- i + 1
        remsDone <- i

      i <- 0

      while i < addStart do
        let x = adds.Items[i]
        let aval = mapping x
        // Version read BEFORE the force (see EnsureInitialized).
        let preV = aval.Version
        let newV = aval.GetValue()
        let entry = ElementEntry.create aval preV newV

        match newV with
        | ValueSome u ->
          if state.Set.Add u then
            state.Out.Adds.Append u
        | ValueNone -> ()

        cache[x] <- entry
        i <- i + 1
        addsDone <- i
    finally
      state.Journal.InDrain[0] <- 0
      // Compact against the LIVE journal counts: reentrant writes during
      // the mapping append to the live journal (a stale copy would drop
      // them). Consumed entries are dropped; the throwing entry survives.
      state.Journal.Rems.Compact(remsDone)
      state.Journal.Adds.Compact(addsDone)

  /// The element scan: re-read every cached aval's version; force the
  /// changed ones and apply the contribution change through the refcounted
  /// set. Self-healing: a reentrant write mid-scan keeps the dirty flag set
  /// so the next read rescans.
  member private this.ScanElements() =
    let startGen = GraphContext.Default.WriteGeneration
    let mutable changed = false
    let mutable e = cache.GetEnumerator()

    while e.MoveNext() do
      let kvp = e.Current
      let entry = kvp.Value
      // Version read BEFORE the force (see EnsureInitialized).
      let preV = entry.Aval.Version

      if preV <> entry.Version then
        let newV = entry.Aval.GetValue()

        match entry.Last with
        | ValueSome old ->
          match newV with
          | ValueSome newU ->
            if EqualityComparer<'U>.Default.Equals(old, newU) then
              () // version bumped to an equal value: no delta
            else
              if state.Set.Remove old then
                state.Out.Rems.Append old

              if state.Set.Add newU then
                state.Out.Adds.Append newU

              changed <- true
          | ValueNone ->
            if state.Set.Remove old then
              state.Out.Rems.Append old

            changed <- true
        | ValueNone ->
          match newV with
          | ValueSome newU ->
            if state.Set.Add newU then
              state.Out.Adds.Append newU

            changed <- true
          | ValueNone -> ()

        entry.Version <- preV
        entry.Last <- newV

    if changed then
      state.Version <- state.Version + 1L

    if GraphContext.Default.WriteGeneration <> startGen then
      // A reentrant write landed mid-scan: the output may be stale, keep
      // the flag so the next read rescans.
      elementDirty <- true
    else
      elementDirty <- false

  /// Drain the journal, then the element scan, then push the accumulated
  /// output delta once.
  member private this.Process() =
    let ctx = GraphContext.Default
    let wasActive = ctx.TxActive
    ctx.TxActive <- true

    try
      if not state.Journal.IsEmpty then
        this.DrainJournal()

      if this.NeedsElementScan() then
        this.ScanElements()

      if not state.Out.IsEmpty then
        state.Version <- state.Version + 1L
        Collections.pushSetDelta state.Sinks state.Out
        state.Out.Clear()
    finally
      ctx.TxActive <- wasActive

    // Capture AFTER the push: the downstream sink's write notification
    // advances the write generation during the delta delivery. Capturing
    // in the scan (before the push) left the gate permanently open and
    // the Version dirty indicator inflated, so a version-gated consumer
    // recorded a phantom version and missed the next change.
    lastDrainWriteGen <- GraphContext.Default.WriteGeneration

  interface ICommittedVersion with
    member this.CommittedVersion = state.Version

  interface ISetDeltaSink<'T> with
    member this.OnDeltas(adds: 'T[], addCount: int, rems: 'T[], remCount: int) =
      if not disposed then
        Collections.journalAppendSet state.Journal adds addCount rems remCount
        state.Version <- state.Version + 1L
        GraphContext.Default.BumpWriteGeneration()

  interface IAdaptiveSet<'U> with
    member this.GetValue() =
      let ctx = GraphContext.Default
      ctx.ClaimOwner()

      try
        if disposed then
          invalidOp "This adaptive set has been disposed."

        this.EnsureInitialized()

        if source.Version <> state.DepVersions[0] then
          source.GetValue() |> ignore
          state.DepVersions[0] <- Collections.committedVersion source

        this.Process()
        AdaptiveRuntime.addDependency (this :> IAdaptiveObject) state.Version
        state.Set.Data :> IReadOnlySet<'U>
      finally
        ctx.ReleaseOwner()

    member this.Version =
      // Dirty indicator for version-checking readers: a pending element
      // scan is reported as a version bump (the scan itself advances the
      // version only when the output changes).
      if this.NeedsElementScan() then
        state.Version + 1L
      else
        state.Version

  interface IDisposable with
    member this.Dispose() =
      if not disposed then
        disposed <- true
        this.Unregister()
        Collections.clearSinks state.Sinks

  interface ISetSinkRegistry with
    member this.AddSetSink(sink) = Collections.addSink state.Sinks sink

    member this.RemoveSetSink(sink) = Collections.removeSink state.Sinks sink
