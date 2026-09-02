namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// Internal. Reference-dropping fill; stands in for the original's
/// Array.Clear calls (clearing only drops references — JS needs no GC
/// prompt — but the calls are kept for parity).
module internal Arrays =
  let inline clearRange (arr: 'a[]) (start: int) (len: int) : unit =
    if len > 0 then
      Array.fill arr start len Unchecked.defaultof<'a>

// The scalar core of Mibo.Fable.Adaptive: a line-for-line web port of
// Mibo.Adaptive's Core/Library.fs. Pull-lazy only: writes mark and bump
// versions, nothing recomputes until a read version-checks its dependency
// snapshots. Deviations from the original are limited to the unportable
// .NET semantics: no threads (the ambient graph is one module-level
// context and cross-thread posts become a plain FIFO drained at the next
// graph operation) and no Task/ValueTask surface.

/// <summary>
/// Base interface for all adaptive objects. Provides version tracking for change detection.
/// </summary>
/// <remarks>
/// The version number increases each time the object's value changes.
/// Used by dependent nodes to detect when recomputation is needed.
/// </remarks>
type IAdaptiveObject =
  /// <summary>Gets the current version number. Increases when the value changes.</summary>
  abstract member Version: int64

/// Internal: implemented by nodes whose public <c>Version</c> inflates
/// while dirty (the dirty indicator, "version + 1"). Dependency snapshots
/// record <c>CommittedVersion</c> instead: capturing an inflated reading
/// makes later version comparisons equal and skips real upstream changes.
type internal ICommittedVersion =
  abstract member CommittedVersion: int64

/// <summary>
/// An adaptive value that automatically tracks dependencies and recomputes when inputs change.
/// </summary>
/// <remarks>
/// Adaptive values form a dependency graph. Reading a value re-checks the
/// versions of the dependencies recorded by the previous evaluation and
/// recomputes only when one moved.
/// </remarks>
type IAdaptiveValue<'T> =
  inherit IAdaptiveObject
  /// <summary>
  /// Gets the current value, recomputing if any dependencies have changed.
  /// </summary>
  abstract member GetValue: unit -> 'T

/// <summary>An abbreviation for <see cref="IAdaptiveValue&lt;'T&gt;"/> (FDA <c>aval&lt;'T&gt;</c> parity).</summary>
type aval<'T> = IAdaptiveValue<'T>

/// <summary>
/// A unit of deferred work applied at transaction commit.
/// </summary>
type ICommit =
  /// <summary>Applies the deferred work.</summary>
  abstract member Commit: unit -> unit
  /// <summary>Discards the deferred work after a transaction rollback.</summary>
  abstract member Abort: unit -> unit

/// Internal. Reusable buffer of commit actions for one graph context.
type internal TransactionBuffer() =
  let mutable buffer: ICommit[] = Array.zeroCreate 8
  let mutable count = 0

  member _.Reset() =
    if count > 0 then
      Arrays.clearRange buffer 0 count
      count <- 0

  member _.Enqueue(action: ICommit) =
    if count = buffer.Length then
      let next = Array.zeroCreate(buffer.Length * 2)
      Array.blit buffer 0 next 0 buffer.Length
      buffer <- next

    buffer[count] <- action
    count <- count + 1

  member _.Commit() =
    let mutable i = 0
    let mutable firstEx: exn voption = ValueNone

    while i < count do
      try
        buffer[i].Commit()
      with e ->
        if firstEx.IsNone then
          firstEx <- ValueSome e

        // Do not apply the rest of the batch; discard it.
        i <- i + 1

        while i < count do
          buffer[i].Abort()
          i <- i + 1

      i <- i + 1

    Arrays.clearRange buffer 0 count
    count <- 0

    match firstEx with
    | ValueSome e -> raise e
    | ValueNone -> ()

  member _.Abort() =
    let mutable i = 0

    while i < count do
      buffer[i].Abort()
      i <- i + 1

    Arrays.clearRange buffer 0 count
    count <- 0

/// <summary>
/// Internal. Re-entrant dependency collector with stack frames.
/// One instance lives on each graph context.
/// </summary>
type internal DependencyCollector() =
  let mutable depBuffer: IAdaptiveObject[] = Array.zeroCreate 16
  let mutable versionBuffer: int64[] = Array.zeroCreate 16
  let mutable count = 0
  // Frame stack: stores the starting index of each nested evaluation
  let mutable frameStarts: int[] = Array.zeroCreate 8
  let mutable frameDepth = 0

  member _.Reset() =
    count <- 0
    frameDepth <- 0

  member _.FrameDepth = frameDepth

  member _.Add(dep: IAdaptiveObject, version: int64) =
    if count = depBuffer.Length then
      let newSize = depBuffer.Length * 2
      let nextDeps = Array.zeroCreate newSize
      let nextVersions = Array.zeroCreate newSize
      Array.blit depBuffer 0 nextDeps 0 depBuffer.Length
      Array.blit versionBuffer 0 nextVersions 0 versionBuffer.Length
      depBuffer <- nextDeps
      versionBuffer <- nextVersions

    depBuffer[count] <- dep
    versionBuffer[count] <- version
    count <- count + 1

  member _.PushFrame() =
    if frameDepth = frameStarts.Length then
      let next = Array.zeroCreate(frameStarts.Length * 2)
      Array.blit frameStarts 0 next 0 frameStarts.Length
      frameStarts <- next

    frameStarts[frameDepth] <- count
    frameDepth <- frameDepth + 1

  member _.PopFrame() =
    frameDepth <- frameDepth - 1
    count <- frameStarts[frameDepth]
    frameDepth

  /// Clear the whole buffer at the end of the outermost evaluation: the
  /// collector lives on the single ambient graph context, so without this
  /// the deepest evaluation's objects stay reachable through it until a
  /// deeper evaluation overwrites the slots.
  member _.Clear() =
    if count > 0 then
      Arrays.clearRange depBuffer 0 count
      Arrays.clearRange versionBuffer 0 count

    count <- 0
    frameDepth <- 0

  /// Get the current frame's deps (depBuffer, versionBuffer, start, length).
  member _.CurrentFrame() =
    let start = if frameDepth > 0 then frameStarts[frameDepth - 1] else 0
    struct (depBuffer, versionBuffer, start, count - start)

/// <summary>
/// Internal. Source of a posted change: applies the pending posted value.
/// </summary>
type internal IPostSource =
  abstract member ApplyPosted: unit -> unit

/// <summary>
/// Internal. Holds all mutable runtime state of the adaptive graph.
/// </summary>
/// <remarks>
/// The original Mibo.Adaptive gives every thread its own ambient graph;
/// the JS runtime has one thread, so there is exactly one context. The
/// cross-thread post ring collapses into a plain FIFO that drains at the
/// next graph operation (auto-pump), keeping the "apply as one batch"
/// semantics without threads.
/// </remarks>
[<AllowNullLiteral>]
type internal GraphContext() =
  let mutable evaluationDepth = 0
  // Incremented on every applied write. Invalidates per-evaluation dirty caches
  // when a write lands in the middle of an evaluation.
  let mutable writeGeneration = 0L
  // FIFO of posted sources; drained by Pump at the next graph operation.
  let postQueue = Queue<IPostSource>()
  let collector = DependencyCollector()
  let mutable collectorActive = false
  let txBuffer = TransactionBuffer()
  let mutable txActive = false
  // Operation nesting depth. The automatic drain fires on the outermost claim only.
  let mutable operationDepth = 0

  // The ambient graph. The original keeps one per thread (a ThreadStatic
  // pointer); JS has a single thread, so one module-level context suffices.
  static let mutable currentContext: GraphContext = null

  /// The ambient graph, created lazily on first use.
  static member internal Current: GraphContext =
    if isNull currentContext then
      let created = GraphContext()
      currentContext <- created
      created
    else
      currentContext

  /// Legacy internal name for <see cref="Current"/>: the ambient graph.
  static member internal Default = GraphContext.Current

  member internal _.WriteGeneration = writeGeneration

  member internal _.Collector = collector

  member internal _.PostQueue = postQueue

  /// Add a dependency with its current committed version.
  member internal this.AddDependency(dep: IAdaptiveObject, version: int64) =
    if this.CollectorActive then
      this.Collector.Add(dep, version)

  /// Claim the graph for the current operation. On the outermost claim,
  /// pending posts are drained automatically (auto-pump): they apply as
  /// one batch before the operation runs. A drain failure unwinds the
  /// claim before rethrowing so the caller's ReleaseOwner cannot underflow.
  member internal this.ClaimOwner() =
    operationDepth <- operationDepth + 1

    if operationDepth = 1 && not this.TxActive then
      try
        this.DrainIfPending()
      with e ->
        // Unwind this claim before propagating so the caller's
        // ReleaseOwner in its finally cannot underflow the depth
        // counters; the graph stays usable.
        this.ReleaseOwner()
        raise e

  /// Release one claim of ClaimOwner at the end of an operation. A release
  /// past zero is a no-op: it happens when ClaimOwner unwound its own claim
  /// before rethrowing a drain failure (the caller's finally still runs).
  member internal this.ReleaseOwner() =
    if operationDepth > 0 then
      operationDepth <- operationDepth - 1

  member internal this.EnterEvaluation() =
    this.ClaimOwner()
    evaluationDepth <- evaluationDepth + 1

  member internal this.ExitEvaluation() =
    evaluationDepth <- evaluationDepth - 1
    this.ReleaseOwner()

  member internal _.CollectorActive
    with get () = collectorActive
    and set value = collectorActive <- value

  member internal _.TxBuffer = txBuffer

  member internal _.TxActive
    with get () = txActive
    and set value = txActive <- value

  /// Record one applied write: advances the write generation, which
  /// invalidates every write-generation-keyed dirty cache in this graph.
  /// Called by every write path (scalar writes, collection source writes,
  /// and write-time journal appends of the collection sink machinery).
  member internal _.BumpWriteGeneration() =
    writeGeneration <- writeGeneration + 1L

  /// Apply all pending posts as one batch. Called automatically on the
  /// outermost claim of any graph operation, and from Pump. No-op when the
  /// queue is empty.
  member private this.DrainIfPending() =
    if postQueue.Count > 0 then
      let wasActive = this.TxActive
      this.TxActive <- true

      try
        this.DrainPosts()
      finally
        this.TxActive <- wasActive

  /// Drain the queue, applying each pending posted value.
  member private this.DrainPosts() =
    while postQueue.Count > 0 do
      let source = postQueue.Dequeue()
      source.ApplyPosted()

  /// Apply all pending posts now. Draining is automatic at the start of every
  /// graph operation, so this is optional: use it to choose an explicit batch
  /// boundary (for example, once per frame). No-op when nothing is pending.
  member internal this.Pump() =
    this.ClaimOwner()

    try
      this.DrainIfPending()
    finally
      this.ReleaseOwner()

module internal AdaptiveRuntime =
  // The runtime functions take the graph context explicitly: node methods
  // pass their captured context (a field read), so the per-node hot paths
  // never re-resolve the ambient graph.
  let inline getWriteGeneration(ctx: GraphContext) : int64 = ctx.WriteGeneration

  let inline enterEvaluation(ctx: GraphContext) : unit = ctx.EnterEvaluation()

  let inline exitEvaluation(ctx: GraphContext) : unit = ctx.ExitEvaluation()

  /// Add a dependency with its current committed version. Resolves the
  /// ambient graph; used by the collection nodes, whose read paths are
  /// dominated by delta machinery. The scalar node hot paths use the
  /// explicit-context member <see cref="GraphContext.AddDependency"/> instead.
  let inline addDependency (dep: IAdaptiveObject) (version: int64) : unit =
    let ctx = GraphContext.Current

    if ctx.CollectorActive then
      ctx.Collector.Add(dep, version)

  /// Collect dependencies during evaluation.
  let inline collect (ctx: GraphContext) (f: unit -> 'T) =
    let collector = ctx.Collector

    if not ctx.CollectorActive then
      collector.Reset()
      ctx.CollectorActive <- true

    collector.PushFrame()

    try
      let value = f()
      let struct (deps, versions, start, len) = collector.CurrentFrame()
      struct (value, deps, versions, start, len)
    finally
      if collector.PopFrame() = 0 then
        ctx.CollectorActive <- false
        // Drop references to the evaluation's dependencies: the
        // collector is reachable from the single ambient context.
        collector.Clear()

/// <summary>
/// Applies changes posted to the graph.
/// </summary>
/// <remarks>
/// <para>
/// On the web runtime a "post" is applied at the start of the next graph
/// operation (the drain runs on the outermost claim), so several posts
/// collapse into one batched application with one notification delivery —
/// the same observable behavior as the original's per-thread post rings,
/// minus the cross-thread transport.
/// </para>
/// </remarks>
module Posting =
  /// <summary>
  /// Applies all pending posted changes now. Optional: pending posts are applied
  /// automatically at the next graph operation. Use this to choose an explicit
  /// batch boundary (for example, once per frame).
  /// </summary>
  let pump() : unit = GraphContext.Current.Pump()

/// <summary>
/// Runs a function as a transaction. Writes inside the transaction are deferred
/// and applied at commit. Nested calls join the running transaction.
/// Reads inside a transaction see the pre-transaction values.
/// </summary>
module Transaction =
  /// <summary>
  /// Runs a function as a transaction. Writes inside the transaction are deferred
  /// and applied at commit. Nested calls join the running transaction.
  /// Reads inside a transaction see the pre-transaction values.
  /// </summary>
  let run(f: unit -> 'T) : 'T =
    let ctx = GraphContext.Current
    ctx.ClaimOwner()

    try
      if ctx.TxActive then
        f()
      else
        ctx.TxActive <- true
        ctx.TxBuffer.Reset()
        let mutable committed = false

        let result =
          try
            let value = f()
            ctx.TxBuffer.Commit()
            committed <- true
            value
          finally
            if not committed then
              ctx.TxBuffer.Abort()

            ctx.TxActive <- false

        result
    finally
      ctx.ReleaseOwner()

/// Internal. An adaptive value whose content never changes.
type ConstantValue<'T>(value: 'T) =
  interface IAdaptiveValue<'T> with
    member this.GetValue() =
      AdaptiveRuntime.addDependency (this :> IAdaptiveObject) 0L
      value

    member _.Version = 0L

/// Internal. An adaptive value computed at most once, on the first read.
type LazyConstantValue<'T>(create: unit -> 'T) =
  let mutable computed = false
  let mutable value = Unchecked.defaultof<'T>

  interface IAdaptiveValue<'T> with
    member this.GetValue() =
      AdaptiveRuntime.addDependency (this :> IAdaptiveObject) 0L

      if computed then
        value
      else
        let v = create()
        value <- v
        computed <- true
        v

    member _.Version = 0L

/// Internal. The generic derived node: recomputes on read when any recorded
/// dependency version moved. Write-generation-keyed dirty cache: the verdict
/// of the last version check (or recompute) stays valid until the next
/// applied write moves the global generation.
type AdaptiveNode<'T>(compute: unit -> 'T) =
  // The graph this node belongs to, captured at creation (the ambient graph
  // of the creating thread; on JS the single ambient context).
  let ctx = GraphContext.Current
  let mutable version = 0L
  let mutable hasValue = false
  let mutable value = Unchecked.defaultof<'T>
  let mutable deps: IAdaptiveObject[] = Array.empty
  let mutable depVersions: int64[] = Array.empty
  let mutable depCount = 0
  let mutable lastCheckedWriteGen = -1L
  let mutable dirtyCache = true

  member private this.IsDirty() : bool =
    let writeGen = AdaptiveRuntime.getWriteGeneration ctx

    if lastCheckedWriteGen = writeGen then
      // Already checked at this write generation: return cached result
      dirtyCache
    else
      // Version check: a dependency whose version moved makes this node dirty.
      let mutable dirty = not hasValue
      let mutable i = 0

      while not dirty && i < depCount do
        if deps[i].Version <> depVersions[i] then
          dirty <- true

        i <- i + 1

      lastCheckedWriteGen <- writeGen
      dirtyCache <- dirty
      dirty

  member private this.Recompute() : unit =
    // Generation at which the recompute starts. A write from user code in
    // the middle of the compute moves the generation; the cache is keyed
    // at the start generation, so the next read version-checks and sees
    // the moved dependency version.
    let checkedGen = AdaptiveRuntime.getWriteGeneration ctx

    let struct (newValue, newDeps, newVersions, newStart, newLen) =
      AdaptiveRuntime.collect ctx compute

    value <- newValue

    // Plain arrays, reused across recomputes: grow only when the node's
    // dependency set outgrew the previous one.
    if deps.Length < newLen then
      deps <- Array.zeroCreate newLen
      depVersions <- Array.zeroCreate newLen

    Array.blit newDeps newStart deps 0 newLen
    Array.blit newVersions newStart depVersions 0 newLen

    if depCount > newLen then
      Arrays.clearRange deps newLen (depCount - newLen)

    depCount <- newLen
    hasValue <- true
    version <- version + 1L

    // The recompute is valid as of checkedGen: key the cache there so later
    // reads at the same generation skip the version check.
    lastCheckedWriteGen <- checkedGen
    dirtyCache <- false

  interface IAdaptiveValue<'T> with
    member this.GetValue() =
      AdaptiveRuntime.enterEvaluation ctx

      try
        if this.IsDirty() then
          this.Recompute()

        // Add dependency with committed version AFTER any recompute
        ctx.AddDependency(this :> IAdaptiveObject, version)
        value
      finally
        AdaptiveRuntime.exitEvaluation ctx

    member this.Version =
      AdaptiveRuntime.enterEvaluation ctx

      try
        if this.IsDirty() then version + 1L else version
      finally
        AdaptiveRuntime.exitEvaluation ctx

  interface ICommittedVersion with
    member this.CommittedVersion = version

/// Internal. Specialized adaptive node over a fixed set of dependencies of
/// the same type. The values buffer is node-owned and reused across
/// recomputes; the compute function must not retain it.
type MapNNode<'T, 'U>(deps: IAdaptiveValue<'T>[], compute: 'T[] -> 'U) =
  let ctx = GraphContext.Current
  let mutable version = 0L
  let mutable hasValue = false
  let mutable value = Unchecked.defaultof<'U>
  let values = Array.zeroCreate deps.Length
  let depVersions = Array.zeroCreate deps.Length
  let mutable lastCheckedWriteGen = -1L
  let mutable dirtyCache = true

  member private this.IsDirty() : bool =
    let writeGen = AdaptiveRuntime.getWriteGeneration ctx

    if lastCheckedWriteGen = writeGen then
      dirtyCache
    else
      let mutable dirty = not hasValue
      let mutable i = 0

      while not dirty && i < deps.Length do
        if (deps[i] :> IAdaptiveObject).Version <> depVersions[i] then
          dirty <- true

        i <- i + 1

      lastCheckedWriteGen <- writeGen
      dirtyCache <- dirty
      dirty

  member private this.Recompute() : unit =
    let checkedGen = AdaptiveRuntime.getWriteGeneration ctx

    for i in 0 .. deps.Length - 1 do
      values[i] <- deps[i].GetValue()
      depVersions[i] <- (deps[i] :> IAdaptiveObject).Version

    value <- compute values
    hasValue <- true
    version <- version + 1L
    lastCheckedWriteGen <- checkedGen
    dirtyCache <- false

  interface IAdaptiveValue<'U> with
    member this.GetValue() =
      AdaptiveRuntime.enterEvaluation ctx

      try
        if this.IsDirty() then
          this.Recompute()

        ctx.AddDependency(this :> IAdaptiveObject, version)
        value
      finally
        AdaptiveRuntime.exitEvaluation ctx

    member this.Version =
      AdaptiveRuntime.enterEvaluation ctx

      try
        if this.IsDirty() then version + 1L else version
      finally
        AdaptiveRuntime.exitEvaluation ctx

  interface ICommittedVersion with
    member this.CommittedVersion = version

/// Internal. Specialized adaptive node that reduces N dependencies using a
/// binary operation, with no intermediate array. Values are reduced
/// left-to-right; the <c>init</c> value is returned when there are no
/// dependencies.
type ReduceNode<'T>
  (deps: IAdaptiveValue<'T>[], init: 'T, reduce: 'T -> 'T -> 'T) =
  let ctx = GraphContext.Current
  let mutable version = 0L
  let mutable hasValue = false
  let mutable value = Unchecked.defaultof<'T>
  let depVersions = Array.zeroCreate deps.Length
  let mutable lastCheckedWriteGen = -1L
  let mutable dirtyCache = true

  member private this.IsDirty() : bool =
    let writeGen = AdaptiveRuntime.getWriteGeneration ctx

    if lastCheckedWriteGen = writeGen then
      dirtyCache
    else
      let mutable dirty = not hasValue
      let mutable i = 0

      while not dirty && i < deps.Length do
        if (deps[i] :> IAdaptiveObject).Version <> depVersions[i] then
          dirty <- true

        i <- i + 1

      lastCheckedWriteGen <- writeGen
      dirtyCache <- dirty
      dirty

  member private this.Recompute() : unit =
    let checkedGen = AdaptiveRuntime.getWriteGeneration ctx

    let mutable acc = init

    for i in 0 .. deps.Length - 1 do
      let v = deps[i].GetValue()
      depVersions[i] <- (deps[i] :> IAdaptiveObject).Version
      acc <- reduce acc v

    value <- acc
    hasValue <- true
    version <- version + 1L

    // The recompute is valid as of checkedGen: key the cache there so
    // later reads at the same generation skip the version check.
    lastCheckedWriteGen <- checkedGen
    dirtyCache <- false

  interface IAdaptiveValue<'T> with
    member this.GetValue() =
      AdaptiveRuntime.enterEvaluation ctx

      try
        if this.IsDirty() then
          this.Recompute()

        ctx.AddDependency(this :> IAdaptiveObject, version)
        value
      finally
        AdaptiveRuntime.exitEvaluation ctx

    member this.Version =
      AdaptiveRuntime.enterEvaluation ctx

      try
        if this.IsDirty() then version + 1L else version
      finally
        AdaptiveRuntime.exitEvaluation ctx

  interface ICommittedVersion with
    member this.CommittedVersion = version

/// <summary>
/// An adaptive value whose content can be replaced. Writes bump the
/// version; writes inside <see cref="Transaction.run"/> are deferred to
/// commit; a write with an equal value marks nothing.
/// </summary>
/// <example>
/// <code>
/// let x = CVal.create 1
/// CVal.set 2 x
/// AVal.getValue (CVal.value x)  // 2
/// </code>
/// </example>
type ChangeableValue<'T>(initial: 'T) =
  // The graph this node belongs to, captured at creation (the ambient graph
  // of the creating thread; on JS the single ambient context).
  let ctx = GraphContext.Current
  // Hoisted so the write path does not allocate a comparer per call (the
  // JS runtime constructs one per Default access).
  let ec = EqualityComparer<'T>.Default
  let mutable value = initial
  let mutable version = 0L
  // Pending slot for writes inside a transaction. Last write wins.
  let mutable hasPending = false
  let mutable pendingValue = Unchecked.defaultof<'T>
  // Posted value, applied at the next drain. Last post wins. The queued
  // flag keeps at most one queue entry per source: a post that lands
  // before the drain only replaces the posted value.
  let mutable postedValue = Unchecked.defaultof<'T>
  let mutable posted = false

  member internal this.Apply(newValue: 'T) : unit =
    ctx.ClaimOwner()

    try
      // Equality at the source: a write that changes nothing does nothing.
      if not(ec.Equals(value, newValue)) then
        value <- newValue
        version <- version + 1L
        ctx.BumpWriteGeneration()
    finally
      ctx.ReleaseOwner()

  member internal this.ApplyPending() : unit =
    let newValue = pendingValue
    hasPending <- false
    pendingValue <- Unchecked.defaultof<'T>
    this.Apply(newValue)

  member internal _.AbortPending() : unit =
    hasPending <- false
    pendingValue <- Unchecked.defaultof<'T>

  /// <summary>
  /// Posts a new value. The value is applied automatically at the next
  /// graph operation. Several posts to this source before the application
  /// collapse to the last value. The source equality check applies at
  /// application, so posting an equal value does not mark. Allocates nothing.
  /// </summary>
  /// <example>
  /// <code>
  /// CVal.post (health - 1) health
  /// // the next read applies the post automatically
  /// let h = AVal.getValue (CVal.value health)
  /// </code>
  /// </example>
  member this.Post(newValue: 'T) : unit =
    postedValue <- newValue

    if not posted then
      posted <- true
      ctx.PostQueue.Enqueue(this :> IPostSource)

  /// Apply the pending posted value (called from the drain). The queued
  /// flag clears before applying, mirroring the original's ordering: a
  /// post that lands after the clear re-enqueues, so it cannot be lost.
  member internal this.ApplyPostedValue() : unit =
    posted <- false
    this.Apply(postedValue)

  /// <summary>
  /// Sets the current value. Inside a transaction the write is deferred to
  /// commit; otherwise it applies immediately.
  /// </summary>
  member this.Set(newValue: 'T) : unit =
    if ctx.TxActive then
      pendingValue <- newValue

      if not hasPending then
        hasPending <- true
        ctx.TxBuffer.Enqueue(this :> ICommit)
    else
      this.Apply(newValue)

  /// <summary>
  /// Gets or sets the current value. The setter routes through
  /// <see cref="Set"/>: inside a transaction the write is deferred to
  /// commit. The getter returns the raw current value; it does not
  /// register a dependency (use <see cref="GetValue"/> for that).
  /// </summary>
  member this.Value
    with get (): 'T = value
    and set newValue = this.Set newValue

  /// <summary>
  /// Gets the current value and registers a dependency for the calling
  /// computation.
  /// </summary>
  member this.GetValue() : 'T = (this :> IAdaptiveValue<_>).GetValue()

  /// <summary>
  /// Sets the current value and returns whether the value changed. A write
  /// with an equal value returns <c>false</c> and marks nothing.
  /// </summary>
  member this.UpdateTo(newValue: 'T) : bool =
    if ec.Equals(value, newValue) then
      false
    else
      this.Set newValue
      true

  interface IAdaptiveValue<'T> with
    member this.GetValue() =
      ctx.ClaimOwner()

      try
        ctx.AddDependency(this :> IAdaptiveObject, version)
        value
      finally
        ctx.ReleaseOwner()

    member _.Version = version

  interface ICommit with
    member this.Commit() = this.ApplyPending()
    member this.Abort() = this.AbortPending()

  interface IPostSource with
    member this.ApplyPosted() = this.ApplyPostedValue()

/// <summary>An abbreviation for <see cref="ChangeableValue&lt;'T&gt;"/> (FDA <c>cval&lt;'T&gt;</c> parity).</summary>
type cval<'T> = ChangeableValue<'T>

/// <summary>
/// Core operations for creating and transforming adaptive values.
/// Adaptive values automatically track dependencies and recompute only when
/// their inputs change.
/// </summary>
/// <example>
/// <code>
/// let x = CVal.create 1
/// let y = CVal.create 2
/// let sum = AVal.map2 (+) (CVal.value x) (CVal.value y)
/// AVal.getValue sum  // 3
/// CVal.set 10 x
/// AVal.getValue sum  // 12
/// </code>
/// </example>
module AVal =
  /// <summary>
  /// An adaptive value whose content is supplied by an external snapshot
  /// function, re-read only when invalidated via the handle returned by
  /// <see cref="AVal.ofExternal"/>. Not invalidated → reads are O(1): no
  /// re-read, no comparison, no allocation. The invalidate handle is O(1)
  /// to call; the re-read happens on the next read.
  /// </summary>
  type ExternalValueNode<'T when 'T: equality>(read: unit -> 'T) =
    let ctx = GraphContext.Current
    let ec = EqualityComparer<'T>.Default
    let mutable value = Unchecked.defaultof<'T>
    let mutable hasValue = false
    let mutable version = 0L
    let mutable dirty = true

    /// <summary>
    /// The invalidate handle implementation (returned by
    /// <see cref="AVal.ofExternal"/>). Call this when the external source
    /// changed; the re-read happens on the next read. Not for direct use.
    /// </summary>
    member this.Invalidate() : unit =
      dirty <- true
      ctx.BumpWriteGeneration()

    member private this.Poll() : unit =
      if dirty then
        dirty <- false
        let next = read()

        if not hasValue || not(ec.Equals(value, next)) then
          value <- next
          hasValue <- true
          version <- version + 1L

    interface IPostSource with
      member this.ApplyPosted() = this.Invalidate()

    interface IAdaptiveValue<'T> with
      member this.GetValue() =
        ctx.ClaimOwner()

        try
          this.Poll()
          ctx.AddDependency(this :> IAdaptiveObject, version)
          value
        finally
          ctx.ReleaseOwner()

      member this.Version =
        // Dirty indicator: version + 1 while invalidated but not yet
        // re-read, so version-checking consumers recompute exactly
        // once; the re-read at GetValue decides the real version.
        if dirty then version + 1L else version

  /// <summary>
  /// Creates an adaptive value from an external snapshot function and an
  /// invalidate handle. The read function runs at most once per invalidate,
  /// on the next read; when not invalidated, reads are O(1) and allocate
  /// nothing. The handle is O(1) to call.
  /// </summary>
  /// <example>
  /// <code>
  /// let mutable current = 0
  /// let value, invalidate = AVal.ofExternal (fun () -> current)
  /// current &lt;- 42
  /// invalidate ()
  /// AVal.getValue value  // 42
  /// </code>
  /// </example>
  let inline ofExternal(read: unit -> 'T) : aval<'T> * (unit -> unit) =
    let node = ExternalValueNode<'T>(read)
    (node :> aval<'T>, fun () -> node.Invalidate())

  /// <summary>
  /// Creates a constant adaptive value that never changes.
  /// </summary>
  /// <remarks>
  /// Constant values have zero overhead - they never recompute and don't
  /// track dependencies.
  /// </remarks>
  /// <example>
  /// <code>
  /// let pi = AVal.constant 3.14159
  /// let doubled = AVal.map (fun x -> x * 2.0) pi
  /// </code>
  /// </example>
  let inline constant(value: 'T) : aval<'T> = ConstantValue value :> aval<_>

  /// <summary>
  /// Creates a constant adaptive value using the given create function. The
  /// function runs at most once, on the first read; later reads return the
  /// cached value.
  /// </summary>
  let inline delay(create: unit -> 'T) : aval<'T> =
    LazyConstantValue create :> aval<_>

  /// <summary>
  /// Creates a changeable value initially holding the given value (the
  /// same as <c>CVal.create</c>).
  /// </summary>
  let inline init(value: 'T) : cval<'T> = ChangeableValue value

  /// <summary>
  /// Transforms an adaptive value using a mapping function.
  /// </summary>
  /// <remarks>
  /// The function is called lazily - only when the result is read and the
  /// source has changed. The result is cached until the source changes.
  /// </remarks>
  /// <example>
  /// <code>
  /// let celsius = CVal.create 20.0
  /// let fahrenheit = AVal.map (fun c -> c * 9.0/5.0 + 32.0) (CVal.value celsius)
  /// </code>
  /// </example>
  let inline map (f: 'T -> 'U) (value: aval<'T>) : aval<'U> =
    AdaptiveNode(fun () -> f(value.GetValue())) :> aval<_>

  /// <summary>
  /// Combines two adaptive values using a mapping function.
  /// </summary>
  /// <remarks>
  /// Recomputes only when either input changes. Both inputs are read in a
  /// single evaluation.
  /// </remarks>
  let inline map2
    (f: 'T -> 'U -> 'V)
    (left: aval<'T>)
    (right: aval<'U>)
    : aval<'V> =
    AdaptiveNode(fun () -> f (left.GetValue()) (right.GetValue())) :> aval<_>

  /// <summary>
  /// Combines three adaptive values using a mapping function.
  /// Recomputes only when one of the three inputs changes.
  /// </summary>
  let inline map3
    (f: 'A -> 'B -> 'C -> 'T)
    (a: aval<'A>)
    (b: aval<'B>)
    (c: aval<'C>)
    : aval<'T> =
    AdaptiveNode(fun () -> f (a.GetValue()) (b.GetValue()) (c.GetValue()))
    :> aval<_>

  /// <summary>
  /// Combines four adaptive values using a mapping function.
  /// Recomputes only when one of the four inputs changes.
  /// </summary>
  let inline map4
    (f: 'A -> 'B -> 'C -> 'D -> 'T)
    (a: aval<'A>)
    (b: aval<'B>)
    (c: aval<'C>)
    (d: aval<'D>)
    : aval<'T> =
    AdaptiveNode(fun () ->
      f (a.GetValue()) (b.GetValue()) (c.GetValue()) (d.GetValue()))
    :> aval<_>

  /// <summary>
  /// Combines N adaptive values of the same type using a function that
  /// receives all values as an array. Optimized for wide fan-in patterns.
  /// </summary>
  /// <remarks>
  /// The array passed to the compute function is reused by the node and is
  /// valid only during the call. Do not retain it. If you only need a
  /// reduction (sum, min, max, etc.), prefer <see cref="reduce"/> for
  /// better performance.
  /// </remarks>
  let inline mapN (compute: 'T[] -> 'U) (deps: aval<'T>[]) : aval<'U> =
    MapNNode(deps, compute) :> aval<_>

  /// <summary>
  /// Reduces N adaptive values using a binary operation and initial value.
  /// Optimized for wide fan-in aggregation patterns (sum, product, min,
  /// max, etc.). Returns the <c>init</c> value when <c>deps</c> is empty.
  /// </summary>
  let inline reduce
    (init: 'T)
    (reduce: 'T -> 'T -> 'T)
    (deps: aval<'T>[])
    : aval<'T> =
    ReduceNode(deps, init, reduce) :> aval<_>

  /// <summary>
  /// Sums N adaptive integer values. Convenience function equivalent to
  /// <c>reduce 0 (+)</c>. Returns 0 when <c>deps</c> is empty.
  /// </summary>
  let inline sum(deps: aval<int>[]) : aval<int> =
    ReduceNode(deps, 0, (+)) :> aval<_>

  /// <summary>
  /// Adaptively depends on <c>value</c>, applies the mapping, and
  /// adaptively depends on the adaptive value the mapping returns. When an
  /// input changes, the previously returned inner value is dropped and the
  /// mapping selects a new one (dynamic dependencies: the next evaluation
  /// re-reads whatever the mapping returns this time).
  /// </summary>
  let inline bind (f: 'T -> aval<'U>) (value: aval<'T>) : aval<'U> =
    AdaptiveNode(fun () ->
      let inner = f(value.GetValue())
      inner.GetValue())
    :> aval<_>

  /// <summary>
  /// Adaptively applies the mapping to the two values and adaptively
  /// depends on the adaptive value the mapping returns.
  /// </summary>
  let inline bind2
    (f: 'T -> 'U -> aval<'V>)
    (a: aval<'T>)
    (b: aval<'U>)
    : aval<'V> =
    AdaptiveNode(fun () -> (f (a.GetValue()) (b.GetValue())).GetValue())
    :> aval<_>

  /// <summary>
  /// Adaptively applies the mapping to the three values and adaptively
  /// depends on the adaptive value the mapping returns.
  /// </summary>
  let inline bind3
    (f: 'T -> 'U -> 'V -> aval<'W>)
    (a: aval<'T>)
    (b: aval<'U>)
    (c: aval<'V>)
    : aval<'W> =
    AdaptiveNode(fun () ->
      (f (a.GetValue()) (b.GetValue()) (c.GetValue())).GetValue())
    :> aval<_>

  /// <summary>
  /// Creates a custom adaptive value using the given computation.
  /// Callers are responsible for removing inputs that are no longer needed.
  /// </summary>
  let inline custom(compute: unit -> 'T) : aval<'T> =
    AdaptiveNode compute :> aval<_>

  /// <summary>Gets the current value of an adaptive value.</summary>
  let inline getValue(value: aval<'T>) : 'T = value.GetValue()

  /// <summary>Evaluates the given adaptive value (the same as <c>getValue</c>).</summary>
  let inline force(value: aval<'T>) : 'T = value.GetValue()

/// <summary>Operations on changeable values.</summary>
module CVal =
  /// <summary>Creates a changeable value initially holding the given value.</summary>
  let inline create(value: 'T) : cval<'T> = ChangeableValue value

  /// <summary>Sets the current value (deferred inside a transaction).</summary>
  let inline set (value: 'T) (cval: cval<'T>) : unit = cval.Set value

  /// <summary>
  /// Posts a new value; it is applied automatically at the next graph
  /// operation. See <see cref="ChangeableValue&lt;'T&gt;.Post"/>.
  /// </summary>
  let inline post (value: 'T) (cval: cval<'T>) : unit = cval.Post value

  /// <summary>Views the changeable value as an adaptive value.</summary>
  let inline value(cval: cval<'T>) : aval<'T> = cval :> aval<'T>
