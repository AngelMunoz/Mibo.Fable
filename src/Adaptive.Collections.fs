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

/// Internal. JS WeakRef interop (System.WeakReference is not supported by
/// Fable). Deref normalizes undefined and null slots to null.
module internal WeakRefs =
  [<Emit("new WeakRef($0)")>]
  let create(target: obj) : obj = jsNative

  [<Emit("($0 && $0.deref()) || null")>]
  let deref(weakRef: obj) : obj = jsNative

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

  [<Emit("new WeakMap()")>]
  let committedVersions: IWeakMap = jsNative

  [<Emit("new WeakMap()")>]
  let setRegistries: IWeakMap = jsNative

  [<Emit("new WeakMap()")>]
  let mapRegistries: IWeakMap = jsNative

  [<Emit("new WeakMap()")>]
  let listRegistries: IWeakMap = jsNative

  [<Emit("\"CommittedVersion\" in $0")>]
  let hasCommittedVersion(x: obj) : bool = jsNative

  [<Emit("\"AddSetSink\" in $0")>]
  let hasSetRegistry(x: obj) : bool = jsNative

  [<Emit("\"AddMapSink\" in $0")>]
  let hasMapRegistry(x: obj) : bool = jsNative

  [<Emit("\"AddListSink\" in $0")>]
  let hasListRegistry(x: obj) : bool = jsNative

  /// Mark a node as a version-inflating node (implements ICommittedVersion).
  let inline markCommittedVersion(node: IAdaptiveObject) : unit =
    committedVersions.set(node, node)

  /// Mark a node as a set delta sink registry (implements ISetSinkRegistry).
  let inline markSetRegistry(node: obj) : unit = setRegistries.set(node, node)

  /// Mark a node as a map delta sink registry (implements IMapSinkRegistry).
  let inline markMapRegistry(node: obj) : unit = mapRegistries.set(node, node)

  /// Mark a node as a list delta sink registry (implements IListSinkRegistry).
  let inline markListRegistry(node: obj) : unit = listRegistries.set(node, node)

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

/// <summary>One reusable array plus count. Node-owned; grows amortized.</summary>
type internal DeltaBuffer<'T> = internal {
  mutable Items: 'T[]
  mutable Count: int
} with

  member this.IsEmpty = this.Count = 0

  member this.Clear() = this.Count <- 0

  /// Grow the array to hold at least n items. Amortized O(1); array growth only.
  member this.EnsureCapacity(n: int) : unit =
    if this.Items.Length < n then
      let next = Array.zeroCreate(max n (this.Items.Length * 2))
      Array.blit this.Items 0 next 0 this.Items.Length
      this.Items <- next

  /// Append one item to the buffer.
  member this.Append(item: 'T) : unit =
    this.EnsureCapacity(this.Count + 1)
    this.Items[this.Count] <- item
    this.Count <- this.Count + 1

  /// Append many items from a source array.
  member this.AppendRange(items: 'T[], count: int) : unit =
    this.EnsureCapacity(this.Count + count)
    Array.blit items 0 this.Items this.Count count
    this.Count <- this.Count + count

  /// Drop the entries whose key appears in <paramref name="keys" />,
  /// preserving order (shift-compact, one pass). Linear scan for small
  /// products (zero allocation); a hash set above the threshold.
  member this.RemoveKeys
    (keyOfItem: 'T -> 'K, keyOfKey: 'S -> 'K, keys: 'S[], keyCount: int)
    : unit =
    if keyCount > 0 && this.Count > 0 then
      let comparer = EqualityComparer<'K>.Default
      let mutable w = 0

      if int64 keyCount * int64 this.Count <= 4096L then
        for r in 0 .. this.Count - 1 do
          let item = this.Items[r]
          let key = keyOfItem item
          let mutable found = false
          let mutable j = 0

          while not found && j < keyCount do
            if comparer.Equals(key, keyOfKey keys[j]) then
              found <- true

            j <- j + 1

          if not found then
            this.Items[w] <- item
            w <- w + 1
      else
        let keySet = HashSet<'K>()

        for j in 0 .. keyCount - 1 do
          keySet.Add(keyOfKey keys[j]) |> ignore

        for r in 0 .. this.Count - 1 do
          let item = this.Items[r]

          if not(keySet.Contains(keyOfItem item)) then
            this.Items[w] <- item
            w <- w + 1

      this.Count <- w

  /// Compact in place: drop the first <c>doneCount</c> entries, keeping any
  /// entries appended after the captured start (reentrant writes survive).
  member this.Compact(doneCount: int) : unit =
    let live = this.Count

    if live > doneCount then
      Array.blit this.Items doneCount this.Items 0 (live - doneCount)
      this.Count <- live - doneCount
    else
      this.Count <- 0

module internal DeltaBuffer =
  let create<'T>() : DeltaBuffer<'T> = {
    Items = Array.zeroCreate 16
    Count = 0
  }

  /// Point-in-time copy: shares the buffer array, copies the count (the
  /// .NET struct-copy semantics of the original).
  let copy<'T>(source: DeltaBuffer<'T>) : DeltaBuffer<'T> = {
    Items = source.Items
    Count = source.Count
  }

/// <summary>
/// A set delta: elements added and removed since the previous delivery. The
/// buffers are transient: valid only during the delivery that received the
/// delta.
/// </summary>
type SetDelta<'T> = internal {
  mutable Adds: DeltaBuffer<'T>
  mutable Rems: DeltaBuffer<'T>
  // Shared in-drain flag (a one-slot array: the drains capture the
  // buffer references and counts, so a plain field would not be
  // visible to reentrant appends). Nonzero while a drain is
  // replaying this journal; the append-time cross-kind coalescing
  // (journalAppendSet) is suspended then, so it can never remove an
  // entry the in-flight drain is about to process.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member this.IsEmpty = this.Adds.IsEmpty && this.Rems.IsEmpty

  member internal this.Clear() =
    this.Adds.Clear()
    this.Rems.Clear()

  /// <summary>The elements added. Transient: valid during the callback only.</summary>
  member this.Added: 'T[] = this.Adds.Items

  /// <summary>The number of added elements.</summary>
  member this.AddedCount: int = this.Adds.Count

  /// <summary>The elements removed. Transient: valid during the callback only.</summary>
  member this.Removed: 'T[] = this.Rems.Items

  /// <summary>The number of removed elements.</summary>
  member this.RemovedCount: int = this.Rems.Count

  /// <summary>Appends an add operation. For <see cref="ASet.custom"/> computes.</summary>
  member this.Add(item: 'T) = this.Adds.Append item

  /// <summary>Appends a remove operation. For <see cref="ASet.custom"/> computes.</summary>
  member this.Remove(item: 'T) = this.Rems.Append item

module internal SetDelta =
  let create<'T>() : SetDelta<'T> = {
    Adds = DeltaBuffer.create()
    Rems = DeltaBuffer.create()
    InDrain = [| 0 |]
  }

  /// Point-in-time copy: shares the buffers, copies the counts.
  let copy<'T>(source: SetDelta<'T>) : SetDelta<'T> = {
    Adds = DeltaBuffer.copy source.Adds
    Rems = DeltaBuffer.copy source.Rems
    InDrain = [| 0 |]
  }

/// <summary>
/// A mutable delta builder for <see cref="ASet.custom"/> computes. The compute
/// receives the current view and this builder, appends the operations that
/// describe the change since the previous call, and returns. The builder is a
/// class: appends mutate the node's pending delta directly.
/// </summary>
type SetDeltaBuilder<'T>() =
  let adds = DeltaBuffer.create()
  let rems = DeltaBuffer.create()

  /// <summary>Appends an add operation.</summary>
  member _.Add(item: 'T) = adds.Append item

  /// <summary>Appends a remove operation.</summary>
  member _.Remove(item: 'T) = rems.Append item

  member internal _.IsEmpty = adds.IsEmpty && rems.IsEmpty

  member internal _.Clear() =
    adds.Clear()
    rems.Clear()

  member internal _.Adds = adds
  member internal _.Rems = rems

  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal this.Snapshot() : SetDelta<'T> =
    SetDelta.copy {
      Adds = adds
      Rems = rems
      InDrain = [| 0 |]
    }

/// <summary>
/// A map delta: upserted entries and removed keys since the previous
/// delivery. The buffers are transient: valid only during the delivery that
/// received the delta.
/// </summary>
type MapDelta<'K, 'V> = internal {
  mutable Sets: DeltaBuffer<struct ('K * 'V)>
  mutable Rems: DeltaBuffer<'K>
  // Shared in-drain flag; see SetDelta.InDrain.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member this.IsEmpty = this.Sets.IsEmpty && this.Rems.IsEmpty

  member internal this.Clear() =
    this.Sets.Clear()
    this.Rems.Clear()

  /// <summary>The entries set (added or updated). Transient: valid during the callback only.</summary>
  member this.SetEntries: struct ('K * 'V)[] = this.Sets.Items

  /// <summary>The number of entries set.</summary>
  member this.SetCount: int = this.Sets.Count

  /// <summary>The keys removed. Transient: valid during the callback only.</summary>
  member this.RemovedKeys: 'K[] = this.Rems.Items

  /// <summary>The number of keys removed.</summary>
  member this.RemovedCount: int = this.Rems.Count

  /// <summary>Appends an upsert operation. For <see cref="AMap.custom"/> computes.</summary>
  member this.Set(key: 'K, value: 'V) = this.Sets.Append(struct (key, value))

  /// <summary>Appends a remove operation. For <see cref="AMap.custom"/> computes.</summary>
  member this.Remove(key: 'K) = this.Rems.Append key

module internal MapDelta =
  let create<'K, 'V>() : MapDelta<'K, 'V> = {
    Sets = DeltaBuffer.create()
    Rems = DeltaBuffer.create()
    InDrain = [| 0 |]
  }

  /// Point-in-time copy: shares the buffers, copies the counts.
  let copy<'K, 'V>(source: MapDelta<'K, 'V>) : MapDelta<'K, 'V> = {
    Sets = DeltaBuffer.copy source.Sets
    Rems = DeltaBuffer.copy source.Rems
    InDrain = [| 0 |]
  }

/// <summary>
/// A mutable delta builder for <see cref="AMap.custom"/> computes. See
/// <see cref="SetDeltaBuilder&lt;'T&gt;"/> for the protocol.
/// </summary>
type MapDeltaBuilder<'K, 'V>() =
  let sets = DeltaBuffer.create()
  let rems = DeltaBuffer.create()

  /// <summary>Appends an upsert operation.</summary>
  member _.Set(key: 'K, value: 'V) = sets.Append(struct (key, value))

  /// <summary>Appends a remove operation.</summary>
  member _.Remove(key: 'K) = rems.Append key

  member internal _.IsEmpty = sets.IsEmpty && rems.IsEmpty

  member internal _.Clear() =
    sets.Clear()
    rems.Clear()

  member internal _.Sets = sets
  member internal _.Rems = rems

  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal this.Snapshot() : MapDelta<'K, 'V> =
    MapDelta.copy {
      Sets = sets
      Rems = rems
      InDrain = [| 0 |]
    }

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

  new(kind: ListOpKind, position: int, value: 'T, source: byte) =
    {
      Kind = kind
      Position = position
      Value = value
      Source = source
    }

/// <summary>
/// A list delta: ordered operations since the previous delivery. The buffer
/// is transient: valid only during the delivery that received the delta.
/// Order is the semantics: apply the operations sequentially.
/// </summary>
type ListDelta<'T> = internal {
  mutable Ops: DeltaBuffer<ListOp<'T>>
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member this.IsEmpty = this.Ops.Count = 0

  member internal this.Clear() = this.Ops.Clear()

  /// <summary>The operations, in application order. Transient: valid during the callback only.</summary>
  member this.Operations: ListOp<'T>[] = this.Ops.Items

  /// <summary>The number of operations.</summary>
  member this.OperationCount: int = this.Ops.Count

  /// <summary>Appends an insert operation. For <see cref="AList.custom"/> computes.</summary>
  member this.Insert(position: int, value: 'T) =
    this.Ops.Append(ListOp(ListOpKind.Insert, position, value, 0uy))

  /// <summary>Appends a remove operation. For <see cref="AList.custom"/> computes.</summary>
  member this.Remove(position: int) =
    this.Ops.Append(
      ListOp(ListOpKind.Remove, position, Unchecked.defaultof<'T>, 0uy)
    )

  /// <summary>Appends an update operation. For <see cref="AList.custom"/> computes.</summary>
  member this.Update(position: int, value: 'T) =
    this.Ops.Append(ListOp(ListOpKind.Update, position, value, 0uy))

module internal ListDelta =
  let create<'T>() : ListDelta<'T> = { Ops = DeltaBuffer.create() }

  /// Point-in-time copy: shares the buffer, copies the count.
  let copy<'T>(source: ListDelta<'T>) : ListDelta<'T> = {
    Ops = DeltaBuffer.copy source.Ops
  }

/// <summary>
/// A class-based delta builder for <see cref="AList.custom"/> computes.
/// </summary>
type ListDeltaBuilder<'T>() =
  let delta = ListDelta.create()

  member internal _.IsEmpty = delta.IsEmpty

  member internal this.Clear() = delta.Clear()

  member internal this.Snapshot() : ListDelta<'T> = ListDelta.copy delta

  /// <summary>Appends an insert operation. Positions refer to the state as of the previous operation.</summary>
  member this.Insert(position: int, value: 'T) = delta.Insert(position, value)

  /// <summary>Appends a remove operation. Positions refer to the state as of the previous operation.</summary>
  member this.Remove(position: int) = delta.Remove(position)

  /// <summary>Appends an update operation. Positions refer to the state as of the previous operation.</summary>
  member this.Update(position: int, value: 'T) = delta.Update(position, value)

/// <summary>Internal. Receives deltas from a list dependency.</summary>
type internal IListDeltaSink<'T> =
  abstract member OnDeltas: ops: ListOp<'T>[] * opCount: int -> unit

/// <summary>Internal. Register/unregister a list delta sink with a dependency.</summary>
type internal IListSinkRegistry =
  abstract member AddListSink: sink: obj -> unit
  abstract member RemoveListSink: sink: obj -> unit

/// <summary>
/// The sink list of a source. Entries are weak (a JS WeakRef): a derived node
/// the user dropped (and that is not observed) is collected, and delivery
/// skips its dead entry. A live sink is strongly reachable through its owner
/// (the user, an observation, or a downstream node), so delivery always
/// resolves it.
/// </summary>
type internal SinkList = internal {
  mutable Sinks: obj[]
  mutable Count: int
} with

  member this.IsEmpty = this.Count = 0

module internal SinkList =
  let create() : SinkList = {
    Sinks = Array.zeroCreate 4
    Count = 0
  }

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
  member this.Add(item: 'T) : bool =
    let mutable n = 0

    if this.Refcounts.TryGetValue(item, &n) then
      this.Refcounts[item] <- n + 1
      false
    else
      this.Refcounts[item] <- 1
      this.Data.Add item

  /// Remove one reference. Returns whether the element is fully removed.
  member this.Remove(item: 'T) : bool =
    let mutable n = 0

    if this.Refcounts.TryGetValue(item, &n) then
      if n = 1 then
        this.Refcounts.Remove item |> ignore
        this.Data.Remove item
      else
        this.Refcounts[item] <- n - 1
        false
    else
      false

module internal RefCountedSet =
  let create<'T when 'T: equality>() : RefCountedSet<'T> = {
    Data = HashSet<'T>()
    Refcounts = Dictionary<'T, int>()
  }

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
  let create<'T, 'U when 'U: equality>(depCount: int) : SetNodeState<'T, 'U> = {
    Version = 0L
    Sinks = SinkList.create()
    DepVersions = Array.zeroCreate depCount
    Set = RefCountedSet.create()
    Journal = SetDelta.create()
    Out = SetDelta.create()
  }

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
  let create<'K, 'V, 'U when 'K: equality>
    (depCount: int)
    : MapNodeState<'K, 'V, 'U> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Data = Dictionary<'K, 'U>()
      Journal = MapDelta.create()
      Out = MapDelta.create()
    }

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
  let create<'U>
    (aval: aval<'U voption>)
    (version: int64)
    (last: 'U voption)
    : ElementEntry<'U> =
    {
      Aval = aval
      Version = version
      Last = last
    }

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
