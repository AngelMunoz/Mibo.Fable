namespace Mibo.Signals

open System

// Implementation notes live in the signature file. Batching depth is one
// module-level counter — the JS runtime has a single thread, so no owner
// checks or write generations are needed.

type MapDelta<'K, 'V> = {
  Sets: ('K * 'V) array
  Removes: 'K array
}

type SetDelta<'T> = { Adds: 'T array; Removes: 'T array }

type ListOpKind =
  | ListInsert
  | ListRemove
  | ListUpdate

type ListOp<'T> = {
  Kind: ListOpKind
  Position: int
  Value: 'T
}

type ListDelta<'T> = { Operations: ListOp<'T> array }

type Unsubscriber(wrapper: unit -> unit) =
  interface IDisposable with
    member _.Dispose() = wrapper()

[<Class>]
type AMap<'K, 'V when 'K: comparison>
  (state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)>) =

  member val internal State = state

  // Set by the changeable owner so reads flush posted boundary intents.
  member val internal OnRead: (unit -> unit) = ignore with get, set

[<Class>]
type ASet<'T when 'T: comparison>(state: AVal<struct (int64 * Set<'T>)>) =

  member val internal State = state
  member val internal OnRead: (unit -> unit) = ignore with get, set

[<Class>]
type ListCells<'T>(order: int array, byId: Map<int, AVal<'T voption>>) =

  member _.Order = order
  member _.ById = byId

[<Class>]
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
