namespace Mibo.Fable.Adaptive

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
  val inline create<'T> : unit -> ListDelta<'T>
  /// Point-in-time copy: shares the buffer, copies the count.
  val inline copy<'T> : source: ListDelta<'T> -> ListDelta<'T>

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
