module ListTests

// AList/CList contracts against Mibo.Adaptive's public AList/CList surface:
// stable-id element cells, positional writes, perform/custom batches.

open Mibo.Testing.QUnit
open Mibo.Signals

let contents(items: int array) : string =
  items |> Array.map(string) |> String.concat ","

QUnit.module'("CList writes", ignore)

QUnit.test(
  "append, prepend and insertAt place elements at the right positions",
  fun assert' ->
    let items = CList.empty<int>
    CList.append 2 items
    CList.prepend 1 items
    CList.insertAt 1 99 items
    assert'.strictEqual(contents(CList.force items), "1,99,2")
)

QUnit.test(
  "updateAt writes one position, removeAt drops it",
  fun assert' ->
    let items = CList.ofSeq [ 1; 2; 3 ]
    CList.updateAt 1 20 items
    assert'.strictEqual(contents(CList.force items), "1,20,3")

    assert'.throws((fun () -> CList.updateAt 7 20 items), "out of range raises")

    CList.removeAt 0 items
    assert'.strictEqual(contents(CList.force items), "20,3")
)

QUnit.test(
  "set replaces the whole content; updateTo skips equal targets",
  fun assert' ->
    let items = CList.ofSeq [ 1; 2 ]
    CList.set [ 9; 8; 7 ] items
    assert'.strictEqual(contents(CList.force items), "9,8,7")
    assert'.strictEqual(CList.updateTo [| 9; 8; 7 |] items, false)
    assert'.strictEqual(CList.updateTo [| 9 |] items, true)
)

QUnit.test(
  "remove drops the first match; postSet replaces at the boundary",
  fun assert' ->
    let items = CList.ofSeq [ "a"; "b"; "a" ]
    CList.remove "a" items
    assert'.strictEqual(Array.length(CList.force items), 2)
    CList.remove "zz" items
    assert'.strictEqual(Array.length(CList.force items), 2, "absent is a no-op")
    CList.postSet [ "x" ] items
    assert'.strictEqual(Array.length(CList.force items), 1, "replace wins")
    CList.postClear items
    assert'.strictEqual(Array.length(CList.force items), 0, "clear wins")
)

QUnit.test(
  "perform applies builder operations with per-op positions",
  fun assert' ->
    let items = CList.ofSeq [ 1; 2 ]
    let delta = ListDeltaBuilder<int>()
    delta.Insert(2, 3) // [1; 2; 3]
    delta.Update(0, 10) // [10; 2; 3]
    delta.Remove(1) // [10; 3]
    CList.perform delta items
    assert'.strictEqual(contents(CList.force items), "10,3")
)

QUnit.module'("AList reads", ignore)

QUnit.test(
  "tryAt reads by position and shifts with insertions",
  fun assert' ->
    let items = CList.ofSeq [ "a"; "b" ]
    let view = CList.value items
    assert'.strictEqual(AVal.get(AList.tryAt 0 view), ValueSome "a")
    CList.prepend "z" items

    assert'.strictEqual(
      AVal.get(AList.tryAt 0 view),
      ValueSome "z",
      "insert shifts"
    )

    assert'.strictEqual(AVal.get(AList.tryAt 2 view), ValueSome "b")
    assert'.strictEqual(AVal.get(AList.tryAt 5 view), ValueNone)
)

QUnit.test(
  "count follows structure; force materializes a retained snapshot",
  fun assert' ->
    let items = CList.ofSeq [ 1; 2 ]
    let view = CList.value items
    let n = AList.count view
    assert'.strictEqual(AVal.get n, 2)
    let snapshot = AList.force view
    CList.append 3 items
    assert'.strictEqual(contents(snapshot), "1,2", "snapshot is retained data")
    assert'.strictEqual(AVal.get n, 3)

    assert'.throws(
      (fun () -> Collections.batch(fun () -> AList.force view |> ignore)),
      "pack inside a batch raises"
    )
)

QUnit.module'("stable ids", ignore)

QUnit.test(
  "insertions never recompute existing mapped elements",
  fun assert' ->
    let items = CList.ofSeq [ "a"; "b" ]
    let evaluations = ResizeArray<string>()

    let mapped =
      AList.map
        (fun v ->
          evaluations.Add v
          v + "!")
        (CList.value items)

    assert'.strictEqual(Array.length(AList.force mapped), 2)
    let afterBuild = evaluations.Count
    CList.prepend "z" items
    assert'.strictEqual(Array.length(AList.force mapped), 3)

    assert'.strictEqual(
      evaluations.Count - afterBuild,
      1,
      "only the new element maps; existing ids keep their cells"
    )

    assert'.strictEqual(
      String.concat "," (Array.map string (AList.force mapped)),
      "z!,a!,b!"
    )
)

QUnit.module'("AList custom", ignore)

QUnit.test(
  "custom applies delta-builder operations to the stable ids",
  fun assert' ->
    // One queued insert per read: the compute consumes its own events.
    let events = ResizeArray([ 1; 2 ])
    let sizes = ResizeArray<int>()

    let view =
      AList.custom(fun (current: int array) (builder: ListDeltaBuilder<int>) ->
        sizes.Add(current.Length)

        if events.Count > 0 then
          let v = events.[0]
          events.RemoveAt(0)
          builder.Insert(current.Length, v))

    assert'.strictEqual(contents(AList.force view), "1")
    assert'.strictEqual(contents(AList.force view), "1,2")

    assert'.strictEqual(
      contents(AList.force view),
      "1,2",
      "no events, no change"
    )

    assert'.strictEqual(sizes.[0], 0)
    assert'.strictEqual(sizes.[1], 1)
)

QUnit.module'("AList derivations", ignore)

QUnit.test(
  "positional derivations re-derive on structural change",
  fun assert' ->
    let items = CList.ofSeq [ 3; 1; 2 ]
    let view = CList.value items
    assert'.strictEqual(contents(AList.force(AList.sort view)), "1,2,3")
    assert'.strictEqual(contents(AList.force(AList.rev view)), "2,1,3")
    assert'.strictEqual(contents(AList.force(AList.take 2 view)), "3,1")
    assert'.strictEqual(contents(AList.force(AList.skip 1 view)), "1,2")

    CList.append 0 items
    assert'.strictEqual(contents(AList.force(AList.sort view)), "0,1,2,3")

    let pairs = AList.force(AList.pairwise view)
    assert'.strictEqual(contents(pairs |> Array.map fst), "3,1,2")
)

QUnit.test(
  "append concatenates two lists; indexed maps positions",
  fun assert' ->
    let left = AList.ofSeq [ 1; 2 ]
    let right = AList.ofSeq [ 3 ]
    assert'.strictEqual(contents(AList.force(AList.append left right)), "1,2,3")

    let items = CList.ofSeq [ "a"; "b" ]
    let indexed = AList.toIndexedASet(CList.value items)
    assert'.strictEqual(Map.count(AMap.force indexed), 2)
    assert'.strictEqual(Map.find 1 (AMap.force indexed), "b")
)

QUnit.test(
  "aggregates re-run on any write",
  fun assert' ->
    let items = CList.ofSeq [ 1.0; 2.0 ]
    let total = AList.sum(CList.value items)
    assert'.strictEqual(AVal.get total, 3.0)
    CList.append 3.0 items
    assert'.strictEqual(AVal.get total, 6.0)
)
