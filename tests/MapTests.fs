module Map.Tests

// Semantic tests for the ported adaptive maps: content, incremental
// recomputation, per-key gates, groupBy live groups, transactions, posts.

open System
open System.Collections.Generic
open Fable.Core
open Mibo.Fable.Adaptive
open Mibo.Testing.QUnit

let private kvToList(map: Dictionary<int, int>) =
  map |> Seq.map(fun kv -> (kv.Key, kv.Value)) |> Seq.sort |> List.ofSeq

let private kvToListS(map: Dictionary<string, int>) =
  map |> Seq.map(fun kv -> (kv.Key, kv.Value)) |> Seq.sort |> List.ofSeq

QUnit.test(
  "AMap reads, count, isEmpty and per-key lookup",
  fun assert' ->
    let m = CMap.ofSeq [ 1, 10; 2, 20 ] |> CMap.value

    assert'.deepEqual(
      box(kvToList(AMap.force m)),
      box [ (1, 10); (2, 20) ],
      "force materializes"
    )

    assert'.equal(AMap.count m |> AVal.force, 2, "count")
    assert'.equal(AMap.isEmpty m |> AVal.force, false, "isEmpty")

    assert'.equal(
      AMap.tryFind 2 m |> AVal.force,
      ValueSome 20,
      "tryFind present"
    )

    assert'.equal(AMap.tryFind 9 m |> AVal.force, ValueNone, "tryFind absent")
)

QUnit.test(
  "AMap.map is incremental: unchanged entries never re-map",
  fun assert' ->
    let src = CMap.ofSeq [ 1, 10; 2, 20 ]
    let mutable mapped = 0

    let derived =
      src
      |> CMap.value
      |> AMap.map(fun k v ->
        mapped <- mapped + 1
        k + v)

    AMap.count derived |> AVal.force |> ignore
    let baseline = mapped

    CMap.addOrUpdate 3 30 src

    AMap.count derived |> AVal.force |> ignore

    assert'.equal(mapped - baseline, 1, "only the new entry ran the mapping")
)

QUnit.test(
  "AMap per-key lookup gates on its key only",
  fun assert' ->
    let src = CMap.ofSeq [ 1, 10; 2, 20 ]
    let mutable recomputeCount = 0

    let lookup =
      src
      |> CMap.value
      |> AMap.tryFind 1
      |> AVal.map(fun v ->
        recomputeCount <- recomputeCount + 1
        v)

    AVal.force lookup |> ignore
    let baseline = recomputeCount

    // An unrelated write must not change the watched value: the direct
    // consumer settles once (the dirty indicator), then holds.
    CMap.addOrUpdate 2 99 src
    AVal.force lookup |> ignore

    assert'.equal(AVal.force lookup, ValueSome 10, "watched value unchanged")

    let settled = recomputeCount
    AVal.force lookup |> ignore

    assert'.equal(recomputeCount - settled, 0, "no further recomputes")

    CMap.addOrUpdate 1 11 src

    assert'.equal(AVal.force lookup, ValueSome 11, "the watched key updates")
)

QUnit.test(
  "AMap.unionWith resolves collisions; intersect/difference compose",
  fun assert' ->
    let l = CMap.ofSeq [ 1, 10; 2, 20 ] |> CMap.value
    let r = CMap.ofSeq [ 2, 99; 3, 30 ] |> CMap.value

    let contents(m: amap<int, int>) = kvToList(AMap.force m)

    let u = AMap.unionWith (fun _ a b -> a + b) l r

    assert'.deepEqual(
      box(contents u),
      box [ (1, 10); (2, 119); (3, 30) ],
      "unionWith adds collisions"
    )

    let i = AMap.intersect l r

    assert'.deepEqual(
      box(i |> AMap.force |> Seq.map(fun kv -> kv.Key, kv.Value) |> List.ofSeq),
      box [ (2, struct (20, 99)) ],
      "intersect pairs both sides"
    )

    assert'.deepEqual(
      box(contents(AMap.difference l r)),
      box [ (1, 10) ],
      "difference keeps left-only keys"
    )
)

QUnit.test(
  "AMap ofASet/mapSet/toASet/keys convert with the right semantics",
  fun assert' ->
    let entries = CSet.ofSeq([ 1, 100; 2, 200 ] :> seq<int * int>)

    let keepAll = AMap.ofASet(CSet.value entries)

    assert'.deepEqual(
      box(
        keepAll
        |> AMap.force
        |> Seq.map(fun kv -> kv.Key, kv.Value.Count)
        |> List.ofSeq
      ),
      box [ (1, 1); (2, 1) ],
      "ofASet keeps per-key value sets"
    )

    let keys = AMap.keys(CMap.value(CMap.ofSeq [ 5, 50; 6, 60 ]))

    assert'.deepEqual(
      box(keys |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 5; 6 ],
      "keys set"
    )

    let pairs = AMap.toASet(CMap.value(CMap.ofSeq [ 1, 10 ]))

    assert'.deepEqual(
      box(pairs |> ASet.force |> List.ofSeq),
      box [ struct (1, 10) ],
      "toASet struct pairs"
    )
)

QUnit.test(
  "AMap.ofAVal replaces with a diff; AMap.bind swaps the inner map",
  fun assert' ->
    let value = CVal.create([ 1, "a" ] :> seq<int * string>)
    let fromValue = AMap.ofAVal(CVal.value value)

    assert'.equal(AMap.count fromValue |> AVal.force, 1, "initial")

    CVal.set ([ 1, "a"; 2, "b" ] :> seq<int * string>) value

    assert'.equal(AMap.count fromValue |> AVal.force, 2, "diff applied")

    let selected = CVal.create 0
    let table0 = CMap.ofSeq [ "x", 1 ]
    let table1 = CMap.ofSeq [ "y", 2 ]

    let visible =
      AMap.bind
        (fun i -> if i = 0 then CMap.value table0 else CMap.value table1)
        (CVal.value selected)

    assert'.deepEqual(
      box(kvToListS(AMap.force visible)),
      box [ ("x", 1) ],
      "bound to table0"
    )

    CVal.set 1 selected

    assert'.deepEqual(
      box(kvToListS(AMap.force visible)),
      box [ ("y", 2) ],
      "swapped to table1"
    )

    CMap.addOrUpdate "x" 77 table0

    assert'.notOk(
      (AMap.force visible).ContainsKey "x",
      "unbound inner detached"
    )
)

QUnit.test(
  "AMap.groupBy exposes live per-group maps",
  fun assert' ->
    let docs = CMap.ofSeq [ 1, "a"; 2, "b"; 3, "a" ]

    let byAuthor = AMap.groupBy (fun _ (doc: string) -> doc) (CMap.value docs)

    let groups = AMap.force byAuthor

    assert'.equal(groups.Count, 2, "two groups")

    let groupA = groups["a"]
    assert'.equal(AMap.count groupA |> AVal.force, 2, "group a holds two docs")

    CMap.addOrUpdate 4 "a" docs
    AMap.force byAuthor |> ignore // drain routes the delta into the child

    assert'.equal(AMap.count groupA |> AVal.force, 3, "group content is live")

    CMap.remove 1 docs
    CMap.remove 3 docs
    CMap.remove 4 docs

    assert'.equal((AMap.force byAuthor).Count, 1, "empty groups disappear")
)

QUnit.test(
  "CMap transactions net deltas; perform batches; posts coalesce",
  fun assert' ->
    let src = CMap.ofSeq [ 1, 10; 2, 20 ]
    let mutable recomputeCount = 0

    let derived =
      src
      |> CMap.value
      |> AMap.count
      |> AVal.map(fun c ->
        recomputeCount <- recomputeCount + 1
        c)

    AVal.force derived |> ignore
    let baseline = recomputeCount

    Transaction.run(fun () ->
      CMap.addOrUpdate 3 30 src
      CMap.remove 3 src
      CMap.addOrUpdate 1 11 src)

    assert'.deepEqual(
      box(kvToList(AMap.force(CMap.value src))),
      box [ (1, 11); (2, 20) ],
      "net content: 3 cancelled, 1 updated"
    )

    AVal.force derived |> ignore

    assert'.equal(recomputeCount - baseline, 1, "one net delta")

    let builder = MapDeltaBuilder<int, int>()
    builder.Set(4, 40)
    builder.Remove(9) // no-op
    CMap.perform builder src

    assert'.equal(AVal.force derived, 3, "perform applied the batch")

    CMap.postAddOrUpdate 5 50 src
    CMap.postRemove 4 src

    assert'.equal(AVal.force derived, 3, "posts applied at the read")
)

QUnit.test(
  "CMap.addOrUpdate skips equal writes at the source",
  fun assert' ->
    let src = CMap.ofSeq [ 1, 10 ]
    let mutable recomputeCount = 0

    let derived =
      src
      |> CMap.value
      |> AMap.count
      |> AVal.map(fun c ->
        recomputeCount <- recomputeCount + 1
        c)

    AVal.force derived |> ignore
    let baseline = recomputeCount

    CMap.addOrUpdate 1 10 src // equal: no-op

    assert'.equal(recomputeCount - baseline, 0, "equal write marks nothing")
)
