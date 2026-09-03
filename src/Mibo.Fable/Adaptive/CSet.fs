module Mibo.Fable.Adaptive.CSet

open System
open System.Collections.Generic

/// <summary>An empty changeable set.</summary>
let inline empty<'T> = new cset<'T>(Seq.empty)

/// <summary>A changeable set with the given items.</summary>
let inline ofSeq(items: seq<'T>) = new cset<'T>(items)

/// <summary>Adds an element. No-op when already present.</summary>
let inline add (item: 'T) (set: cset<'T>) = set.Add item

/// <summary>Removes an element. No-op when absent.</summary>
let inline remove (item: 'T) (set: cset<'T>) = set.Remove item

/// <summary>
/// Posts an add (the <c>cval.Post</c> handoff pattern): queues the
/// operation and returns immediately. The queued operations apply at the
/// next graph operation (reads and writes auto-drain) or at
/// <c>Posting.pump</c>, as one batch: one net delta, one notification
/// delivery. A burst is coalesced into a single handoff.
/// </summary>
let inline postAdd (item: 'T) (set: cset<'T>) = set.PostAdd item

/// <summary>Posts a remove. See <see cref="postAdd"/> for the application contract.</summary>
let inline postRemove (item: 'T) (set: cset<'T>) = set.PostRemove item

/// <summary>
/// Posts a full replace. See <see cref="postAdd"/> for the application
/// contract; a posted replace supersedes the other ops of the same
/// pending batch (the transaction semantics of <see cref="set"/>).
/// </summary>
let inline postSet (value: Set<'T>) (set: cset<'T>) = set.PostSet value

/// <summary>Replaces the whole set.</summary>
let inline set (value: Set<'T>) (set: cset<'T>) = set.Set value

/// <summary>
/// Replaces the whole set and returns whether the content changed (FDA
/// <c>cset.UpdateTo</c> parity). An equal target marks nothing.
/// </summary>
let inline updateTo (target: seq<'T>) (set: cset<'T>) : bool =
  let targetSet = HashSet<'T>(target)
  let view = ASet.getValue set
  let mutable changed = view.Count <> targetSet.Count

  if not changed then
    for x in view do
      if not(targetSet.Contains x) then
        changed <- true

  if changed then
    set.Set target

  changed

/// <summary>
/// Applies a batch of set operations (FDA <c>cset.Perform</c> parity). The
/// batch is applied atomically: sinks receive one net delta. Adding and
/// removing the same element within the batch cancels.
/// </summary>
let perform (delta: SetDeltaBuilder<'T>) (set: cset<'T>) : unit =
  let d = delta.Snapshot()

  if not d.IsEmpty then
    Transaction.run(fun () ->
      let adds = d.Added

      for i in 0 .. d.AddedCount - 1 do
        set.Add adds[i] |> ignore

      let rems = d.Removed

      for i in 0 .. d.RemovedCount - 1 do
        set.Remove rems[i] |> ignore)

/// <summary>Adds all the given elements (FDA <c>cset.UnionWith</c> parity; one atomic batch).</summary>
let inline unionWith (other: seq<'T>) (set: cset<'T>) : unit =
  Transaction.run(fun () ->
    for x in other do
      set.Add x |> ignore)

/// <summary>Removes all the given elements (FDA <c>cset.ExceptWith</c> parity; one atomic batch).</summary>
let inline exceptWith (other: seq<'T>) (set: cset<'T>) : unit =
  Transaction.run(fun () ->
    for x in other do
      set.Remove x |> ignore)

/// <summary>Keeps only the elements also present in <c>other</c> (FDA <c>cset.IntersectWith</c> parity; one atomic batch).</summary>
let inline intersectWith (other: seq<'T>) (set: cset<'T>) : unit =
  Transaction.run(fun () ->
    let otherSet = HashSet<'T>(other)
    let view = ASet.getValue set

    for x in view do
      if not(otherSet.Contains x) then
        set.Remove x |> ignore)

/// <summary>Views the changeable set as an adaptive set.</summary>
let inline value(set: cset<'T>) : aset<'T> = set

/// <summary>Materializes the current state as an immutable snapshot.</summary>
let inline force(set: cset<'T>) : HashSet<'T> = ASet.force set

/// <summary>Materializes the F# <c>Set</c> counterpart.</summary>
let inline toSet(set: cset<'T>) : Set<'T> = ASet.toSet set
