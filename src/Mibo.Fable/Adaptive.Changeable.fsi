namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Web port of Mibo.Adaptive's Core/Collections/Changeable.fs: the changeable
// collection sources. A source write updates the internal state, advances
// the version, and appends the net delta to the journal of every registered
// sink; processing happens on read (drain). Posting keeps the cval.Post
// handoff pattern with a per-node op queue (the .NET posted-op ring
// collapses into a plain FIFO — one thread, no tearing).

/// Internal. One pending posted operation on a changeable set: an element
/// add/remove, or a full replace (the op carries the whole new content).
type internal SetPostOp<'T> =
  | Add of item: 'T
  | Remove of item: 'T
  | Replace of content: seq<'T>

/// Internal. One pending posted operation on a changeable map.
type internal MapPostOp<'K, 'V> =
  | AddOrUpdate of key: 'K * value: 'V
  | Remove of key: 'K
  | Replace of content: seq<'K * 'V>

/// Internal. One pending posted operation on a changeable list. Insert with
/// position -1 appends at the replay-time end of the batch.
type internal ListPostOp<'T> =
  | Insert of position: int * value: 'T
  | RemoveAt of position: int
  | UpdateAt of position: int * value: 'T
  | RemoveValue of value: 'T
  | Replace of content: seq<'T>

/// <summary>
/// A changeable set: the writable source of an adaptive set.
/// </summary>
/// <remarks>
/// <para>
/// Writes inside a <c>Transaction.run</c> are journaled in order and applied at
/// commit as one net delta: adds and removes of the same element in one batch
/// cancel, and the last write wins. Reads inside a transaction see the
/// pre-transaction state.
/// </para>
/// <para>
/// <c>GetValue</c> returns a transient view of the internal state, valid only
/// until the next write. <c>CSet.force</c> materializes an immutable snapshot.
/// </para>
/// </remarks>
type ChangeableSet<'T> =
  new: initial: seq<'T> -> ChangeableSet<'T>
  /// <summary>Replaces the whole set. Supersedes the whole batch inside a
  /// transaction (later writes of the batch are discarded; matches the
  /// list).</summary>
  member Set: newValue: seq<'T> -> unit
  /// <summary>Adds an element. No-op when already present.</summary>
  member Add: item: 'T -> unit
  /// <summary>Removes an element. No-op when absent.</summary>
  member Remove: item: 'T -> unit
  /// <summary>
  /// Posts an add. The operation is queued and returns immediately; the
  /// queued operations apply at the next graph operation (reads and writes
  /// auto-drain) or at <c>Posting.pump</c>, as one batch: one net delta,
  /// one notification delivery, and a burst is coalesced into a single
  /// handoff.
  /// </summary>
  member PostAdd: item: 'T -> unit
  /// <summary>Posts a remove. See <see cref="PostAdd"/> for the application contract.</summary>
  member PostRemove: item: 'T -> unit
  /// <summary>
  /// Posts a full replace. See <see cref="PostAdd"/> for the application
  /// contract; a posted replace supersedes the other ops of the same
  /// pending batch (the transaction semantics of <see cref="Set"/>).
  /// </summary>
  member PostSet: newValue: seq<'T> -> unit
  /// Internal. Number of registered derived sinks (tests).
  member internal SinkCount: int
  interface ICommit
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry
  interface IPostSource

/// <summary>
/// A changeable map: the writable source of an adaptive map. See
/// <see cref="ChangeableSet&lt;'T&gt;"/> for the transaction and view contracts.
/// </summary>
type ChangeableMap<'K, 'V when 'K: equality> =
  new: initial: seq<'K * 'V> -> ChangeableMap<'K, 'V>
  /// <summary>Adds or updates an entry. No-op when the value is unchanged.</summary>
  member AddOrUpdate: key: 'K -> valueToSet: 'V -> unit
  /// <summary>Removes an entry. No-op when absent.</summary>
  member Remove: key: 'K -> unit
  /// <summary>Replaces the whole map. Supersedes the whole batch inside a
  /// transaction (later writes of the batch are discarded; matches the
  /// list).</summary>
  member Set: newValue: seq<'K * 'V> -> unit
  /// <summary>
  /// Posts an add or update. See <see cref="ChangeableSet&lt;'T&gt;.PostAdd"/>
  /// for the application contract.
  /// </summary>
  member PostAddOrUpdate: key: 'K -> valueToSet: 'V -> unit
  /// <summary>Posts a remove. See <see cref="PostAddOrUpdate"/> for the application contract.</summary>
  member PostRemove: key: 'K -> unit
  /// <summary>
  /// Posts a full replace. See <see cref="PostAddOrUpdate"/> for the
  /// application contract; a posted replace supersedes the other ops of the
  /// same pending batch.
  /// </summary>
  member PostSet: newValue: seq<'K * 'V> -> unit
  /// <summary>Posts a clear (a full replace with the empty map).</summary>
  member PostClear: unit -> unit
  /// Internal. Number of registered derived sinks (tests).
  member internal SinkCount: int
  interface ICommit
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry
  interface IPostSource

/// <summary>An abbreviation for <see cref="ChangeableSet&lt;'T&gt;"/> (FDA <c>cset&lt;'T&gt;</c> parity).</summary>
type cset<'T> = ChangeableSet<'T>

/// <summary>An abbreviation for <see cref="ChangeableMap&lt;'K,'V&gt;"/> (FDA <c>cmap&lt;'K,'V&gt;</c> parity).</summary>
type cmap<'K, 'V when 'K: equality> = ChangeableMap<'K, 'V>

/// <summary>
/// A changeable list: the writable source of an adaptive list. See
/// <see cref="ChangeableSet&lt;'T&gt;"/> for the view, transaction, and
/// disposal contracts.
/// </summary>
/// <remarks>
/// <para>
/// Positions are 0-based and refer to the list as of the previous operation of
/// the same batch. A full replace (<see cref="Set"/>) inside a transaction is
/// last-wins over the whole batch: it supersedes all other writes of the
/// batch. Reads inside a transaction see the pre-transaction list, so
/// positions inside a transaction refer to the pre-transaction list.
/// </para>
/// <para>
/// <c>GetValue</c> returns a transient view of the internal list, valid only
/// until the next write. <c>CList.force</c> materializes an immutable array
/// snapshot.
/// </para>
/// </remarks>
type ChangeableList<'T> =
  new: initial: seq<'T> -> ChangeableList<'T>
  /// <summary>Appends an element at the end of the list.</summary>
  member Append: value: 'T -> unit
  /// <summary>Inserts an element at the start of the list.</summary>
  member Prepend: value: 'T -> unit
  /// <summary>
  /// Inserts an element before the element currently at <c>position</c>.
  /// <c>position = Count</c> appends. Throws when out of range.
  /// </summary>
  member InsertAt: position: int * value: 'T -> unit
  /// <summary>Removes the element currently at <c>position</c>. Throws when out of range.</summary>
  member RemoveAt: position: int -> unit
  /// <summary>
  /// Replaces the element currently at <c>position</c>. No-op when the value
  /// is equal (equality at the source). Throws when out of range.
  /// </summary>
  member UpdateAt: position: int * value: 'T -> unit
  /// <summary>Removes the first occurrence of the value. No-op when absent. O(n) write-time scan.</summary>
  member Remove: value: 'T -> unit
  /// <summary>Removes all elements. The delta carries descending removes.</summary>
  member Clear: unit -> unit
  /// <summary>
  /// Replaces the whole list (prefix/suffix-trim diff). Last-wins over the
  /// whole batch inside a transaction: it supersedes all other writes of the
  /// batch.
  /// </summary>
  member Set: newValues: seq<'T> -> unit
  /// <summary>Posts an append. See the application contract on the member docs.</summary>
  member PostAppend: value: 'T -> unit
  /// <summary>Posts an insert at the start.</summary>
  member PostPrepend: value: 'T -> unit
  /// <summary>Posts an insert before the element currently at the position; validated when the batch applies.</summary>
  member PostInsertAt: position: int * value: 'T -> unit
  /// <summary>Posts a remove at the position; validated when the batch applies.</summary>
  member PostRemoveAt: position: int -> unit
  /// <summary>Posts a replace at the position; validated when the batch applies.</summary>
  member PostUpdateAt: position: int * value: 'T -> unit
  /// <summary>Posts a remove of the first occurrence of the value; the scan runs when the batch applies.</summary>
  member PostRemove: value: 'T -> unit
  /// <summary>Posts a clear.</summary>
  member PostClear: unit -> unit
  /// <summary>Posts a full replace; supersedes the other ops of the same pending batch.</summary>
  member PostSet: newValues: seq<'T> -> unit
  /// <summary>Gets the number of elements.</summary>
  member Count: int
  /// <summary>Gets whether the list is empty.</summary>
  member IsEmpty: bool
  /// <summary>Gets the element at the given position.</summary>
  member Item: position: int -> 'T with get
  /// <summary>Gets the element at the given position, or <c>ValueNone</c> when out of range.</summary>
  member TryGet: position: int -> 'T voption
  /// Internal. Number of registered derived sinks (tests).
  member internal SinkCount: int
  interface ICommit
  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry
  interface IPostSource

/// <summary>An abbreviation for <see cref="ChangeableList&lt;'T&gt;"/> (FDA <c>clist&lt;'T&gt;</c> parity).</summary>
type clist<'T> = ChangeableList<'T>
