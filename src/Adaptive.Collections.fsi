namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Web port of Mibo.Adaptive's Core/Collections/Shared.fs.
//
// Representation adaptations forced by the JS runtime (mechanisms unchanged):
// the .NET struct state holders become mutable-field records (Fable drops
// struct-field mutations), interface type tests resolve through WeakMap
// markers (:? is always false on JS), sinks ride a JS WeakRef, and Memory
// views become transient (array, count) pairs. Journals, cross-kind
// cancellation/coalescing, the InDrain drain guard, refcounted sets, the
// net-delta invariants, and the pull-lazy drain-on-read behavior are ported
// line for line.

/// Internal. JS WeakRef interop (System.WeakReference is not supported by
/// Fable). Deref normalizes undefined and null slots to null.
module internal WeakRefs =
  val create: target: obj -> obj
  val deref: weakRef: obj -> obj

/// Internal. WeakMap-based markers plus prototype-member probes: the
/// JS-safe replacement for the interface type tests the original performs
/// (:? ICommittedVersion and :? ISetSinkRegistry & friends — always false on
/// JS). Fable emits interface implementation members as named prototype
/// members, so the probes test for them; nodes may also register eagerly in
/// the WeakMaps (the changeable sources do) and the lookups accept both.
module internal WeakMarkers =
  type internal IWeakMap =
    abstract has: key: obj -> bool
    abstract set: key: obj * value: obj -> unit

  val committedVersions: IWeakMap
  val setRegistries: IWeakMap
  val mapRegistries: IWeakMap
  val listRegistries: IWeakMap

  val hasCommittedVersion: x: obj -> bool
  val hasSetRegistry: x: obj -> bool
  val hasMapRegistry: x: obj -> bool
  val hasListRegistry: x: obj -> bool

  /// Mark a node as a version-inflating node (implements ICommittedVersion).
  val inline markCommittedVersion: node: IAdaptiveObject -> unit

  /// Mark a node as a set delta sink registry (implements ISetSinkRegistry).
  val inline markSetRegistry: node: obj -> unit

  /// Mark a node as a map delta sink registry (implements IMapSinkRegistry).
  val inline markMapRegistry: node: obj -> unit

  /// Mark a node as a list delta sink registry (implements IListSinkRegistry).
  val inline markListRegistry: node: obj -> unit

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

/// <summary>One reusable array plus count. Node-owned; grows amortized.</summary>
type internal DeltaBuffer<'T> = internal {
  mutable Items: 'T[]
  mutable Count: int
} with

  member IsEmpty: bool
  member Clear: unit -> unit
  /// Grow the array to hold at least n items. Amortized O(1); array growth only.
  member EnsureCapacity: n: int -> unit
  /// Append one item to the buffer.
  member Append: item: 'T -> unit
  /// Append many items from a source array.
  member AppendRange: items: 'T[] * count: int -> unit

  /// Drop the entries whose key appears in <paramref name="keys" />,
  /// preserving order (shift-compact, one pass). Linear scan for small
  /// products (zero allocation); a hash set above the threshold.
  member RemoveKeys:
    keyOfItem: ('T -> 'K) * keyOfKey: ('S -> 'K) * keys: 'S[] * keyCount: int ->
      unit

  /// Compact in place: drop the first <c>doneCount</c> entries, keeping any
  /// entries appended after the captured start (reentrant writes survive).
  member Compact: doneCount: int -> unit

module internal DeltaBuffer =
  val create<'T> : unit -> DeltaBuffer<'T>
  /// Point-in-time copy: shares the buffer array, copies the count (the
  /// .NET struct-copy semantics of the original).
  val copy<'T> : source: DeltaBuffer<'T> -> DeltaBuffer<'T>

/// <summary>
/// A set delta: elements added and removed since the previous delivery. The
/// buffers are transient: valid only during the delivery that received the
/// delta.
/// </summary>
type SetDelta<'T> = internal {
  mutable Adds: DeltaBuffer<'T>
  mutable Rems: DeltaBuffer<'T>
  /// Shared in-drain flag (a one-slot array). Nonzero while a drain
  /// is replaying this journal; the append-time cross-kind
  /// coalescing is suspended then.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member IsEmpty: bool
  member internal Clear: unit -> unit
  /// <summary>The elements added. Transient: valid during the callback only.</summary>
  member Added: 'T[]
  /// <summary>The number of added elements.</summary>
  member AddedCount: int
  /// <summary>The elements removed. Transient: valid during the callback only.</summary>
  member Removed: 'T[]
  /// <summary>The number of removed elements.</summary>
  member RemovedCount: int
  /// <summary>Appends an add operation. For <see cref="ASet.custom"/> computes.</summary>
  member Add: item: 'T -> unit
  /// <summary>Appends a remove operation. For <see cref="ASet.custom"/> computes.</summary>
  member Remove: item: 'T -> unit

module internal SetDelta =
  val create<'T> : unit -> SetDelta<'T>
  /// Point-in-time copy: shares the buffers, copies the counts.
  val copy<'T> : source: SetDelta<'T> -> SetDelta<'T>

/// <summary>
/// A mutable delta builder for <see cref="ASet.custom"/> computes. The compute
/// receives the current view and this builder, appends the operations that
/// describe the change since the previous call, and returns. The builder is a
/// class: appends mutate the node's pending delta directly.
/// </summary>
type SetDeltaBuilder<'T> =
  new: unit -> SetDeltaBuilder<'T>
  /// <summary>Appends an add operation.</summary>
  member Add: item: 'T -> unit
  /// <summary>Appends a remove operation.</summary>
  member Remove: item: 'T -> unit
  member internal IsEmpty: bool
  member internal Clear: unit -> unit
  member internal Adds: DeltaBuffer<'T>
  member internal Rems: DeltaBuffer<'T>
  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal Snapshot: unit -> SetDelta<'T>

/// <summary>
/// A map delta: upserted entries and removed keys since the previous
/// delivery. The buffers are transient: valid only during the delivery that
/// received the delta.
/// </summary>
type MapDelta<'K, 'V> = internal {
  mutable Sets: DeltaBuffer<struct ('K * 'V)>
  mutable Rems: DeltaBuffer<'K>
  /// Shared in-drain flag; see SetDelta.InDrain.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member IsEmpty: bool
  member internal Clear: unit -> unit
  /// <summary>The entries set (added or updated). Transient: valid during the callback only.</summary>
  member SetEntries: struct ('K * 'V)[]
  /// <summary>The number of entries set.</summary>
  member SetCount: int
  /// <summary>The keys removed. Transient: valid during the callback only.</summary>
  member RemovedKeys: 'K[]
  /// <summary>The number of keys removed.</summary>
  member RemovedCount: int
  /// <summary>Appends an upsert operation. For <see cref="AMap.custom"/> computes.</summary>
  member Set: key: 'K * value: 'V -> unit
  /// <summary>Appends a remove operation. For <see cref="AMap.custom"/> computes.</summary>
  member Remove: key: 'K -> unit

module internal MapDelta =
  val create<'K, 'V> : unit -> MapDelta<'K, 'V>
  /// Point-in-time copy: shares the buffers, copies the counts.
  val copy<'K, 'V> : source: MapDelta<'K, 'V> -> MapDelta<'K, 'V>

/// <summary>
/// A mutable delta builder for <see cref="AMap.custom"/> computes. See
/// <see cref="SetDeltaBuilder&lt;'T&gt;"/> for the protocol.
/// </summary>
type MapDeltaBuilder<'K, 'V> =
  new: unit -> MapDeltaBuilder<'K, 'V>
  /// <summary>Appends an upsert operation.</summary>
  member Set: key: 'K * value: 'V -> unit
  /// <summary>Appends a remove operation.</summary>
  member Remove: key: 'K -> unit
  member internal IsEmpty: bool
  member internal Clear: unit -> unit
  member internal Sets: DeltaBuffer<struct ('K * 'V)>
  member internal Rems: DeltaBuffer<'K>
  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal Snapshot: unit -> MapDelta<'K, 'V>

/// <summary>The kind of a list operation.</summary>
/// <remarks>
/// <c>Clear</c> is used only in changeable-source transaction journals as a
/// marker for a full clear; it is never part of a delivered delta (the source
/// expands it into descending removes).
/// </remarks>
type ListOpKind =
  /// Insert before the element currently at <c>Position</c>; <c>Position = count</c> appends.
  | Insert = 0
  /// Remove the element currently at <c>Position</c>.
  | Remove = 1
  /// Replace the element currently at <c>Position</c>.
  | Update = 2
  /// Internal transaction-journal marker only; never delivered.
  | Clear = 3

/// <summary>
/// One list operation. Positions are 0-based and refer to the state as of the
/// previous operation in the same delta; a delta is applied in order.
/// </summary>
/// <remarks>
/// <c>Source</c> is internal machinery for multi-source nodes (0 = primary or
/// left, 1 = right); delivered deltas always carry 0.
/// </remarks>
[<Struct>]
type ListOp<'T> =
  val Kind: ListOpKind
  val Position: int
  val Value: 'T
  val Source: byte

  new: kind: ListOpKind * position: int * value: 'T * source: byte -> ListOp<'T>

/// <summary>
/// A list delta: ordered operations since the previous delivery. The buffer
/// is transient: valid only during the delivery that received the delta.
/// Order is the semantics: apply the operations sequentially.
/// </summary>
type ListDelta<'T> = internal {
  mutable Ops: DeltaBuffer<ListOp<'T>>
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member IsEmpty: bool
  member internal Clear: unit -> unit
  /// <summary>The operations, in application order. Transient: valid during the callback only.</summary>
  member Operations: ListOp<'T>[]
  /// <summary>The number of operations.</summary>
  member OperationCount: int
  /// <summary>Appends an insert operation. For <see cref="AList.custom"/> computes.</summary>
  member Insert: position: int * value: 'T -> unit
  /// <summary>Appends a remove operation. For <see cref="AList.custom"/> computes.</summary>
  member Remove: position: int -> unit
  /// <summary>Appends an update operation. For <see cref="AList.custom"/> computes.</summary>
  member Update: position: int * value: 'T -> unit

module internal ListDelta =
  val create<'T> : unit -> ListDelta<'T>
  /// Point-in-time copy: shares the buffer, copies the count.
  val copy<'T> : source: ListDelta<'T> -> ListDelta<'T>

/// <summary>
/// A class-based delta builder for <see cref="AList.custom"/> computes.
/// </summary>
type ListDeltaBuilder<'T> =
  new: unit -> ListDeltaBuilder<'T>
  member internal IsEmpty: bool
  member internal Clear: unit -> unit
  member internal Snapshot: unit -> ListDelta<'T>
  /// <summary>Appends an insert operation. Positions refer to the state as of the previous operation.</summary>
  member Insert: position: int * value: 'T -> unit
  /// <summary>Appends a remove operation. Positions refer to the state as of the previous operation.</summary>
  member Remove: position: int -> unit
  /// <summary>Appends an update operation. Positions refer to the state as of the previous operation.</summary>
  member Update: position: int * value: 'T -> unit

/// <summary>Internal. Receives deltas from a list dependency.</summary>
type internal IListDeltaSink<'T> =
  abstract member OnDeltas: ops: ListOp<'T>[] * opCount: int -> unit

/// <summary>Internal. Register/unregister a list delta sink with a dependency.</summary>
type internal IListSinkRegistry =
  abstract member AddListSink: sink: obj -> unit
  abstract member RemoveListSink: sink: obj -> unit

/// <summary>
/// A set with per-element reference counts: two source elements can map onto one
/// output element, and the output element disappears only when the last source
/// reference disappears.
/// </summary>
type internal RefCountedSet<'T when 'T: equality> = internal {
  Data: HashSet<'T>
  Refcounts: Dictionary<'T, int>
} with

  /// Add one reference. Returns whether the element is newly present.
  member Add: item: 'T -> bool
  /// Remove one reference. Returns whether the element is fully removed.
  member Remove: item: 'T -> bool

module internal RefCountedSet =
  val create<'T> : unit -> RefCountedSet<'T> when 'T: equality

/// <summary>
/// State of a derived set node (map over set, filter, union). 'T is the input
/// element type (the journal holds input-coordinate deltas); 'U is the output
/// element type (the state and output deltas live in output coordinates).
/// </summary>
type internal SetNodeState<'T, 'U when 'U: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Set: RefCountedSet<'U>
  mutable Journal: SetDelta<'T>
  mutable Out: SetDelta<'U>
}

module internal SetNodeState =
  val create<'T, 'U> : depCount: int -> SetNodeState<'T, 'U> when 'U: equality

/// <summary>
/// State of a derived map node (map over map, filter). The journal holds
/// input-coordinate deltas (source entries); the state and output deltas live
/// in output coordinates.
/// </summary>
type internal MapNodeState<'K, 'V, 'U when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Data: Dictionary<'K, 'U>
  mutable Journal: MapDelta<'K, 'V>
  mutable Out: MapDelta<'K, 'U>
}

module internal MapNodeState =
  val create<'K, 'V, 'U> :
    depCount: int -> MapNodeState<'K, 'V, 'U> when 'K: equality

/// <summary>
/// Per-element cache entry of the <c>*A</c> nodes (mapA/chooseA/filterA).
/// Holds the element's aval, its version at the last force (the version read
/// BEFORE the force: a mid-force write then leaves the stored version stale,
/// so the next scan re-forces), and its last contribution to the output.
/// </summary>
type internal ElementEntry<'U> = internal {
  mutable Aval: aval<'U voption>
  mutable Version: int64
  mutable Last: 'U voption
}

module internal ElementEntry =
  val create<'U> :
    aval: aval<'U voption> ->
    version: int64 ->
    last: 'U voption ->
      ElementEntry<'U>

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
  val ensureArrayCapacity: arr: 'a[] -> n: int -> 'a[]

  /// Dequeue one item, or none when empty (the PostRing.TryDequeue shape
  /// the posted-batch loops replay through).
  val tryDequeue<'P> : q: Queue<'P> -> 'P voption

  /// The settled counter of a dependency: its committed version when the
  /// node registered as a version-inflating node (the JS-safe stand-in for
  /// the original's :? ICommittedVersion test), its plain Version otherwise
  /// (plain sources never inflate).
  val committedVersion: dep: IAdaptiveObject -> int64

  /// The set delta sink registry of a dependency, when it registered one
  /// (the JS-safe stand-in for the original's :? ISetSinkRegistry test).
  val trySetSinkRegistry: dep: obj -> ISetSinkRegistry voption

  /// The map delta sink registry of a dependency (see trySetSinkRegistry).
  val tryMapSinkRegistry: dep: obj -> IMapSinkRegistry voption

  /// The list delta sink registry of a dependency (see trySetSinkRegistry).
  val tryListSinkRegistry: dep: obj -> IListSinkRegistry voption

  /// Cancel cross-kind duplicates between a journal buffer and an incoming
  /// batch: an incoming entry with a pending opposite-kind entry of the
  /// same element drops BOTH. The pending buffer is compacted in place
  /// and the surviving incoming entries are appended to <c>target</c>.
  val cancelCrossKind<'T when 'T: equality> :
    pending: DeltaBuffer<'T> ->
    incoming: 'T[] ->
    incomingCount: int ->
    target: DeltaBuffer<'T> ->
      unit

  /// Append a delta to a set journal (called at write time by the pusher).
  /// Cross-kind duplicates are cancelled first. Suspended while a drain is
  /// in flight (the InDrain flag).
  val journalAppendSet<'T when 'T: equality> :
    journal: SetDelta<'T> ->
    adds: 'T[] ->
    addCount: int ->
    rems: 'T[] ->
    remCount: int ->
      unit

  /// Append a delta to a map journal (called at write time by the pusher).
  /// Cross-kind duplicates are coalesced first (last op wins).
  val journalAppendMap<'K, 'V when 'K: equality> :
    journal: MapDelta<'K, 'V> ->
    sets: struct ('K * 'V)[] ->
    setCount: int ->
    rems: 'K[] ->
    remCount: int ->
      unit

  /// Append a delta to a list journal (called at write time by the pusher).
  val inline journalAppendList:
    journal: ListDelta<'T> -> ops: ListOp<'T>[] -> opCount: int -> unit

  /// Drop dead sink entries (their node was collected). Runs at the start of
  /// every delivery and on registration: swap-pop is safe here because no
  /// user code can interleave.
  val compactDeadSinks: sinks: SinkList -> unit

  /// Register a sink (weakly). Dead entries are swept on registration so
  /// the list does not accumulate between deliveries.
  val addSink: sinks: SinkList -> sink: obj -> unit

  /// Remove a sink by identity (matches the weak entry's target).
  val removeSink: sinks: SinkList -> sink: obj -> unit

  /// Drop all sinks (disposal): releases the downstream references.
  val clearSinks: sinks: SinkList -> unit

  /// Push a set delta to every registered sink. The batch delivers only to
  /// the sinks registered at the start (bound captured): a sink registered
  /// reentrantly during delivery is not delivered.
  val pushSetDelta<'T> : sinks: SinkList -> delta: SetDelta<'T> -> unit

  /// Push a map delta to every registered sink. See pushSetDelta for the
  /// dead-entry and reentrancy handling.
  val pushMapDelta<'K, 'V> : sinks: SinkList -> delta: MapDelta<'K, 'V> -> unit

  /// Push a set delta to every registered sink and record the write: the
  /// write generation advances, so version-gated readers re-check on their
  /// next read.
  val inline pushAndBumpSet:
    ctx: GraphContext -> delta: SetDelta<'T> -> sinks: SinkList -> unit

  /// Push a map delta to every registered sink and record the write (see
  /// pushAndBumpSet).
  val inline pushAndBumpMap:
    ctx: GraphContext -> delta: MapDelta<'K, 'V> -> sinks: SinkList -> unit

  /// Push a list delta to every registered sink. See pushSetDelta for the
  /// dead-entry and reentrancy handling.
  val pushListDelta<'T> : sinks: SinkList -> delta: ListDelta<'T> -> unit

  /// Push a list delta to every registered sink and record the write (see
  /// pushAndBumpSet).
  val inline pushAndBumpList:
    ctx: GraphContext -> delta: ListDelta<'T> -> sinks: SinkList -> unit

  /// Drain the journal of a set node with refcounts (map over set, union):
  /// apply each pending delta to the state and collect the reduced output
  /// delta. Returns whether the state changed.
  val inline drainRefSet:
    map: ('T -> 'U voption) -> state: SetNodeState<'T, 'U> -> bool

  /// Drain the journal of a set node without refcounts (filter): plain
  /// membership. Returns whether the state changed.
  val inline drainPlainSet:
    map: ('T -> 'T voption) -> state: SetNodeState<'T, 'T> -> bool

  /// Drain the journal of a map node: apply each pending delta to the state
  /// and collect the reduced output delta. Returns whether the state changed.
  val inline drainMap:
    map: ('K -> 'V -> 'U voption) -> state: MapNodeState<'K, 'V, 'U> -> bool

  /// Drain a set node and push the reduced output delta to its sinks.
  val inline drainSetPush:
    map: ('T -> 'U voption) -> state: SetNodeState<'T, 'U> -> unit

  /// Drain a plain set node (filter) and push the reduced output delta.
  val inline drainPlainSetPush:
    map: ('T -> 'T voption) -> state: SetNodeState<'T, 'T> -> unit

  /// Drain a map node and push the reduced output delta to its sinks.
  val inline drainMapPush:
    map: ('K -> 'V -> 'U voption) -> state: MapNodeState<'K, 'V, 'U> -> unit

  /// Initial load of a refcounted set node: build the internal state from a
  /// snapshot of the source view.
  val inline loadRefSet:
    map: ('T -> 'U voption) ->
    snapshot: HashSet<'T> ->
    state: SetNodeState<'T, 'U> ->
      unit

  /// Initial load of a plain set node (filter). See loadRefSet.
  val inline loadPlainSet:
    map: ('T -> 'T voption) ->
    snapshot: HashSet<'T> ->
    state: SetNodeState<'T, 'T> ->
      unit

  /// Initial load of a map node. See loadRefSet.
  val inline loadMap:
    map: ('K -> 'V -> 'U voption) ->
    snapshot: Dictionary<'K, 'V> ->
    state: MapNodeState<'K, 'V, 'U> ->
      unit

  /// <summary>Internal. Receives side-routed set deltas of a two-source node.</summary>
  type internal ITwoSetSinkTarget<'T> =
    abstract member OnSideDeltas:
      side: int * adds: 'T[] * addCount: int * rems: 'T[] * remCount: int ->
        unit

  /// <summary>
  /// Internal. A per-side set sink: appends the delivery to the side's journal.
  /// Two instances per two-source node (construction-time allocation only).
  /// </summary>
  type internal SideSetSink<'T> =
    new: target: obj * side: int -> SideSetSink<'T>
    interface ISetDeltaSink<'T>

  /// <summary>
  /// Internal. Routes a delivery to the node's side handler through a plain
  /// interface call.
  /// </summary>
  type internal ISideMapSinkTarget =
    abstract member OnSideDeltas:
      side: int * sets: obj * setCount: int * rems: obj * remCount: int -> unit

  type internal SideMapSink<'K, 'V> =
    new: target: ISideMapSinkTarget * side: int -> SideMapSink<'K, 'V>
    interface IMapDeltaSink<'K, 'V>

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
    /// Reused scratch for the net-delta post-pass.
    mutable Scratch: HashSet<'T>
  }

  module internal TwoSetState =
    val create<'T> : depCount: int -> TwoSetState<'T> when 'T: equality

  /// <summary>
  /// Process one side's journal of a two-source set node. Returns whether
  /// the output changed.
  /// </summary>
  val processTwoSide:
    op: TwoSetOp -> side: int -> state: TwoSetState<'T> -> bool
      when 'T: equality

  /// <summary>
  /// Net-delta post-pass for the two-source set producers: a batch must not
  /// carry the same element in both adds and rems.
  /// </summary>
  val inline netifyTwoSetDelta: state: TwoSetState<'T> -> unit when 'T: equality

  /// <summary>Drain both journals of a two-source set node. Returns whether the output changed.</summary>
  val inline drainTwoSet:
    op: TwoSetOp -> state: TwoSetState<'T> -> bool when 'T: equality

  /// <summary>
  /// Drain a two-source set node and push the reduced output delta to its
  /// sinks.
  /// </summary>
  val inline drainTwoSetPush:
    op: TwoSetOp -> state: TwoSetState<'T> -> unit when 'T: equality

  /// <summary>
  /// The concrete HashSet view of a set node when available (the hot path,
  /// zero allocation). All views in this implementation are HashSets
  /// (constants included - the port has no FrozenSet), so this is an
  /// identity unbox.
  /// </summary>
  val inline asHashSet: view: IReadOnlySet<'T> -> HashSet<'T>

  /// <summary>
  /// The concrete ResizeArray view of a list node when available (the hot
  /// path). All views in this implementation are ResizeArrays, so this is
  /// an identity unbox.
  /// </summary>
  val inline asResizeList: view: IReadOnlyList<'T> -> ResizeArray<'T>

  /// <summary>
  /// The concrete Dictionary view of a map node when available (the hot
  /// path, zero allocation). All views in this implementation are
  /// Dictionaries, so this is an identity unbox.
  /// </summary>
  val inline asDictionary:
    view: IReadOnlyDictionary<'K, 'V> -> Dictionary<'K, 'V>

  /// <summary>Initial load of a two-source set node: build the state from both source views.</summary>
  val loadTwoSet:
    op: TwoSetOp ->
    left: IAdaptiveSet<'T> ->
    right: IAdaptiveSet<'T> ->
    state: TwoSetState<'T> ->
      unit
      when 'T: equality

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
    /// Reused scratch for the net-delta post-pass.
    mutable Scratch: HashSet<'K>
    mutable Scratch2: HashSet<'K>
  }

  module internal Choose2State =
    val create<'K, 'V1, 'V2, 'V3> :
      depCount: int -> Choose2State<'K, 'V1, 'V2, 'V3> when 'K: equality

  /// <summary>
  /// Apply one output transition of a choose2 drain: compare with the stored
  /// output, emit the delta (with equal-value elision), update the output.
  /// Returns whether anything changed.
  /// </summary>
  val applyChoose2Out<'K, 'V1, 'V2, 'V3 when 'K: equality> :
    state: Choose2State<'K, 'V1, 'V2, 'V3> ->
    k: 'K ->
    newOut: 'V3 voption ->
      bool

  /// <summary>
  /// Process one side's journal of a choose2 node. The mapping is called only
  /// when at least one side has a value (FDA parity). Returns whether the
  /// output changed.
  /// </summary>
  val inline processChoose2Side:
    mapping: ('K -> 'V1 voption -> 'V2 voption -> 'V3 voption) ->
    side: int ->
    state: Choose2State<'K, 'V1, 'V2, 'V3> ->
      bool
      when 'K: equality

  /// <summary>
  /// Net-delta post-pass for choose2 producers: a batch must not carry the
  /// same key in both sets and rems.
  /// </summary>
  val inline netifyChoose2Delta:
    state: Choose2State<'K, 'V1, 'V2, 'V3> -> unit when 'K: equality

  /// <summary>Drain both journals of a choose2 node. Returns whether the output changed.</summary>
  val inline drainChoose2:
    mapping: ('K -> 'V1 voption -> 'V2 voption -> 'V3 voption) ->
    state: Choose2State<'K, 'V1, 'V2, 'V3> ->
      bool
      when 'K: equality

  /// <summary>
  /// Drain a choose2 node and push the reduced output delta to its sinks.
  /// </summary>
  val inline drainChoose2Push:
    mapping: ('K -> 'V1 voption -> 'V2 voption -> 'V3 voption) ->
    state: Choose2State<'K, 'V1, 'V2, 'V3> ->
      unit
      when 'K: equality

  /// <summary>Initial load of a choose2 node: merge both source snapshots through the mapping.</summary>
  val inline loadChoose2:
    mapping: ('K -> 'V1 voption -> 'V2 voption -> 'V3 voption) ->
    leftSnapshot: Dictionary<'K, 'V1> ->
    rightSnapshot: Dictionary<'K, 'V2> ->
    state: Choose2State<'K, 'V1, 'V2, 'V3> ->
      unit
      when 'K: equality

  /// <summary>
  /// Replace the state of a plain set node with <paramref name="next"/> and
  /// collect the diff as the output delta. Returns whether anything changed.
  /// </summary>
  val rebuildSetDiff:
    next: HashSet<'T> -> state: SetNodeState<'T, 'T> -> bool when 'T: equality

  /// <summary>
  /// Replace the state of a map node with <paramref name="next"/> and collect
  /// the diff as the output delta (equal values elided). Returns whether
  /// anything changed.
  /// </summary>
  val rebuildMapDiff<'K, 'V when 'K: equality> :
    next: Dictionary<'K, 'V> -> state: MapNodeState<'K, 'V, 'V> -> bool

  /// <summary>
  /// Replace the list content with <paramref name="next"/> and
  /// collect the positional diff as the output delta (prefix/suffix, the
  /// <c>ChangeableList.ApplyDiff</c> algorithm). Returns whether anything
  /// changed.
  /// </summary>
  val rebuildListDiff:
    next: ResizeArray<'T> -> data: ResizeArray<'T> -> out: ListDelta<'T> -> bool

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
    val create<'U> :
      node: IAdaptiveSet<'U> -> CollectEntry<'U> when 'U: equality

  /// <summary>Internal. Receives the side-routed deltas of one inner set.</summary>
  type internal ICollectTarget<'T, 'U> =
    abstract member OnInnerDeltas:
      key: 'T * adds: 'U[] * addCount: int * rems: 'U[] * remCount: int -> unit

  /// <summary>
  /// Internal. A per-element sink: routes an inner set's delivery to the
  /// entry of its source element.
  /// </summary>
  type internal CollectSink<'T, 'U> =
    new: target: ICollectTarget<'T, 'U> * key: 'T -> CollectSink<'T, 'U>
    interface ISetDeltaSink<'U>

  /// <summary>Internal. State of a collect node (PLAN.md Section 7.4).</summary>
  type internal CollectState<'T, 'U when 'T: equality and 'U: equality> = internal {
    mutable Version: int64
    mutable Sinks: SinkList
    mutable DepVersions: int64[]
    mutable Journal: SetDelta<'T>
    mutable Inner: Dictionary<'T, CollectEntry<'U>>
    mutable Global: RefCountedSet<'U>
    mutable OutDelta: SetDelta<'U>
    /// Reused scratch: prior presence of every output element touched this batch.
    mutable Scratch: Dictionary<'U, bool>
  }

  module internal CollectState =
    val create<'T, 'U> :
      depCount: int -> CollectState<'T, 'U> when 'T: equality and 'U: equality

  /// <summary>
  /// Drain a collect node: process the source journal and then every entry
  /// journal. Returns whether the output changed.
  /// </summary>
  val inline drainCollect:
    target: ICollectTarget<'T, 'U> ->
    mapping: ('T -> IAdaptiveSet<'U>) ->
    state: CollectState<'T, 'U> ->
      bool
      when 'T: equality and 'U: equality

  /// <summary>
  /// Drain a collect node and push the reduced output delta to its sinks.
  /// </summary>
  val inline drainCollectPush:
    target: ICollectTarget<'T, 'U> ->
    mapping: ('T -> IAdaptiveSet<'U>) ->
    state: CollectState<'T, 'U> ->
      unit
      when 'T: equality and 'U: equality

  /// <summary>
  /// Internal. State of a bind node over a scalar value (PLAN.md Section 7.4):
  /// one inner set, swapped when the value changes.
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
    val create<'U> : depCount: int -> BindSetState<'U> when 'U: equality

  /// <summary>
  /// Drain a bind set node: apply the inner journal to the output. Returns
  /// whether the output changed.
  /// </summary>
  val inline drainBindSet: state: BindSetState<'U> -> bool when 'U: equality

  /// <summary>
  /// Drain a bind set node and push the reduced output delta to its sinks.
  /// </summary>
  val inline drainBindSetPush: state: BindSetState<'U> -> unit when 'U: equality

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
    val create<'K, 'V> : depCount: int -> BindMapState<'K, 'V> when 'K: equality

  /// <summary>
  /// Drain a bind map node: apply the inner journal to the output. Returns
  /// whether the output changed.
  /// </summary>
  val inline drainBindMap: state: BindMapState<'K, 'V> -> bool when 'K: equality

  /// <summary>
  /// Drain a bind map node and push the reduced output delta to its sinks.
  /// </summary>
  val inline drainBindMapPush:
    state: BindMapState<'K, 'V> -> unit when 'K: equality
