namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic
open Fable.Core

// =============================================================================
// Web port of Mibo.Adaptive's Core/Collections/Shared.fs.
//
// Representation adaptations forced by the JS runtime (mechanisms unchanged):
//  * the .NET struct state holders become mutable-field records — Fable drops
//    struct-field mutations — and the byref-into-field helpers mutate the
//    shared instances instead; "the drain works on a struct copy" becomes an
//    explicit (buffer reference, count) capture, which shares the same
//    semantics (appends during a drain land past the captured count);
//  * interface type tests are always false on JS, so the two lookup protocols
//    (committed versions, sink registries) resolve through module-level WeakMap
//    markers that implementors register at construction;
//  * System.WeakReference is not supported — sinks ride a JS WeakRef;
//  * Memory views become transient (array, count) pairs.
// =============================================================================

// =============================================================================
// Collection contracts (PLAN.md Section 6.9)
// =============================================================================

/// <summary>
/// An adaptive set: either a changeable source or a derived node.
/// </summary>
/// <remarks>
/// <para>
/// <c>GetValue</c> returns a transient view of the internal state. The view is
/// valid only until the next write. Computations consume it; they must not
/// retain it or mutate it. <c>ASet.force</c> materializes an immutable copy
/// that is safe to retain; the library never touches a forced value again.
/// </para>
/// <para>
/// Derived sets are disposable: disposal unregisters the node from its
/// dependencies and stops all delta processing. Disposing a changeable source
/// is a no-op; sources are owned by the application. Dispose derived nodes
/// before their consumers. Reading a disposed node throws.
/// </para>
/// </remarks>
type IAdaptiveSet<'T> =
  inherit IAdaptiveObject
  inherit IDisposable
  abstract member GetValue: unit -> IReadOnlySet<'T>

/// <summary>An abbreviation for <see cref="IAdaptiveSet&lt;'T&gt;"/> (FDA <c>aset&lt;'T&gt;</c> parity).</summary>
type aset<'T> = IAdaptiveSet<'T>

/// <summary>
/// An adaptive map: either a changeable source or a derived node. See
/// <see cref="IAdaptiveSet&lt;'T&gt;"/> for the view and disposal contracts.
/// </summary>
type IAdaptiveMap<'K, 'V when 'K: equality> =
  inherit IAdaptiveObject
  inherit IDisposable
  abstract member GetValue: unit -> IReadOnlyDictionary<'K, 'V>

/// <summary>An abbreviation for <see cref="IAdaptiveMap&lt;'K,'V&gt;"/> (FDA <c>amap&lt;'K,'V&gt;</c> parity).</summary>
type amap<'K, 'V when 'K: equality> = IAdaptiveMap<'K, 'V>

/// <summary>
/// Internal. Receives deltas from a set dependency. The implementation appends
/// the delta to its journal; processing happens on the next read (drain).
/// </summary>
type internal ISetDeltaSink<'T> =
  abstract member OnDeltas:
    added: 'T[] * addedCount: int * removed: 'T[] * removedCount: int -> unit

/// <summary>
/// Internal. Receives deltas from a map dependency. The implementation appends
/// the delta to its journal; processing happens on the next read (drain).
/// </summary>
type internal IMapDeltaSink<'K, 'V> =
  abstract member OnDeltas:
    setEntries: struct ('K * 'V)[] *
    setCount: int *
    removedKeys: 'K[] *
    removedCount: int ->
      unit

/// <summary>Internal. Register/unregister a set delta sink with a dependency.</summary>
type internal ISetSinkRegistry =
  abstract member AddSetSink: sink: obj -> unit
  abstract member RemoveSetSink: sink: obj -> unit

/// <summary>Internal. Register/unregister a map delta sink with a dependency.</summary>
type internal IMapSinkRegistry =
  abstract member AddMapSink: sink: obj -> unit
  abstract member RemoveMapSink: sink: obj -> unit

/// <summary>
/// An adaptive list: either a changeable source or a derived node. See
/// <see cref="IAdaptiveSet&lt;'T&gt;"/> for the view and disposal contracts.
/// Positions in list operations are 0-based and refer to the state as of the
/// previous operation in the same delta; deltas are applied in order.
/// </summary>
type IAdaptiveList<'T> =
  inherit IAdaptiveObject
  inherit IDisposable
  abstract member GetValue: unit -> IReadOnlyList<'T>

/// <summary>An abbreviation for <see cref="IAdaptiveList&lt;'T&gt;"/> (FDA <c>alist&lt;'T&gt;</c> parity).</summary>
type alist<'T> = IAdaptiveList<'T>

// =============================================================================
// State holders
//
// Mutable-field records: the original's struct holders with byref-into-field
// mutation become shared mutable objects, so field writes are visible to
// every holder — the same observable behavior as the original.
// =============================================================================

/// <summary>Internal. Receives deltas from a list dependency.</summary>
type internal IListDeltaSink<'T> =
  abstract member OnDeltas: ops: ListOp<'T>[] * opCount: int -> unit

/// <summary>Internal. Register/unregister a list delta sink with a dependency.</summary>
type internal IListSinkRegistry =
  abstract member AddListSink: sink: obj -> unit
  abstract member RemoveListSink: sink: obj -> unit

/// <summary>
/// The binary set operation of the two-source set node: difference (left
/// minus right), intersection, or symmetric difference.
/// </summary>
type TwoSetOp =
  | Difference
  | Intersect
  | Xor

module internal Collections =

  /// Grow an array to hold at least n items. Amortized O(1); array growth
  /// only. The JS-safe stand-in for the original's byref ensureCapacity:
  /// call sites rebind the (possibly new) array.
  let ensureArrayCapacity (arr: 'a[]) (n: int) : 'a[] =
    if arr.Length < n then
      let next = Array.zeroCreate(max n (arr.Length * 2))
      Array.blit arr 0 next 0 arr.Length
      next
    else
      arr

  /// Dequeue one item, or none when empty (the PostRing.TryDequeue shape
  /// the posted-batch loops replay through).
  let tryDequeue<'P>(q: Queue<'P>) : 'P voption =
    if q.Count > 0 then ValueSome(q.Dequeue()) else ValueNone

  /// The settled counter of a dependency: its committed version when the
  /// node is a version-inflating node (the JS-safe stand-in for the
  /// original's :? ICommittedVersion test), its plain Version otherwise
  /// (plain sources never inflate).
  let committedVersion(dep: IAdaptiveObject) : int64 =
    if
      WeakMarkers.committedVersions.has dep
      || WeakMarkers.hasCommittedVersion dep
    then
      (unbox<ICommittedVersion> dep).CommittedVersion
    else
      dep.Version

  /// The set delta sink registry of a dependency, when it is one
  /// (the JS-safe stand-in for the original's :? ISetSinkRegistry test).
  let trySetSinkRegistry(dep: obj) : ISetSinkRegistry voption =
    if WeakMarkers.setRegistries.has dep || WeakMarkers.hasSetRegistry dep then
      ValueSome(unbox<ISetSinkRegistry> dep)
    else
      ValueNone

  /// The map delta sink registry of a dependency (see trySetSinkRegistry).
  let tryMapSinkRegistry(dep: obj) : IMapSinkRegistry voption =
    if WeakMarkers.mapRegistries.has dep || WeakMarkers.hasMapRegistry dep then
      ValueSome(unbox<IMapSinkRegistry> dep)
    else
      ValueNone

  /// The list delta sink registry of a dependency (see trySetSinkRegistry).
  let tryListSinkRegistry(dep: obj) : IListSinkRegistry voption =
    if
      WeakMarkers.listRegistries.has dep || WeakMarkers.hasListRegistry dep
    then
      ValueSome(unbox<IListSinkRegistry> dep)
    else
      ValueNone

  /// Cancel cross-kind duplicates between a journal buffer and an incoming
  /// batch: an incoming entry with a pending opposite-kind entry of the
  /// same element drops BOTH (the element never left the derived state, so
  /// replaying the survivor alone would double its refcount and
  /// double-count a reduction). The pending buffer is compacted in place
  /// and the surviving incoming entries are appended to
  /// <paramref name="target" />. Linear for small products (zero
  /// allocation); hash sets above the threshold. The caller guarantees no
  /// drain is in flight (the InDrain flag).
  let cancelCrossKind<'T when 'T: equality>
    (pending: DeltaBuffer<'T>)
    (incoming: 'T[])
    (incomingCount: int)
    (target: DeltaBuffer<'T>)
    : unit =
    if incomingCount > 0 then
      if pending.Count = 0 then
        target.AppendRange(incoming, incomingCount)
      elif int64 incomingCount * int64 pending.Count <= 4096L then
        let comparer = EqualityComparer<'T>.Default
        target.EnsureCapacity(target.Count + incomingCount)
        let mutable tw = target.Count

        for i in 0 .. incomingCount - 1 do
          let x = incoming[i]
          let mutable found = -1
          let mutable j = 0

          while found < 0 && j < pending.Count do
            if comparer.Equals(pending.Items[j], x) then
              found <- j

            j <- j + 1

          if found >= 0 then
            for k in found .. pending.Count - 2 do
              pending.Items[k] <- pending.Items[k + 1]

            pending.Count <- pending.Count - 1
          else
            target.Items[tw] <- x
            tw <- tw + 1

        target.Count <- tw
      else
        let pendingKeys = HashSet<'T>()
        let incomingKeys = HashSet<'T>()

        for j in 0 .. pending.Count - 1 do
          pendingKeys.Add(pending.Items[j]) |> ignore

        for i in 0 .. incomingCount - 1 do
          incomingKeys.Add(incoming[i]) |> ignore

        let mutable w = 0

        for j in 0 .. pending.Count - 1 do
          let item = pending.Items[j]

          if not(incomingKeys.Contains item) then
            pending.Items[w] <- item
            w <- w + 1

        pending.Count <- w
        target.EnsureCapacity(target.Count + incomingCount)
        let mutable tw = target.Count

        for i in 0 .. incomingCount - 1 do
          let x = incoming[i]

          if not(pendingKeys.Contains x) then
            target.Items[tw] <- x
            tw <- tw + 1

        target.Count <- tw

  /// Append a delta to a set journal (called at write time by the pusher).
  /// Cross-kind duplicates are cancelled first (an add cancels a pending
  /// rem of the same element and vice versa; arrival order = append order):
  /// without this the journal can hold [add x, rem x] across two deliveries
  /// with no drain between, and the drain replays rems before adds
  /// regardless of arrival order, leaving x present although the source
  /// removed it. Suspended while a drain is in flight (the InDrain flag).
  let journalAppendSet<'T when 'T: equality>
    (journal: SetDelta<'T>)
    (adds: 'T[])
    (addCount: int)
    (rems: 'T[])
    (remCount: int)
    : unit =
    if journal.InDrain[0] = 0 then
      cancelCrossKind journal.Rems adds addCount journal.Adds
      cancelCrossKind journal.Adds rems remCount journal.Rems
    else
      journal.Adds.AppendRange(adds, addCount)
      journal.Rems.AppendRange(rems, remCount)

  /// Append a delta to a map journal (called at write time by the pusher).
  /// Cross-kind duplicates are coalesced first (last op wins: an incoming
  /// set supersedes a pending rem of the same key and vice versa). See
  /// journalAppendSet for the divergence this prevents and the InDrain
  /// suspension rule.
  let journalAppendMap<'K, 'V when 'K: equality>
    (journal: MapDelta<'K, 'V>)
    (sets: struct ('K * 'V)[])
    (setCount: int)
    (rems: 'K[])
    (remCount: int)
    : unit =
    if journal.InDrain[0] = 0 then
      journal.Rems.RemoveKeys(id, (fun (struct (k, _)) -> k), sets, setCount)
      journal.Sets.RemoveKeys((fun (struct (k, _)) -> k), id, rems, remCount)

    journal.Sets.AppendRange(sets, setCount)
    journal.Rems.AppendRange(rems, remCount)

  /// Append a delta to a list journal (called at write time by the pusher).
  let inline journalAppendList
    (journal: ListDelta<'T>)
    (ops: ListOp<'T>[])
    (opCount: int)
    : unit =
    journal.Ops.AppendRange(ops, opCount)

  /// Drop dead sink entries (their node was collected). Runs at the start of
  /// every delivery and on registration: swap-pop is safe here because no
  /// user code can interleave. Amortized O(1) per dead entry, zero
  /// allocation.
  let compactDeadSinks(sinks: SinkList) : unit =
    let mutable i = 0

    while i < sinks.Count do
      if isNull(WeakRefs.deref sinks.Sinks[i]) then
        sinks.Sinks[i] <- sinks.Sinks[sinks.Count - 1]
        sinks.Sinks[sinks.Count - 1] <- null
        sinks.Count <- sinks.Count - 1
      else
        i <- i + 1

  /// Register a sink (weakly). Dead entries are swept on registration so
  /// the list does not accumulate between deliveries.
  let addSink (sinks: SinkList) (sink: obj) : unit =
    compactDeadSinks sinks

    if sinks.Count = sinks.Sinks.Length then
      let next = Array.zeroCreate(sinks.Sinks.Length * 2)
      Array.blit sinks.Sinks 0 next 0 sinks.Sinks.Length
      sinks.Sinks <- next

    sinks.Sinks[sinks.Count] <- WeakRefs.create sink
    sinks.Count <- sinks.Count + 1

  /// Remove a sink by identity (matches the weak entry's target).
  let removeSink (sinks: SinkList) (sink: obj) : unit =
    let mutable found = -1
    let mutable i = 0

    while found < 0 && i < sinks.Count do
      if obj.ReferenceEquals(WeakRefs.deref sinks.Sinks[i], sink) then
        found <- i
      else
        i <- i + 1

    if found >= 0 then
      sinks.Count <- sinks.Count - 1

      for j in found .. sinks.Count - 1 do
        sinks.Sinks[j] <- sinks.Sinks[j + 1]

      sinks.Sinks[sinks.Count] <- null

  /// Drop all sinks (disposal): releases the downstream references.
  let clearSinks(sinks: SinkList) : unit =
    Arrays.clearRange sinks.Sinks 0 sinks.Count
    sinks.Count <- 0

  /// Push a set delta to every registered sink. The batch delivers only to
  /// the sinks registered at the start (bound captured): a sink registered
  /// reentrantly during delivery is not delivered, because its init snapshot
  /// already reflects the change (register between snapshot and load) and
  /// delivering would double-apply. Dead entries are compacted before the
  /// loop; an entry that dies mid-delivery (a GC inside user code) is
  /// skipped and compacted by the next delivery. The resolved target is
  /// rooted by the local, so a mid-delivery GC cannot collect it.
  let pushSetDelta<'T> (sinks: SinkList) (delta: SetDelta<'T>) : unit =
    if not delta.IsEmpty then
      compactDeadSinks sinks
      let adds = delta.Adds.Items
      let addCount = delta.Adds.Count
      let rems = delta.Rems.Items
      let remCount = delta.Rems.Count
      let bound = sinks.Count
      let mutable i = 0

      while i < bound do
        let target = WeakRefs.deref sinks.Sinks[i]

        if not(isNull target) then
          (unbox<ISetDeltaSink<'T>> target)
            .OnDeltas(adds, addCount, rems, remCount)

        i <- i + 1

  /// Push a map delta to every registered sink. See pushSetDelta for the
  /// dead-entry and reentrancy handling.
  let pushMapDelta<'K, 'V> (sinks: SinkList) (delta: MapDelta<'K, 'V>) : unit =
    if not delta.IsEmpty then
      compactDeadSinks sinks
      let sets = delta.Sets.Items
      let setCount = delta.Sets.Count
      let rems = delta.Rems.Items
      let remCount = delta.Rems.Count
      let bound = sinks.Count
      let mutable i = 0

      while i < bound do
        let target = WeakRefs.deref sinks.Sinks[i]

        if not(isNull target) then
          (unbox<IMapDeltaSink<'K, 'V>> target)
            .OnDeltas(sets, setCount, rems, remCount)

        i <- i + 1

  /// Push a set delta to every registered sink and record the write: the
  /// write generation advances, so version-gated readers (the *A gates, the
  /// scalar dirty caches) re-check on their next read.
  let inline pushAndBumpSet
    (ctx: GraphContext)
    (delta: SetDelta<'T>)
    (sinks: SinkList)
    : unit =
    ctx.BumpWriteGeneration()
    pushSetDelta sinks delta

  /// Push a map delta to every registered sink and record the write (see
  /// pushAndBumpSet).
  let inline pushAndBumpMap
    (ctx: GraphContext)
    (delta: MapDelta<'K, 'V>)
    (sinks: SinkList)
    : unit =
    ctx.BumpWriteGeneration()
    pushMapDelta sinks delta

  /// Push a list delta to every registered sink. See pushSetDelta for the
  /// dead-entry and reentrancy handling.
  let pushListDelta<'T> (sinks: SinkList) (delta: ListDelta<'T>) : unit =
    compactDeadSinks sinks
    let ops = delta.Ops.Items
    let opCount = delta.Ops.Count
    let bound = sinks.Count
    let mutable i = 0

    while i < bound do
      let target = WeakRefs.deref sinks.Sinks[i]

      if not(isNull target) then
        (unbox<IListDeltaSink<'T>> target).OnDeltas(ops, opCount)

      i <- i + 1

  /// Push a list delta to every registered sink and record the write (see
  /// pushAndBumpSet).
  let inline pushAndBumpList
    (ctx: GraphContext)
    (delta: ListDelta<'T>)
    (sinks: SinkList)
    : unit =
    ctx.BumpWriteGeneration()
    pushListDelta sinks delta

  /// Drain the journal of a set node with refcounts (map over set, union):
  /// apply each pending delta to the state and collect the reduced output
  /// delta. Entries appended during processing (reentrant writes) survive.
  /// Returns whether the state changed. The drains capture the journal
  /// buffer references and counts (the "struct copy" of the original), so
  /// appends that land during processing are not replayed here.
  let inline drainRefSet
    (map: 'T -> 'U voption)
    (state: SetNodeState<'T, 'U>)
    : bool =
    let changed = ref false
    let rems = state.Journal.Rems
    let adds = state.Journal.Adds
    let remStart = rems.Count
    let addStart = adds.Count
    // Consumed counts: entries before these positions were applied and must
    // never be applied again; the entry that threw (and reentrant entries)
    // survive for the next drain.
    let mutable remsDone = 0
    let mutable addsDone = 0

    try
      let mutable i = 0

      while i < remStart do
        let x = rems.Items[i]

        match map x with
        | ValueSome y ->
          if state.Set.Remove y then
            state.Out.Rems.Append y
            changed.Value <- true
        | ValueNone -> ()

        i <- i + 1
        remsDone <- i

      let mutable j = 0

      while j < addStart do
        let y = adds.Items[j]

        match map y with
        | ValueSome z ->
          if state.Set.Add z then
            state.Out.Adds.Append z
            changed.Value <- true
        | ValueNone -> ()

        j <- j + 1
        addsDone <- j
    finally
      // Compact in the finally so a throwing mapping cannot make the next
      // drain re-apply consumed entries (double-apply corrupts refcounts).
      state.Journal.Rems.Compact(remsDone)
      state.Journal.Adds.Compact(addsDone)

    changed.Value

  /// Drain the journal of a set node without refcounts (filter): plain
  /// membership. Returns whether the state changed.
  let inline drainPlainSet
    (map: 'T -> 'T voption)
    (state: SetNodeState<'T, 'T>)
    : bool =
    let changed = ref false
    let rems = state.Journal.Rems
    let adds = state.Journal.Adds
    let remStart = rems.Count
    let addStart = adds.Count
    let mutable remsDone = 0
    let mutable addsDone = 0

    try
      let mutable i = 0

      while i < remStart do
        let x = rems.Items[i]

        if state.Set.Data.Remove x then
          state.Out.Rems.Append x
          changed.Value <- true

        i <- i + 1
        remsDone <- i

      let mutable j = 0

      while j < addStart do
        let x = adds.Items[j]

        match map x with
        | ValueSome z ->
          if state.Set.Data.Add z then
            state.Out.Adds.Append z
            changed.Value <- true
        | ValueNone -> ()

        j <- j + 1
        addsDone <- j
    finally
      // Compact in the finally so a throwing predicate cannot make the
      // next drain re-apply consumed entries.
      state.Journal.Rems.Compact(remsDone)
      state.Journal.Adds.Compact(addsDone)

    changed.Value

  /// Drain the journal of a map node: apply each pending delta to the state
  /// and collect the reduced output delta. The lambda returns ValueNone for
  /// elements to drop (filter). Returns whether the state changed.
  let inline drainMap
    (map: 'K -> 'V -> 'U voption)
    (state: MapNodeState<'K, 'V, 'U>)
    : bool =
    let changed = ref false
    let rems = state.Journal.Rems
    let sets = state.Journal.Sets
    let remStart = rems.Count
    let setStart = sets.Count
    let mutable remsDone = 0
    let mutable setsDone = 0

    try
      let mutable i = 0

      while i < remStart do
        let k = rems.Items[i]

        if state.Data.Remove k then
          state.Out.Rems.Append k
          changed.Value <- true

        i <- i + 1
        remsDone <- i

      let mutable j = 0

      while j < setStart do
        let struct (k, v) = sets.Items[j]

        match map k v with
        | ValueSome u ->
          let mutable old = Unchecked.defaultof<'U>

          if
            state.Data.TryGetValue(k, &old)
            && EqualityComparer<'U>.Default.Equals(old, u)
          then
            ()
          else
            state.Data[k] <- u
            state.Out.Sets.Append(struct (k, u))
            changed.Value <- true
        | ValueNone ->
          if state.Data.Remove k then
            state.Out.Rems.Append k
            changed.Value <- true

        j <- j + 1
        setsDone <- j
    finally
      // Compact in the finally so a throwing mapping cannot make the next
      // drain re-apply consumed entries.
      state.Journal.Rems.Compact(remsDone)
      state.Journal.Sets.Compact(setsDone)

    changed.Value

  /// Drain a set node and push the reduced output delta to its sinks.
  let inline drainSetPush
    (map: 'T -> 'U voption)
    (state: SetNodeState<'T, 'U>)
    : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true
    // Suspend the append-time coalescing while the journal is replayed:
    // the drain captures the journal buffers, and a removal from the
    // journal could shift entries under the captured counts.
    state.Journal.InDrain[0] <- 1

    try
      let changed = drainRefSet map state

      if changed then
        pushSetDelta state.Sinks state.Out
        state.Out.Clear()
    finally
      state.Journal.InDrain[0] <- 0
      ctx.TxActive <- wasActive

  /// Drain a plain set node (filter) and push the reduced output delta.
  let inline drainPlainSetPush
    (map: 'T -> 'T voption)
    (state: SetNodeState<'T, 'T>)
    : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true
    state.Journal.InDrain[0] <- 1

    try
      let changed = drainPlainSet map state

      if changed then
        pushSetDelta state.Sinks state.Out
        state.Out.Clear()
    finally
      state.Journal.InDrain[0] <- 0
      ctx.TxActive <- wasActive

  /// Drain a map node and push the reduced output delta to its sinks.
  let inline drainMapPush
    (map: 'K -> 'V -> 'U voption)
    (state: MapNodeState<'K, 'V, 'U>)
    : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true
    state.Journal.InDrain[0] <- 1

    try
      let changed = drainMap map state

      if changed then
        pushMapDelta state.Sinks state.Out
        state.Out.Clear()
    finally
      state.Journal.InDrain[0] <- 0
      ctx.TxActive <- wasActive

  /// Initial load of a refcounted set node: build the internal state from a
  /// snapshot of the source view. The node takes the snapshot and registers
  /// its sink between the snapshot and this call, so user-code writes from
  /// the mapping land in the journal instead of mutating the transient view
  /// mid-iteration.
  let inline loadRefSet
    (map: 'T -> 'U voption)
    (snapshot: HashSet<'T>)
    (state: SetNodeState<'T, 'U>)
    : unit =
    for item in snapshot do
      match map item with
      | ValueSome z -> state.Set.Add z |> ignore
      | ValueNone -> ()

  /// Initial load of a plain set node (filter). See loadRefSet.
  let inline loadPlainSet
    (map: 'T -> 'T voption)
    (snapshot: HashSet<'T>)
    (state: SetNodeState<'T, 'T>)
    : unit =
    for item in snapshot do
      match map item with
      | ValueSome z -> state.Set.Data.Add z |> ignore
      | ValueNone -> ()

  /// Initial load of a map node. See loadRefSet.
  let inline loadMap
    (map: 'K -> 'V -> 'U voption)
    (snapshot: Dictionary<'K, 'V>)
    (state: MapNodeState<'K, 'V, 'U>)
    : unit =
    let mutable e = snapshot.GetEnumerator()

    while e.MoveNext() do
      let k = e.Current.Key
      let v = e.Current.Value

      match map k v with
      | ValueSome u -> state.Data[k] <- u
      | ValueNone -> ()

  // =============================================================================
  // Two-source set algebra (PLAN.md Section 7.3): difference, intersect, xor.
  //
  // Each source has its own journal (side sinks route deliveries by side). The
  // state keeps per-side reference counts; the output membership is derived per
  // operation. Cross-side ordering does not matter: each side's counts update
  // independently, and the output transition is decided from the counts.
  // =============================================================================

  /// <summary>Internal. Receives side-routed set deltas of a two-source node.</summary>
  type internal ITwoSetSinkTarget<'T> =
    abstract member OnSideDeltas:
      side: int * adds: 'T[] * addCount: int * rems: 'T[] * remCount: int ->
        unit

  /// <summary>
  /// Internal. A per-side set sink: appends the delivery to the side's journal.
  /// Two instances per two-source node (construction-time allocation only).
  /// </summary>
  type internal SideSetSink<'T>(target: obj, side: int) =
    interface ISetDeltaSink<'T> with
      member this.OnDeltas
        (adds: 'T[], addCount: int, rems: 'T[], remCount: int)
        =
        (unbox<ITwoSetSinkTarget<'T>> target)
          .OnSideDeltas(side, adds, addCount, rems, remCount)

  /// <summary>
  /// Internal. Routes a delivery to the node's side handler through a plain
  /// interface call. The deltas cross as <c>obj</c> and are unboxed once per
  /// delivery by the node (arrays are reference types).
  /// </summary>
  type internal ISideMapSinkTarget =
    abstract member OnSideDeltas:
      side: int * sets: obj * setCount: int * rems: obj * remCount: int -> unit

  type internal SideMapSink<'K, 'V>(target: ISideMapSinkTarget, side: int) =
    interface IMapDeltaSink<'K, 'V> with
      member this.OnDeltas
        (sets: struct ('K * 'V)[], setCount: int, rems: 'K[], remCount: int)
        =
        target.OnSideDeltas(side, sets, setCount, rems, remCount)

  /// <summary>Internal. State of a two-source set node.</summary>
  type internal TwoSetState<'T when 'T: equality> = internal {
    mutable Version: int64
    mutable Sinks: SinkList
    mutable DepVersions: int64[]
    mutable Left: RefCountedSet<'T>
    mutable Right: RefCountedSet<'T>
    mutable Out: HashSet<'T>
    mutable JournalL: SetDelta<'T>
    mutable JournalR: SetDelta<'T>
    mutable OutDelta: SetDelta<'T>
    // Reused scratch for the net-delta post-pass (construction-time
    // allocation only; zero steady-state allocation).
    mutable Scratch: HashSet<'T>
  }

  module internal TwoSetState =
    let create<'T when 'T: equality>(depCount: int) : TwoSetState<'T> = {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Left = RefCountedSet.create()
      Right = RefCountedSet.create()
      Out = HashSet<'T>()
      JournalL = SetDelta.create()
      JournalR = SetDelta.create()
      OutDelta = SetDelta.create()
      Scratch = HashSet<'T>()
    }

  /// <summary>
  /// Process one side's journal of a two-source set node. Returns whether
  /// the output changed.
  /// </summary>
  let processTwoSide
    (op: TwoSetOp)
    (side: int)
    (state: TwoSetState<'T>)
    : bool =
    let changed = ref false

    let rems =
      if side = 0 then
        state.JournalL.Rems
      else
        state.JournalR.Rems

    let adds =
      if side = 0 then
        state.JournalL.Adds
      else
        state.JournalR.Adds

    let remStart = rems.Count
    let addStart = adds.Count

    try
      let mutable i = 0

      while i < remStart do
        let x = rems.Items[i]

        let removed =
          if side = 0 then
            state.Left.Remove x
          else
            state.Right.Remove x

        if removed then
          let otherHas =
            if side = 0 then
              state.Right.Data.Contains x
            else
              state.Left.Data.Contains x

          match op with
          | Difference ->
            if side = 0 then
              if not otherHas && state.Out.Remove x then
                state.OutDelta.Rems.Append x
                changed.Value <- true
            elif otherHas && state.Out.Add x then
              state.OutDelta.Adds.Append x
              changed.Value <- true
          | Intersect ->
            if otherHas && state.Out.Remove x then
              state.OutDelta.Rems.Append x
              changed.Value <- true
          | Xor ->
            if otherHas then
              if state.Out.Add x then
                state.OutDelta.Adds.Append x
                changed.Value <- true
            elif state.Out.Remove x then
              state.OutDelta.Rems.Append x
              changed.Value <- true

        i <- i + 1

      let mutable j = 0

      while j < addStart do
        let x = adds.Items[j]
        let added = if side = 0 then state.Left.Add x else state.Right.Add x

        if added then
          let otherHas =
            if side = 0 then
              state.Right.Data.Contains x
            else
              state.Left.Data.Contains x

          match op with
          | Difference ->
            if side = 0 then
              if not otherHas && state.Out.Add x then
                state.OutDelta.Adds.Append x
                changed.Value <- true
            elif otherHas && state.Out.Remove x then
              state.OutDelta.Rems.Append x
              changed.Value <- true
          | Intersect ->
            if otherHas && state.Out.Add x then
              state.OutDelta.Adds.Append x
              changed.Value <- true
          | Xor ->
            if otherHas then
              if state.Out.Remove x then
                state.OutDelta.Rems.Append x
                changed.Value <- true
            elif state.Out.Add x then
              state.OutDelta.Adds.Append x
              changed.Value <- true

        j <- j + 1
    finally
      // Compact the side's journal even when an op threw: consumed
      // entries must not be applied twice by the next drain. Entries
      // appended during processing (reentrant writes) survive.
      if side = 0 then
        state.JournalL.Rems.Compact(remStart)
        state.JournalL.Adds.Compact(addStart)
      else
        state.JournalR.Rems.Compact(remStart)
        state.JournalR.Adds.Compact(addStart)

    changed.Value

  /// <summary>
  /// Net-delta post-pass for the two-source set producers: a batch must not
  /// carry the same element in both adds and rems (consumers apply the
  /// buffers in either order; a same-element pair would diverge). For a
  /// refcounted set an add is only emitted when the element was absent, so a
  /// same-element add+remove in one batch always nets to nothing: drop both.
  /// </summary>
  let inline netifyTwoSetDelta(state: TwoSetState<'T>) : unit =
    if state.OutDelta.Adds.Count > 0 && state.OutDelta.Rems.Count > 0 then
      state.Scratch.Clear()

      for i in 0 .. state.OutDelta.Rems.Count - 1 do
        state.Scratch.Add state.OutDelta.Rems.Items[i] |> ignore

      let mutable ai = 0

      while ai < state.OutDelta.Adds.Count do
        if state.Scratch.Contains state.OutDelta.Adds.Items[ai] then
          // Swap-pop: order within a set delta buffer does not matter.
          state.OutDelta.Adds.Items[ai] <-
            state.OutDelta.Adds.Items[state.OutDelta.Adds.Count - 1]

          state.OutDelta.Adds.Count <- state.OutDelta.Adds.Count - 1
        else
          ai <- ai + 1

      let mutable ri = 0

      while ri < state.OutDelta.Rems.Count do
        if state.Scratch.Contains state.OutDelta.Rems.Items[ri] then
          state.OutDelta.Rems.Items[ri] <-
            state.OutDelta.Rems.Items[state.OutDelta.Rems.Count - 1]

          state.OutDelta.Rems.Count <- state.OutDelta.Rems.Count - 1
        else
          ri <- ri + 1

  /// <summary>Drain both journals of a two-source set node. Returns whether the output changed.</summary>
  let inline drainTwoSet (op: TwoSetOp) (state: TwoSetState<'T>) : bool =
    let c1 = processTwoSide op 0 state
    let c2 = processTwoSide op 1 state

    // Net-delta invariant (see netifyTwoSetDelta): same-element add+rem
    // pairs cancel; consumers apply net deltas order-free.
    if c1 || c2 then
      netifyTwoSetDelta state
      true
    else
      false

  /// <summary>
  /// Drain a two-source set node and push the reduced output delta to its
  /// sinks.
  /// </summary>
  let inline drainTwoSetPush (op: TwoSetOp) (state: TwoSetState<'T>) : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true

    try
      let changed = drainTwoSet op state

      if changed then
        pushSetDelta state.Sinks state.OutDelta
        state.OutDelta.Clear()
    finally
      ctx.TxActive <- wasActive

  /// <summary>
  /// The concrete HashSet view of a set node when available (the hot path,
  /// zero allocation). All views in this implementation are HashSets
  /// (constants included — the port has no FrozenSet), so this is an
  /// identity unbox.
  /// </summary>
  let inline asHashSet(view: IReadOnlySet<'T>) : HashSet<'T> =
    unbox<HashSet<'T>> view

  /// <summary>
  /// The concrete ResizeArray view of a list node when available (the hot
  /// path, zero allocation). All views in this implementation are
  /// ResizeArrays, so this is an identity unbox.
  /// </summary>
  let inline asResizeList(view: IReadOnlyList<'T>) : ResizeArray<'T> =
    unbox<ResizeArray<'T>> view

  /// <summary>
  /// The concrete Dictionary view of a map node when available (the hot
  /// path, zero allocation). All views in this implementation are
  /// Dictionaries, so this is an identity unbox.
  /// </summary>
  let inline asDictionary
    (view: IReadOnlyDictionary<'K, 'V>)
    : Dictionary<'K, 'V> =
    unbox<Dictionary<'K, 'V>> view

  /// <summary>Initial load of a two-source set node: build the state from both source views.</summary>
  let loadTwoSet
    (op: TwoSetOp)
    (left: IAdaptiveSet<'T>)
    (right: IAdaptiveSet<'T>)
    (state: TwoSetState<'T>)
    : unit =
    // The views are HashSets in this implementation; interface
    // iteration would box the enumerator.
    let leftView = asHashSet(left.GetValue())

    for x in leftView do
      state.Left.Add x |> ignore

      match op with
      | Difference
      | Xor -> state.Out.Add x |> ignore
      | Intersect -> () // the right loop seeds the intersection

    let rightView = asHashSet(right.GetValue())

    for x in rightView do
      state.Right.Add x |> ignore

      match op with
      | Difference -> state.Out.Remove x |> ignore
      | Intersect ->
        if state.Left.Data.Contains x then
          state.Out.Add x |> ignore
      | Xor ->
        if state.Left.Data.Contains x then
          state.Out.Remove x |> ignore
        else
          state.Out.Add x |> ignore

  // =============================================================================
  // Two-source map algebra (PLAN.md Section 7.3): choose2, intersect(With),
  // union(With). One node shape; the mapping decides the semantics (FDA models
  // all of them on Choose2VReader). The mapping receives the key and both side
  // values (voptions) and returns the output value (voption). It is called only
  // when at least one side has a value (FDA parity: "mapping will always receive
  // at least one *Some* argument"); a key with no value on either side is removed
  // without calling it.
  // =============================================================================

  /// <summary>Internal. State of a choose2 map node.</summary>
  type internal Choose2State<'K, 'V1, 'V2, 'V3 when 'K: equality> = internal {
    mutable Version: int64
    mutable Sinks: SinkList
    mutable DepVersions: int64[]
    mutable Sides: Dictionary<'K, struct ('V1 voption * 'V2 voption)>
    mutable Out: Dictionary<'K, 'V3>
    mutable JournalL: MapDelta<'K, 'V1>
    mutable JournalR: MapDelta<'K, 'V2>
    mutable OutDelta: MapDelta<'K, 'V3>
    // Reused scratch for the net-delta post-pass (construction-time
    // allocation only; zero steady-state allocation).
    mutable Scratch: HashSet<'K>
    mutable Scratch2: HashSet<'K>
  }

  module internal Choose2State =
    let create<'K, 'V1, 'V2, 'V3 when 'K: equality>
      (depCount: int)
      : Choose2State<'K, 'V1, 'V2, 'V3> =
      {
        Version = 0L
        Sinks = SinkList.create()
        DepVersions = Array.zeroCreate depCount
        Sides = Dictionary<'K, struct ('V1 voption * 'V2 voption)>()
        Out = Dictionary<'K, 'V3>()
        JournalL = MapDelta.create()
        JournalR = MapDelta.create()
        OutDelta = MapDelta.create()
        Scratch = HashSet<'K>()
        Scratch2 = HashSet<'K>()
      }

  /// <summary>
  /// Apply one output transition of a choose2 drain: compare with the stored
  /// output, emit the delta (with equal-value elision), update the output.
  /// Returns whether anything changed.
  /// </summary>
  let applyChoose2Out
    (state: Choose2State<'K, 'V1, 'V2, 'V3>)
    (k: 'K)
    (newOut: 'V3 voption)
    : bool =
    let mutable changed = false
    let mutable old = Unchecked.defaultof<'V3>

    if state.Out.TryGetValue(k, &old) then
      if newOut.IsSome then
        let v = newOut.Value

        if EqualityComparer<'V3>.Default.Equals(old, v) then
          ()
        else
          state.Out[k] <- v
          state.OutDelta.Sets.Append(struct (k, v))
          changed <- true
      else
        state.Out.Remove k |> ignore
        state.OutDelta.Rems.Append k
        changed <- true
    elif newOut.IsSome then
      let v = newOut.Value
      state.Out[k] <- v
      state.OutDelta.Sets.Append(struct (k, v))
      changed <- true

    changed

  /// <summary>
  /// Process one side's journal of a choose2 node. The mapping is called only
  /// when at least one side has a value (FDA parity). Returns whether the
  /// output changed.
  /// </summary>
  let inline processChoose2Side
    (mapping: 'K -> 'V1 voption -> 'V2 voption -> 'V3 voption)
    (side: int)
    (state: Choose2State<'K, 'V1, 'V2, 'V3>)
    : bool =
    let changed = ref false

    // The two side journals have different value types, so the loops are
    // per side (an `if side` over the journals would unify 'V1 with 'V2).
    if side = 0 then
      let rems = state.JournalL.Rems
      let sets = state.JournalL.Sets
      let remStart = rems.Count
      let setStart = sets.Count
      let mutable i = 0

      try
        while i < remStart do
          let k = rems.Items[i]
          let mutable cur = struct (ValueNone, ValueNone)

          if state.Sides.TryGetValue(k, &cur) then
            let struct (_, rv) = cur

            match rv with
            | ValueNone ->
              // both sides gone: remove without calling the mapping.
              state.Sides.Remove k |> ignore

              if applyChoose2Out state k ValueNone then
                changed.Value <- true
            | ValueSome _ ->
              let newOut = mapping k ValueNone rv

              if applyChoose2Out state k newOut then
                changed.Value <- true

              state.Sides[k] <- struct (ValueNone, rv)

          i <- i + 1

        let mutable j = 0

        while j < setStart do
          let struct (k, v) = sets.Items[j]
          let mutable cur = struct (ValueNone, ValueNone)

          let rv =
            if state.Sides.TryGetValue(k, &cur) then
              let struct (_, rv) = cur
              rv
            else
              ValueNone

          let newOut = mapping k (ValueSome v) rv

          if applyChoose2Out state k newOut then
            changed.Value <- true

          state.Sides[k] <- struct (ValueSome v, rv)
          j <- j + 1
      finally
        state.JournalL.Rems.Compact(remStart)
        state.JournalL.Sets.Compact(setStart)
    else
      let rems = state.JournalR.Rems
      let sets = state.JournalR.Sets
      let remStart = rems.Count
      let setStart = sets.Count
      let mutable i = 0

      try
        while i < remStart do
          let k = rems.Items[i]
          let mutable cur = struct (ValueNone, ValueNone)

          if state.Sides.TryGetValue(k, &cur) then
            let struct (lv, _) = cur

            match lv with
            | ValueNone ->
              // both sides gone: remove without calling the mapping.
              state.Sides.Remove k |> ignore

              if applyChoose2Out state k ValueNone then
                changed.Value <- true
            | ValueSome _ ->
              let newOut = mapping k lv ValueNone

              if applyChoose2Out state k newOut then
                changed.Value <- true

              state.Sides[k] <- struct (lv, ValueNone)

          i <- i + 1

        let mutable j = 0

        while j < setStart do
          let struct (k, v) = sets.Items[j]
          let mutable cur = struct (ValueNone, ValueNone)

          let lv =
            if state.Sides.TryGetValue(k, &cur) then
              let struct (lv, _) = cur
              lv
            else
              ValueNone

          let newOut = mapping k lv (ValueSome v)

          if applyChoose2Out state k newOut then
            changed.Value <- true

          state.Sides[k] <- struct (lv, ValueSome v)
          j <- j + 1
      finally
        state.JournalR.Rems.Compact(remStart)
        state.JournalR.Sets.Compact(setStart)

    changed.Value

  /// <summary>
  /// Net-delta post-pass for choose2 producers: a batch must not carry the
  /// same key in both sets and rems. Both sides can touch one key in one
  /// batch (Set k then Rem k, or Rem k then Set k); the final membership
  /// (state.Out) decides the net: present -> keep the sets, drop the rems;
  /// absent -> keep the rems, drop the sets.
  /// </summary>
  let inline netifyChoose2Delta(state: Choose2State<'K, 'V1, 'V2, 'V3>) : unit =
    if state.OutDelta.Sets.Count > 0 && state.OutDelta.Rems.Count > 0 then
      state.Scratch.Clear()
      state.Scratch2.Clear()

      for i in 0 .. state.OutDelta.Sets.Count - 1 do
        let struct (k, _) = state.OutDelta.Sets.Items[i]
        state.Scratch.Add k |> ignore

      let mutable ri = 0

      while ri < state.OutDelta.Rems.Count do
        let k = state.OutDelta.Rems.Items[ri]

        if state.Scratch.Contains k then
          if state.Out.ContainsKey k then
            // Present at the end: the net is a Set. Drop this Rem.
            state.OutDelta.Rems.Items[ri] <-
              state.OutDelta.Rems.Items[state.OutDelta.Rems.Count - 1]

            state.OutDelta.Rems.Count <- state.OutDelta.Rems.Count - 1
          else
            // Absent at the end: the net is a Rem. Drop the Sets.
            state.Scratch2.Add k |> ignore
            ri <- ri + 1
        else
          ri <- ri + 1

      let mutable si = 0

      while si < state.OutDelta.Sets.Count do
        let struct (k, _) = state.OutDelta.Sets.Items[si]

        if state.Scratch2.Contains k then
          state.OutDelta.Sets.Items[si] <-
            state.OutDelta.Sets.Items[state.OutDelta.Sets.Count - 1]

          state.OutDelta.Sets.Count <- state.OutDelta.Sets.Count - 1
        else
          si <- si + 1

  /// <summary>Drain both journals of a choose2 node. Returns whether the output changed.</summary>
  let inline drainChoose2
    (mapping: 'K -> 'V1 voption -> 'V2 voption -> 'V3 voption)
    (state: Choose2State<'K, 'V1, 'V2, 'V3>)
    : bool =
    let c1 = processChoose2Side mapping 0 state
    let c2 = processChoose2Side mapping 1 state

    // Net-delta invariant (see netifyChoose2Delta): same-key set+rem pairs
    // reduce to the final state; consumers apply net deltas order-free.
    if c1 || c2 then
      netifyChoose2Delta state
      true
    else
      false

  /// <summary>
  /// Drain a choose2 node and push the reduced output delta to its sinks.
  /// </summary>
  let inline drainChoose2Push
    (mapping: 'K -> 'V1 voption -> 'V2 voption -> 'V3 voption)
    (state: Choose2State<'K, 'V1, 'V2, 'V3>)
    : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true
    // Suspend the append-time coalescing while the journals are replayed
    // (the mapping is user code that can write to either source).
    state.JournalL.InDrain[0] <- 1
    state.JournalR.InDrain[0] <- 1

    try
      let changed = drainChoose2 mapping state

      if changed then
        pushMapDelta state.Sinks state.OutDelta
        state.OutDelta.Clear()
    finally
      state.JournalL.InDrain[0] <- 0
      state.JournalR.InDrain[0] <- 0
      ctx.TxActive <- wasActive

  /// <summary>Initial load of a choose2 node: merge both source snapshots through the mapping.</summary>
  let inline loadChoose2
    (mapping: 'K -> 'V1 voption -> 'V2 voption -> 'V3 voption)
    (leftSnapshot: Dictionary<'K, 'V1>)
    (rightSnapshot: Dictionary<'K, 'V2>)
    (state: Choose2State<'K, 'V1, 'V2, 'V3>)
    : unit =
    let mutable e = leftSnapshot.GetEnumerator()

    while e.MoveNext() do
      let k = e.Current.Key
      let v = e.Current.Value
      state.Sides[k] <- struct (ValueSome v, ValueNone)

      match mapping k (ValueSome v) ValueNone with
      | ValueSome o -> state.Out[k] <- o
      | ValueNone -> ()

    let mutable e2 = rightSnapshot.GetEnumerator()

    while e2.MoveNext() do
      let k = e2.Current.Key
      let v = e2.Current.Value
      let mutable cur = struct (ValueNone, ValueNone)

      if state.Sides.TryGetValue(k, &cur) then
        let struct (lv, _) = cur
        state.Sides[k] <- struct (lv, ValueSome v)

        match mapping k lv (ValueSome v) with
        | ValueSome o -> state.Out[k] <- o
        | ValueNone -> state.Out.Remove k |> ignore
      else
        state.Sides[k] <- struct (ValueNone, ValueSome v)

        match mapping k ValueNone (ValueSome v) with
        | ValueSome o -> state.Out[k] <- o
        | ValueNone -> ()

  // =============================================================================
  // Rebuild helpers (PLAN.md Section 7.3): ofAval replaces the whole state on
  // every value change and emits the diff as the output delta.
  // =============================================================================

  /// <summary>
  /// Replace the state of a plain set node with <paramref name="next"/> and
  /// collect the diff as the output delta. Returns whether anything changed.
  /// The removals are collected first: mutating a HashSet while iterating it is
  /// undefined.
  /// </summary>
  let rebuildSetDiff (next: HashSet<'T>) (state: SetNodeState<'T, 'T>) : bool =
    let mutable changed = false
    let mutable e = next.GetEnumerator()

    while e.MoveNext() do
      let x = e.Current

      if state.Set.Data.Add x then
        state.Out.Adds.Append x
        changed <- true

    // Removals must be collected before mutating: iterate into a scratch
    // buffer, then remove (the original appends into the Out buffer, which
    // is guaranteed empty here — the callers clear it after every push).
    let mutable e2 = state.Set.Data.GetEnumerator()
    let removals = DeltaBuffer.create()

    while e2.MoveNext() do
      let x = e2.Current

      if not(next.Contains x) then
        removals.Append x

    for i in 0 .. removals.Count - 1 do
      state.Set.Data.Remove removals.Items[i] |> ignore
      state.Out.Rems.Append removals.Items[i]

    changed

  /// <summary>
  /// Replace the state of a map node with <paramref name="next"/> and collect
  /// the diff as the output delta (equal values elided). Returns whether
  /// anything changed. The removals are collected first: mutating a Dictionary
  /// while iterating it is undefined.
  /// </summary>
  let rebuildMapDiff
    (next: Dictionary<'K, 'V>)
    (state: MapNodeState<'K, 'V, 'V>)
    : bool =
    let mutable changed = false
    let mutable e = next.GetEnumerator()

    while e.MoveNext() do
      let k = e.Current.Key
      let v = e.Current.Value
      let mutable old = Unchecked.defaultof<'V>

      if
        state.Data.TryGetValue(k, &old)
        && EqualityComparer<'V>.Default.Equals(old, v)
      then
        ()
      else
        state.Data[k] <- v
        state.Out.Sets.Append(struct (k, v))
        changed <- true

    let mutable e2 = state.Data.GetEnumerator()
    let removals = DeltaBuffer.create()

    while e2.MoveNext() do
      let k = e2.Current.Key

      if not(next.ContainsKey k) then
        removals.Append k

    for i in 0 .. removals.Count - 1 do
      state.Data.Remove removals.Items[i] |> ignore
      state.Out.Rems.Append removals.Items[i]
      // Removals are a change too: without this, a removals-only poll
      // leaves Out uncleared and the stale removals replay on the
      // next diff, deleting freshly added entries.
      changed <- true

    changed

  /// <summary>
  /// Replace the list content with <paramref name="next"/> and
  /// collect the positional diff as the output delta (prefix/suffix, the
  /// <c>ChangeableList.ApplyDiff</c> algorithm). Returns whether anything
  /// changed. The ops are appended via the public <see cref="ListDelta"/>
  /// helpers (application order: removals then inserts for the structural
  /// case, updates in place for the equal-count case).
  /// </summary>
  let rebuildListDiff
    (next: ResizeArray<'T>)
    (data: ResizeArray<'T>)
    (out: ListDelta<'T>)
    : bool =
    let oldCount = data.Count
    let newCount = next.Count
    let mutable prefix = 0
    let limit = min oldCount newCount

    while prefix < limit
          && EqualityComparer<'T>.Default.Equals(data[prefix], next[prefix]) do
      prefix <- prefix + 1

    let mutable suffix = 0
    let mutable trimming = true

    while trimming do
      if
        suffix < limit - prefix
        && EqualityComparer<'T>.Default
          .Equals(data[oldCount - 1 - suffix], next[newCount - 1 - suffix])
      then
        suffix <- suffix + 1
      else
        trimming <- false

    let oldMid = oldCount - prefix - suffix
    let newMid = newCount - prefix - suffix

    if oldMid = newMid then
      for i in 0 .. oldMid - 1 do
        let v = next[prefix + i]
        data[prefix + i] <- v
        out.Update(prefix + i, v)
    else
      for i in oldMid - 1 .. -1 .. 0 do
        data.RemoveAt(prefix + i)
        out.Remove(prefix + i)

      for i in 0 .. newMid - 1 do
        let v = next[prefix + i]
        data.Insert(prefix + i, v)
        out.Insert(prefix + i, v)

    not out.IsEmpty

  // =============================================================================
  // Dynamic dependencies (PLAN.md Section 7.4): collect and bind.
  //
  // CollectSetNode unions one inner adaptive set per source element. Each
  // element's contribution is tracked separately (content set + journal +
  // sink); the output is the refcounted union. A removed source element
  // unregisters its inner sink eagerly. BindSetNode/BindMapNode swap the
  // whole inner collection when their scalar value changes (FDA BindReader
  // semantics).
  // =============================================================================

  /// <summary>
  /// Internal. One source element's contribution to a collect node: the inner
  /// adaptive set, its last-seen version, the current content, the pending
  /// journal, and the registered sink.
  /// </summary>
  type internal CollectEntry<'U when 'U: equality> = internal {
    mutable Node: IAdaptiveSet<'U>
    mutable Version: int64
    mutable Content: HashSet<'U>
    mutable Journal: SetDelta<'U>
    mutable Sink: obj
  }

  module internal CollectEntry =
    let create<'U when 'U: equality>
      (node: IAdaptiveSet<'U>)
      : CollectEntry<'U> =
      {
        Node = node
        Version = 0L
        Content = HashSet<'U>()
        Journal = SetDelta.create()
        Sink = null
      }

  /// <summary>Internal. Receives the side-routed deltas of one inner set.</summary>
  type internal ICollectTarget<'T, 'U> =
    abstract member OnInnerDeltas:
      key: 'T * adds: 'U[] * addCount: int * rems: 'U[] * remCount: int -> unit

  /// <summary>
  /// Internal. A per-element sink: routes an inner set's delivery to the
  /// entry of its source element. Per-element allocation (amortized edge
  /// formation; zero steady-state allocation).
  /// </summary>
  type internal CollectSink<'T, 'U>(target: ICollectTarget<'T, 'U>, key: 'T) =
    interface ISetDeltaSink<'U> with
      member this.OnDeltas
        (adds: 'U[], addCount: int, rems: 'U[], remCount: int)
        =
        target.OnInnerDeltas(key, adds, addCount, rems, remCount)

  /// <summary>Internal. State of a collect node (PLAN.md Section 7.4).</summary>
  type internal CollectState<'T, 'U when 'T: equality and 'U: equality> = internal {
    mutable Version: int64
    mutable Sinks: SinkList
    mutable DepVersions: int64[]
    mutable Journal: SetDelta<'T>
    mutable Inner: Dictionary<'T, CollectEntry<'U>>
    mutable Global: RefCountedSet<'U>
    mutable OutDelta: SetDelta<'U>
    // Reused scratch for the net-delta pass: prior presence of every
    // output element touched this batch (construction-time allocation
    // only; zero steady-state allocation).
    mutable Scratch: Dictionary<'U, bool>
  }

  module internal CollectState =
    let create<'T, 'U when 'T: equality and 'U: equality>
      (depCount: int)
      : CollectState<'T, 'U> =
      {
        Version = 0L
        Sinks = SinkList.create()
        DepVersions = Array.zeroCreate depCount
        Journal = SetDelta.create()
        Inner = Dictionary<'T, CollectEntry<'U>>()
        Global = RefCountedSet.create()
        OutDelta = SetDelta.create()
        Scratch = Dictionary<'U, bool>()
      }

  /// <summary>
  /// Drain a collect node: process the source journal (removed elements drop
  /// their contribution and unregister their inner sink; added elements create
  /// an entry and load the inner content) and then every entry journal.
  /// Returns whether the output changed.
  /// </summary>
  let inline drainCollect
    (target: ICollectTarget<'T, 'U>)
    (mapping: 'T -> IAdaptiveSet<'U>)
    (state: CollectState<'T, 'U>)
    : bool =
    let changed = ref false
    // Reused scratch: prior presence of every output element touched this
    // batch. The net out delta is derived at the end: a same-element
    // add+remove in one batch (two inner sets, or reentrant writes) must
    // not reach consumers as a same-element pair (net-delta invariant).
    state.Scratch.Clear()

    // ---- source journal: removals first (drop entries), then adds (create).
    let rems = state.Journal.Rems
    let remStart = rems.Count
    let adds = state.Journal.Adds
    let addStart = adds.Count
    let mutable i = 0
    // Consumed counts: see drainRefSet (the throwing mapping entry
    // survives for the next drain).
    let mutable remsDone = 0
    let mutable addsDone = 0

    try
      while i < remStart do
        let x = rems.Items[i]
        let mutable entry = Unchecked.defaultof<CollectEntry<'U>>

        if state.Inner.TryGetValue(x, &entry) then
          // Eager edge removal: unregister before dropping.
          match trySetSinkRegistry(box entry.Node) with
          | ValueSome r -> r.RemoveSetSink(entry.Sink)
          | ValueNone -> ()

          let mutable ce = entry.Content.GetEnumerator()

          while ce.MoveNext() do
            let u = ce.Current

            if not(state.Scratch.ContainsKey u) then
              state.Scratch[u] <- state.Global.Data.Contains u

            if state.Global.Remove u then
              changed.Value <- true

          state.Inner.Remove x |> ignore

        i <- i + 1
        remsDone <- i

      let mutable j = 0

      while j < addStart do
        let x = adds.Items[j]
        let mutable existing = Unchecked.defaultof<CollectEntry<'U>>

        if not(state.Inner.TryGetValue(x, &existing)) then
          let inner = mapping x
          // Read first, register after: the view is complete, and the sink
          // sees only deltas that follow this point in time.
          let view = inner.GetValue()
          let entry = CollectEntry.create inner
          // The view is a HashSet in this implementation; an
          // interface iteration would box the enumerator.
          let data = asHashSet view

          let mutable de = data.GetEnumerator()

          while de.MoveNext() do
            let u = de.Current

            if not(state.Scratch.ContainsKey u) then
              state.Scratch[u] <- state.Global.Data.Contains u

            if state.Global.Add u then
              changed.Value <- true

            entry.Content.Add u |> ignore

          let sink = CollectSink<'T, 'U>(target, x)
          entry.Sink <- box sink

          match trySetSinkRegistry(box inner) with
          | ValueSome r -> r.AddSetSink(entry.Sink)
          | ValueNone -> ()

          entry.Version <- committedVersion inner
          state.Inner[x] <- entry

        j <- j + 1
        addsDone <- j

      // ---- entry journals. No user code runs here: the dictionary is stable.
      let mutable de = state.Inner.GetEnumerator()

      while de.MoveNext() do
        let x = de.Current.Key
        let entry = de.Current.Value

        if not entry.Journal.IsEmpty then
          let entryRems = entry.Journal.Rems
          let entryAdds = entry.Journal.Adds
          let entryRemStart = entryRems.Count
          let entryAddStart = entryAdds.Count
          let mutable k = 0

          while k < entryRemStart do
            let u = entryRems.Items[k]

            if entry.Content.Remove u then
              if not(state.Scratch.ContainsKey u) then
                state.Scratch[u] <- state.Global.Data.Contains u

              if state.Global.Remove u then
                changed.Value <- true

            k <- k + 1

          let mutable l = 0

          while l < entryAddStart do
            let u = entryAdds.Items[l]

            if entry.Content.Add u then
              if not(state.Scratch.ContainsKey u) then
                state.Scratch[u] <- state.Global.Data.Contains u

              if state.Global.Add u then
                changed.Value <- true

            l <- l + 1

          // Compact the entry journal (no reentrancy in this pass, but
          // keep the marker pattern: entries appended during processing
          // would survive).
          entry.Journal.Rems.Compact(entryRemStart)
          entry.Journal.Adds.Compact(entryAddStart)
          state.Inner[x] <- entry
    finally
      // Compact the source journal even when the mapping threw: consumed
      // entries must not be applied twice by the next drain (double
      // subtract corrupts the refcounted union); the entry that threw
      // survives.
      state.Journal.Rems.Compact(remsDone)
      state.Journal.Adds.Compact(addsDone)

    // Net out delta: prior vs final presence per touched element (the
    // intermediate ops already moved the state; the delta describes the
    // true batch transition).
    let mutable e = state.Scratch.GetEnumerator()

    while e.MoveNext() do
      let u = e.Current.Key
      let prior = e.Current.Value
      let final = state.Global.Data.Contains u

      if prior <> final then
        if final then
          state.OutDelta.Adds.Append u
        else
          state.OutDelta.Rems.Append u

        changed.Value <- true

    changed.Value

  /// <summary>
  /// Drain a collect node and push the reduced output delta to its sinks.
  /// </summary>
  let inline drainCollectPush
    (target: ICollectTarget<'T, 'U>)
    (mapping: 'T -> IAdaptiveSet<'U>)
    (state: CollectState<'T, 'U>)
    : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true
    // Suspend the append-time coalescing while the journal is replayed:
    // the mapping is user code that can write to the source.
    state.Journal.InDrain[0] <- 1

    try
      let changed = drainCollect target mapping state

      if changed then
        pushSetDelta state.Sinks state.OutDelta
        state.OutDelta.Clear()
    finally
      state.Journal.InDrain[0] <- 0
      ctx.TxActive <- wasActive

  /// <summary>
  /// Internal. State of a bind node over a scalar value (PLAN.md Section 7.4):
  /// one inner set, swapped when the value changes. The content set is the
  /// output (a single contribution needs no refcounts).
  /// </summary>
  type internal BindSetState<'U when 'U: equality> = internal {
    mutable Version: int64
    mutable Sinks: SinkList
    mutable DepVersions: int64[]
    mutable Journal: SetDelta<'U>
    mutable Data: HashSet<'U>
    mutable OutDelta: SetDelta<'U>
  }

  module internal BindSetState =
    let create<'U when 'U: equality>(depCount: int) : BindSetState<'U> = {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Journal = SetDelta.create()
      Data = HashSet<'U>()
      OutDelta = SetDelta.create()
    }

  /// <summary>
  /// Drain a bind set node: apply the inner journal to the output. Returns
  /// whether the output changed.
  /// </summary>
  let inline drainBindSet(state: BindSetState<'U>) : bool =
    let changed = ref false
    let rems = state.Journal.Rems
    let remStart = rems.Count
    let mutable i = 0

    while i < remStart do
      let u = rems.Items[i]

      if state.Data.Remove u then
        state.OutDelta.Rems.Append u
        changed.Value <- true

      i <- i + 1

    let adds = state.Journal.Adds
    let addStart = adds.Count
    let mutable j = 0

    while j < addStart do
      let u = adds.Items[j]

      if state.Data.Add u then
        state.OutDelta.Adds.Append u
        changed.Value <- true

      j <- j + 1

    state.Journal.Rems.Compact(remStart)
    state.Journal.Adds.Compact(addStart)
    changed.Value

  /// <summary>
  /// Drain a bind set node and push the reduced output delta to its sinks.
  /// </summary>
  let inline drainBindSetPush(state: BindSetState<'U>) : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true

    try
      let changed = drainBindSet state

      if changed then
        pushSetDelta state.Sinks state.OutDelta
        state.OutDelta.Clear()
    finally
      ctx.TxActive <- wasActive

  /// <summary>
  /// Internal. State of a bind map node over a scalar value (PLAN.md Section
  /// 7.4): one inner map, swapped when the value changes.
  /// </summary>
  type internal BindMapState<'K, 'V when 'K: equality> = internal {
    mutable Version: int64
    mutable Sinks: SinkList
    mutable DepVersions: int64[]
    mutable Journal: MapDelta<'K, 'V>
    mutable Data: Dictionary<'K, 'V>
    mutable OutDelta: MapDelta<'K, 'V>
  }

  module internal BindMapState =
    let create<'K, 'V when 'K: equality>(depCount: int) : BindMapState<'K, 'V> = {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Journal = MapDelta.create()
      Data = Dictionary<'K, 'V>()
      OutDelta = MapDelta.create()
    }

  /// <summary>
  /// Drain a bind map node: apply the inner journal to the output (equal
  /// values elided defensively; the boundary already emits effective deltas).
  /// Returns whether the output changed.
  /// </summary>
  let inline drainBindMap(state: BindMapState<'K, 'V>) : bool =
    let changed = ref false
    let rems = state.Journal.Rems
    let remStart = rems.Count
    let mutable i = 0

    while i < remStart do
      let k = rems.Items[i]

      if state.Data.Remove k then
        state.OutDelta.Rems.Append k
        changed.Value <- true

      i <- i + 1

    let sets = state.Journal.Sets
    let setStart = sets.Count
    let mutable j = 0

    while j < setStart do
      let struct (k, v) = sets.Items[j]
      let mutable old = Unchecked.defaultof<'V>

      if
        state.Data.TryGetValue(k, &old)
        && EqualityComparer<'V>.Default.Equals(old, v)
      then
        ()
      else
        state.Data[k] <- v
        state.OutDelta.Sets.Append(struct (k, v))
        changed.Value <- true

      j <- j + 1

    state.Journal.Rems.Compact(remStart)
    state.Journal.Sets.Compact(setStart)
    changed.Value

  /// <summary>
  /// Drain a bind map node and push the reduced output delta to its sinks.
  /// </summary>
  let inline drainBindMapPush(state: BindMapState<'K, 'V>) : unit =
    let ctx = GraphContext.Current
    let wasActive = ctx.TxActive
    ctx.TxActive <- true

    try
      let changed = drainBindMap state

      if changed then
        pushMapDelta state.Sinks state.OutDelta
        state.OutDelta.Clear()
    finally
      ctx.TxActive <- wasActive
