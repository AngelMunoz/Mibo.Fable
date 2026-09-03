namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

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
  let inline create<'T>() : ListDelta<'T> = { Ops = DeltaBuffer.create() }

  /// Point-in-time copy: shares the buffer, copies the count.
  let inline copy<'T>(source: ListDelta<'T>) : ListDelta<'T> = {
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
