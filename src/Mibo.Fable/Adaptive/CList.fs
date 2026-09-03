module Mibo.Fable.Adaptive.CList

open System
open System.Collections.Generic

/// <summary>An empty changeable list.</summary>
let inline empty<'T> : clist<'T> = new ChangeableList<'T>(Seq.empty)

/// <summary>A changeable list with the given items.</summary>
let inline ofSeq(items: seq<'T>) : clist<'T> = new ChangeableList<'T>(items)

/// <summary>A changeable list with the given items.</summary>
let inline ofArray(items: 'T[]) : clist<'T> = new ChangeableList<'T>(items)

/// <summary>A changeable list with the given items.</summary>
let inline ofList(items: 'T list) : clist<'T> = new ChangeableList<'T>(items)

/// <summary>Appends an element at the end of the list.</summary>
let inline append (value: 'T) (list: clist<'T>) = list.Append value

/// <summary>Inserts an element at the start of the list.</summary>
let inline prepend (value: 'T) (list: clist<'T>) = list.Prepend value

/// <summary>Inserts an element before the element currently at the position.</summary>
let inline insertAt (position: int) (value: 'T) (list: clist<'T>) =
  list.InsertAt(position, value)

/// <summary>Removes the element currently at the position.</summary>
let inline removeAt (position: int) (list: clist<'T>) = list.RemoveAt position

/// <summary>Replaces the element currently at the position.</summary>
let inline updateAt (position: int) (value: 'T) (list: clist<'T>) =
  list.UpdateAt(position, value)

/// <summary>Removes the first occurrence of the value. No-op when absent.</summary>
let inline remove (value: 'T) (list: clist<'T>) = list.Remove value

/// <summary>
/// Posts an append (the <c>cval.Post</c> handoff pattern): queues the
/// operation and returns immediately. The queued operations apply at the
/// next graph operation (reads and writes auto-drain) or at
/// <c>Posting.pump</c>, as one batch: one delta, one notification
/// delivery. A burst is coalesced into a single handoff. The positions of
/// the batch refer to the state built by its earlier ops and are validated
/// when the batch applies.
/// </summary>
let inline postAppend (value: 'T) (list: clist<'T>) = list.PostAppend value

/// <summary>Posts an insert at the start. See <see cref="postAppend"/> for the application contract.</summary>
let inline postPrepend (value: 'T) (list: clist<'T>) = list.PostPrepend value

/// <summary>Posts an insert before the element currently at the position. See <see cref="postAppend"/> for the application contract.</summary>
let inline postInsertAt (position: int) (value: 'T) (list: clist<'T>) =
  list.PostInsertAt(position, value)

/// <summary>Posts a remove at the position. See <see cref="postAppend"/> for the application contract.</summary>
let inline postRemoveAt (position: int) (list: clist<'T>) =
  list.PostRemoveAt position

/// <summary>Posts a replace at the position. See <see cref="postAppend"/> for the application contract.</summary>
let inline postUpdateAt (position: int) (value: 'T) (list: clist<'T>) =
  list.PostUpdateAt(position, value)

/// <summary>Posts a remove of the first occurrence of the value. See <see cref="postAppend"/> for the application contract.</summary>
let inline postRemove (value: 'T) (list: clist<'T>) = list.PostRemove value

/// <summary>Posts a clear. See <see cref="postAppend"/> for the application contract.</summary>
let inline postClear(list: clist<'T>) = list.PostClear()

/// <summary>
/// Posts a full replace. See <see cref="postAppend"/> for the application
/// contract; a posted replace supersedes the other ops of the same pending
/// batch (the transaction semantics of <see cref="set"/>).
/// </summary>
let inline postSet (values: seq<'T>) (list: clist<'T>) = list.PostSet values

/// <summary>Removes all elements.</summary>
let inline clear(list: clist<'T>) = list.Clear()

/// <summary>Replaces the whole list. Last-wins over the whole batch inside a transaction.</summary>
let inline set (values: seq<'T>) (list: clist<'T>) = list.Set values

/// <summary>
/// Replaces the whole list and returns whether the content changed (FDA
/// <c>clist.UpdateTo</c> parity). An equal target marks nothing.
/// </summary>
let inline updateTo (target: 'T[]) (list: clist<'T>) : bool =
  let view = Collections.asResizeList(AList.getValue list)
  let mutable changed = view.Count <> target.Length

  if not changed then
    let mutable i = 0

    while not changed && i < target.Length do
      if not(EqualityComparer<'T>.Default.Equals(view[i], target[i])) then
        changed <- true

      i <- i + 1

  if changed then
    list.Set target

  changed

/// <summary>
/// Applies a batch of list operations (FDA <c>clist.Perform</c> parity). The
/// operations are positional and applied in order; the batch is atomic
/// (sinks receive one delta).
/// </summary>
let perform (delta: ListDeltaBuilder<'T>) (list: clist<'T>) : unit =
  let d = delta.Snapshot()

  if not d.IsEmpty then
    Transaction.run(fun () ->
      let ops = d.Operations

      for i in 0 .. d.OperationCount - 1 do
        let op = ops[i]

        match op.Kind with
        | ListOpKind.Insert -> list.InsertAt(op.Position, op.Value)
        | ListOpKind.Remove -> list.RemoveAt op.Position
        | _ -> list.UpdateAt(op.Position, op.Value))

/// <summary>Appends all the given elements (FDA <c>clist.AddRange</c> parity; one atomic batch).</summary>
let inline addRange (items: seq<'T>) (list: clist<'T>) : unit =
  Transaction.run(fun () ->
    for x in items do
      list.Append x |> ignore)

/// <summary>Views the changeable list as an adaptive list.</summary>
let inline value(list: clist<'T>) : alist<'T> = list

/// <summary>Materializes the current state as an immutable array snapshot.</summary>
let inline force(list: clist<'T>) : 'T[] = AList.force list

/// <summary>Materializes the F# <c>list</c> counterpart.</summary>
let inline toList(list: clist<'T>) : 'T list = AList.toList list
