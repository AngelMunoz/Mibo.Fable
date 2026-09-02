namespace Mibo.Signals

open System

// Implementation notes live in the signature file. Batching depth is one
// module-level counter — the JS runtime has a single thread, so no owner
// checks or write generations are needed.

type ListOpKind =
  | ListInsert
  | ListRemove
  | ListUpdate

type ListOp<'T> = {
  Kind: ListOpKind
  Position: int
  Value: 'T
}

/// A mutable builder the <c>custom</c> computes of a set use to report the
/// change since their previous run: the compute appends the operations (for
/// example, by consuming its own event queue) and the node applies them.
type SetDeltaBuilder<'T>() =

  let adds = ResizeArray<'T>()
  let rems = ResizeArray<'T>()

  member _.Add(item: 'T) = adds.Add item
  member _.Remove(item: 'T) = rems.Add item
  member internal _.Adds = adds.ToArray()
  member internal _.Removes = rems.ToArray()

/// A mutable builder the <c>custom</c> computes of a map use to report the
/// change since their previous run. See <see cref="SetDeltaBuilder"/>.
type MapDeltaBuilder<'K, 'V>() =

  let sets = ResizeArray<struct ('K * 'V)>()
  let rems = ResizeArray<'K>()

  member _.Set(key: 'K, value: 'V) = sets.Add(struct (key, value))
  member _.Remove(key: 'K) = rems.Add key
  member internal _.Sets = sets.ToArray()
  member internal _.Removes = rems.ToArray()

/// A mutable builder the <c>custom</c> computes of a list use to report the
/// change since their previous run. Positions refer to the state as of the
/// previous operation. See <see cref="SetDeltaBuilder"/>.
type ListDeltaBuilder<'T>() =

  let ops = ResizeArray<ListOp<'T>>()

  member _.Insert(position: int, value: 'T) =
    ops.Add(
      {
        Kind = ListInsert
        Position = position
        Value = value
      }
    )

  member _.Remove(position: int) =
    ops.Add(
      {
        Kind = ListRemove
        Position = position
        Value = Unchecked.defaultof<'T>
      }
    )

  member _.Update(position: int, value: 'T) =
    ops.Add(
      {
        Kind = ListUpdate
        Position = position
        Value = value
      }
    )

  member internal _.Operations = ops.ToArray()

type AMap<'K, 'V when 'K: comparison>
  (state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)>) =

  member val internal State = state

  // Set by the changeable owner so reads flush posted boundary intents.
  member val internal OnRead: (unit -> unit) = ignore with get, set

type ASet<'T when 'T: comparison>(state: AVal<struct (int64 * Set<'T>)>) =

  member val internal State = state
  member val internal OnRead: (unit -> unit) = ignore with get, set

type ListCells<'T>(order: int array, byId: Map<int, AVal<'T voption>>) =

  member _.Order = order
  member _.ById = byId

type AList<'T>(state: AVal<struct (int64 * ListCells<'T>)>) =

  member val internal State = state
  member val internal OnRead: (unit -> unit) = ignore with get, set

[<RequireQualifiedAccess>]
module Collections =

  let mutable private batchDepth = 0

  let inBatch() = batchDepth > 0

  let batch(work: unit -> unit) =
    batchDepth <- batchDepth + 1

    try
      if batchDepth = 1 then
        // The outermost batch owns the preact batch: dependent effect
        // re-runs (and with them the delta sinks) flush when it ends.
        Signals.batch work
      else
        work()
    finally
      batchDepth <- batchDepth - 1

  let guardPack(operation: string) =
    if batchDepth > 0 then
      failwith(
        sprintf
          "%s packs the collection inside a batch. Materialize once per step, outside the update batch."
          operation
      )
