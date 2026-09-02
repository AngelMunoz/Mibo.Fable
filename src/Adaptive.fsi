namespace Mibo.Fable.Adaptive

// The scalar core: the web port of Mibo.Adaptive's Core/Library.fs.
// Pull-lazy dependency graph — writes mark and bump versions; reads
// version-check their dependency snapshots and recompute at most once per
// change. Deviations from the original are limited to the unportable .NET
// semantics: single thread (the ambient graph is one context, posts ride a
// plain FIFO drained at the next graph operation) and no Task/ValueTask
// members.

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
/// <para>
/// Adaptive values form a dependency graph. When you read a value via
/// <c>GetValue()</c>, the system checks if any dependencies have changed and
/// recomputes if necessary.
/// </para>
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

/// Internal. Reference-dropping fill; stands in for the original's
/// Array.Clear calls (clearing only drops references — JS needs no GC
/// prompt — but the calls are kept for parity).
module internal Arrays =
  val inline clearRange: arr: 'a[] -> start: int -> len: int -> unit

/// Internal. Reusable buffer of commit actions for one graph context.
type internal TransactionBuffer =
  new: unit -> TransactionBuffer
  member Reset: unit -> unit
  member Enqueue: action: ICommit -> unit
  member Commit: unit -> unit
  member Abort: unit -> unit

/// Internal. Re-entrant dependency collector with stack frames. One
/// instance lives on each graph context.
type internal DependencyCollector =
  new: unit -> DependencyCollector
  member Reset: unit -> unit
  member FrameDepth: int
  member Add: dep: IAdaptiveObject * version: int64 -> unit
  member PushFrame: unit -> unit
  member PopFrame: unit -> int
  /// Clear the whole buffer at the end of the outermost evaluation: the
  /// collector lives on the ambient graph context, so without this the
  /// deepest evaluation's objects stay reachable through it until a deeper
  /// evaluation overwrites the slots.
  member Clear: unit -> unit
  /// Get the current frame's deps (depBuffer, versionBuffer, start, length).
  member CurrentFrame: unit -> struct (IAdaptiveObject[] * int64[] * int * int)

/// Internal. Source of a posted change: applies the pending posted value.
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
type internal GraphContext =
  new: unit -> GraphContext
  /// The ambient graph, created lazily on first use.
  static member internal Current: GraphContext
  /// Legacy internal name for <see cref="Current"/>: the ambient graph.
  static member internal Default: GraphContext
  member internal WriteGeneration: int64
  member internal Collector: DependencyCollector
  member internal PostQueue: System.Collections.Generic.Queue<IPostSource>
  /// Add a dependency with its current committed version.
  member internal AddDependency: dep: IAdaptiveObject * version: int64 -> unit
  /// Claim the graph for the current operation. On the outermost claim,
  /// pending posts are drained automatically (auto-pump). A drain failure
  /// unwinds the claim before rethrowing so the caller's ReleaseOwner
  /// cannot underflow.
  member internal ClaimOwner: unit -> unit
  /// Release one claim of ClaimOwner. A release past zero is a no-op.
  member internal ReleaseOwner: unit -> unit
  member internal EnterEvaluation: unit -> unit
  member internal ExitEvaluation: unit -> unit
  member internal CollectorActive: bool with get, set
  member internal TxBuffer: TransactionBuffer
  member internal TxActive: bool with get, set
  /// Record one applied write: advances the write generation, which
  /// invalidates every write-generation-keyed dirty cache in this graph.
  member internal BumpWriteGeneration: unit -> unit
  /// Apply all pending posts now (optional; drains run automatically at
  /// the next graph operation). No-op when nothing is pending.
  member internal Pump: unit -> unit

/// Internal. The runtime functions take the graph context explicitly: node
/// methods pass their captured context (a field read), so the per-node hot
/// paths never re-resolve the ambient graph.
module internal AdaptiveRuntime =
  val inline getWriteGeneration: ctx: GraphContext -> int64
  val inline enterEvaluation: ctx: GraphContext -> unit
  val inline exitEvaluation: ctx: GraphContext -> unit
  /// Add a dependency with its current committed version. Resolves the
  /// ambient graph; used by the collection nodes. The scalar node hot
  /// paths use <see cref="GraphContext.AddDependency"/> instead.
  val inline addDependency: dep: IAdaptiveObject -> version: int64 -> unit

  /// Collect dependencies during evaluation.
  val inline collect:
    ctx: GraphContext ->
    f: (unit -> 'T) ->
      struct ('T * IAdaptiveObject[] * int64[] * int * int)

/// <summary>
/// Applies changes posted to the graph.
/// </summary>
/// <remarks>
/// A "post" is applied at the start of the next graph operation (the drain
/// runs on the outermost claim), so several posts collapse into one
/// batched application — the same observable behavior as the original's
/// per-thread post rings, minus the cross-thread transport.
/// </remarks>
module Posting =
  /// <summary>
  /// Applies all pending posted changes now. Optional: pending posts are
  /// applied automatically at the next graph operation. Use this to choose
  /// an explicit batch boundary (for example, once per frame).
  /// </summary>
  val pump: unit -> unit

/// <summary>
/// Runs a function as a transaction. Writes inside the transaction are
/// deferred and applied at commit. Nested calls join the running
/// transaction. Reads inside a transaction see the pre-transaction values.
/// </summary>
module Transaction =
  /// <summary>
  /// Runs a function as a transaction. Writes inside the transaction are
  /// deferred and applied at commit. Nested calls join the running
  /// transaction. Reads inside a transaction see the pre-transaction
  /// values. A failure aborts: deferred writes are discarded.
  /// </summary>
  /// <example>
  /// <code>
  /// Transaction.run (fun () ->
  ///     CVal.set 1 a
  ///     CVal.set 2 b) |> ignore
  /// </code>
  /// </example>
  val run: f: (unit -> 'T) -> 'T

/// Internal. An adaptive value whose content never changes.
type ConstantValue<'T> =
  new: value: 'T -> ConstantValue<'T>
  interface IAdaptiveValue<'T>

/// Internal. An adaptive value computed at most once, on the first read.
type LazyConstantValue<'T> =
  new: create: (unit -> 'T) -> LazyConstantValue<'T>
  interface IAdaptiveValue<'T>

/// Internal. The generic derived node: recomputes on read when any
/// recorded dependency version moved. Re-reads all dependencies per
/// recompute (dynamic dependencies and edge self-healing rely on it).
type AdaptiveNode<'T> =
  new: compute: (unit -> 'T) -> AdaptiveNode<'T>
  interface IAdaptiveValue<'T>
  interface ICommittedVersion

/// Internal. Specialized adaptive node over a fixed set of dependencies of
/// the same type. The values buffer is node-owned and reused across
/// recomputes; the compute function must not retain it.
type MapNNode<'T, 'U> =
  new: deps: IAdaptiveValue<'T>[] * compute: ('T[] -> 'U) -> MapNNode<'T, 'U>
  interface IAdaptiveValue<'U>
  interface ICommittedVersion

/// Internal. Specialized adaptive node that reduces N dependencies using a
/// binary operation, with no intermediate array. Values are reduced
/// left-to-right; the <c>init</c> value is returned when there are no
/// dependencies.
type ReduceNode<'T> =
  new:
    deps: IAdaptiveValue<'T>[] * init: 'T * reduce: ('T -> 'T -> 'T) ->
      ReduceNode<'T>

  interface IAdaptiveValue<'T>
  interface ICommittedVersion

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
type ChangeableValue<'T> =
  new: initial: 'T -> ChangeableValue<'T>
  /// <summary>
  /// Sets the current value. Inside a transaction the write is deferred to
  /// commit; otherwise it applies immediately.
  /// </summary>
  member Set: newValue: 'T -> unit
  /// <summary>
  /// Gets or sets the current value. The setter routes through
  /// <see cref="Set"/>. The getter returns the raw current value; it does
  /// not register a dependency (use <see cref="GetValue"/> for that).
  /// </summary>
  member Value: 'T with get, set
  /// <summary>
  /// Gets the current value and registers a dependency for the calling
  /// computation.
  /// </summary>
  member GetValue: unit -> 'T
  /// <summary>
  /// Sets the current value and returns whether the value changed. A write
  /// with an equal value returns <c>false</c> and marks nothing.
  /// </summary>
  member UpdateTo: newValue: 'T -> bool
  /// <summary>
  /// Posts a new value. The value is applied automatically at the next
  /// graph operation. Several posts before the application collapse to the
  /// last value; posting an equal value does not mark.
  /// </summary>
  member Post: newValue: 'T -> unit
  member internal Apply: newValue: 'T -> unit
  member internal ApplyPending: unit -> unit
  member internal AbortPending: unit -> unit
  member internal ApplyPostedValue: unit -> unit
  interface IAdaptiveValue<'T>
  interface ICommit
  interface IPostSource

/// <summary>An abbreviation for <see cref="ChangeableValue&lt;'T&gt;"/> (FDA <c>cval&lt;'T&gt;</c> parity).</summary>
type cval<'T> = ChangeableValue<'T>

/// <summary>
/// Core operations for creating and transforming adaptive values.
/// Adaptive values automatically track dependencies and recompute only
/// when their inputs change.
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
  /// <see cref="ofExternal"/>. Not invalidated → reads are O(1): no
  /// re-read, no comparison, no allocation. The invalidate handle is O(1)
  /// to call; the re-read happens on the next read.
  /// </summary>
  type ExternalValueNode<'T when 'T: equality> =
    new: read: (unit -> 'T) -> ExternalValueNode<'T>
    /// <summary>
    /// The invalidate handle implementation (returned by
    /// <see cref="ofExternal"/>). Call this when the external source
    /// changed; the re-read happens on the next read. Not for direct use.
    /// </summary>
    member Invalidate: unit -> unit
    interface IPostSource
    interface IAdaptiveValue<'T>

  /// <summary>
  /// Creates an adaptive value from an external snapshot function and an
  /// invalidate handle. The read function runs at most once per
  /// invalidate, on the next read; when not invalidated, reads are O(1)
  /// and allocate nothing. The handle is O(1) to call.
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
  val inline ofExternal:
    read: (unit -> 'T) -> aval<'T> * (unit -> unit) when 'T: equality

  /// <summary>
  /// Creates a constant adaptive value that never changes.
  /// </summary>
  /// <remarks>
  /// Constant values have zero overhead - they never recompute and don't
  /// track dependencies.
  /// </remarks>
  val inline constant: value: 'T -> aval<'T>

  /// <summary>
  /// Creates a constant adaptive value using the given create function.
  /// The function runs at most once, on the first read; later reads return
  /// the cached value.
  /// </summary>
  val inline delay: create: (unit -> 'T) -> aval<'T>

  /// <summary>
  /// Creates a changeable value initially holding the given value (the
  /// same as <c>CVal.create</c>).
  /// </summary>
  val inline init: value: 'T -> cval<'T>

  /// <summary>
  /// Transforms an adaptive value using a mapping function.
  /// </summary>
  /// <remarks>
  /// The function is called lazily - only when the result is read and the
  /// source has changed. The result is cached until the source changes.
  /// </remarks>
  val inline map: f: ('T -> 'U) -> value: aval<'T> -> aval<'U>

  /// <summary>
  /// Combines two adaptive values using a mapping function.
  /// </summary>
  /// <remarks>
  /// Recomputes only when either input changes. Both inputs are read in a
  /// single evaluation.
  /// </remarks>
  val inline map2:
    f: ('T -> 'U -> 'V) -> left: aval<'T> -> right: aval<'U> -> aval<'V>

  /// <summary>
  /// Combines three adaptive values using a mapping function.
  /// Recomputes only when one of the three inputs changes.
  /// </summary>
  val inline map3:
    f: ('A -> 'B -> 'C -> 'T) ->
    a: aval<'A> ->
    b: aval<'B> ->
    c: aval<'C> ->
      aval<'T>

  /// <summary>
  /// Combines four adaptive values using a mapping function.
  /// Recomputes only when one of the four inputs changes.
  /// </summary>
  val inline map4:
    f: ('A -> 'B -> 'C -> 'D -> 'T) ->
    a: aval<'A> ->
    b: aval<'B> ->
    c: aval<'C> ->
    d: aval<'D> ->
      aval<'T>

  /// <summary>
  /// Combines N adaptive values of the same type using a function that
  /// receives all values as an array. Optimized for wide fan-in patterns.
  /// </summary>
  /// <remarks>
  /// The array passed to the compute function is reused by the node and is
  /// valid only during the call. Do not retain it. If you only need a
  /// reduction (sum, min, max, etc.), prefer <see cref="reduce"/>.
  /// </remarks>
  val inline mapN: compute: ('T[] -> 'U) -> deps: aval<'T>[] -> aval<'U>

  /// <summary>
  /// Reduces N adaptive values using a binary operation and initial value.
  /// Optimized for wide fan-in aggregation patterns (sum, product, min,
  /// max, etc.). Returns the <c>init</c> value when <c>deps</c> is empty.
  /// </summary>
  val inline reduce:
    init: 'T -> reduce: ('T -> 'T -> 'T) -> deps: aval<'T>[] -> aval<'T>

  /// <summary>
  /// Sums N adaptive integer values. Convenience function equivalent to
  /// <c>reduce 0 (+)</c>. Returns 0 when <c>deps</c> is empty.
  /// </summary>
  val inline sum: deps: aval<int>[] -> aval<int>

  /// <summary>
  /// Adaptively depends on <c>value</c>, applies the mapping, and
  /// adaptively depends on the adaptive value the mapping returns. When an
  /// input changes, the previously returned inner value is dropped and the
  /// mapping selects a new one (dynamic dependencies: the next evaluation
  /// re-reads whatever the mapping returns this time).
  /// </summary>
  val inline bind: f: ('T -> aval<'U>) -> value: aval<'T> -> aval<'U>

  /// <summary>
  /// Adaptively applies the mapping to the two values and adaptively
  /// depends on the adaptive value the mapping returns.
  /// </summary>
  val inline bind2:
    f: ('T -> 'U -> aval<'V>) -> a: aval<'T> -> b: aval<'U> -> aval<'V>

  /// <summary>
  /// Adaptively applies the mapping to the three values and adaptively
  /// depends on the adaptive value the mapping returns.
  /// </summary>
  val inline bind3:
    f: ('T -> 'U -> 'V -> aval<'W>) ->
    a: aval<'T> ->
    b: aval<'U> ->
    c: aval<'V> ->
      aval<'W>

  /// <summary>
  /// Creates a custom adaptive value using the given computation.
  /// Callers are responsible for removing inputs that are no longer needed.
  /// </summary>
  val inline custom: compute: (unit -> 'T) -> aval<'T>

  /// <summary>Gets the current value of an adaptive value.</summary>
  val inline getValue: value: aval<'T> -> 'T

  /// <summary>Evaluates the given adaptive value (the same as <c>getValue</c>).</summary>
  val inline force: value: aval<'T> -> 'T

/// <summary>Operations on changeable values.</summary>
module CVal =
  /// <summary>Creates a changeable value initially holding the given value.</summary>
  val inline create: value: 'T -> cval<'T>
  /// <summary>Sets the current value (deferred inside a transaction).</summary>
  val inline set: value: 'T -> cval: cval<'T> -> unit
  /// <summary>
  /// Posts a new value; it is applied automatically at the next graph
  /// operation. See <see cref="ChangeableValue&lt;'T&gt;.Post"/>.
  /// </summary>
  val inline post: value: 'T -> cval: cval<'T> -> unit
  /// <summary>Views the changeable value as an adaptive value.</summary>
  val inline value: cval: cval<'T> -> aval<'T>
