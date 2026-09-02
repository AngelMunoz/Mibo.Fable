module List.Tests

// Semantic tests for the ported adaptive lists: positional translation,
// incremental recomputation, refcounts on toASet, reduce/fold with rebuild
// fallback, transactions, posts.
//
// Content assertions compare comma-joined strings: typed arrays and F#
// lists serialize differently under deepEqual on JS.

open System
open System.Collections.Generic
open Fable.Core
open Mibo.Fable.Adaptive
open Mibo.Testing.QUnit

let private showItems(l: alist<int>) : string = String.Join(",", AList.force l)

let private showPairs(l: alist<struct (int * int)>) : string =
  l
  |> AList.force
  |> Seq.map(fun struct (a, b) -> sprintf "%d-%d" a b)
  |> String.concat ","

let private showMap(map: Dictionary<int, int>) : string =
  map
  |> Seq.map(fun kv -> sprintf "%d=%d" kv.Key kv.Value)
  |> Seq.sort
  |> String.concat ","

let private has(list: 'T array, item: 'T) =
  Array.exists (fun x -> x = item) list

QUnit.test(
  "AList reads, count, isEmpty, positional lookups",
  fun assert' ->
    let l = CList.ofSeq [ 10; 20; 30 ] |> CList.value

    assert'.equal(showItems l, "10,20,30", "force materializes")
    assert'.equal(AList.count l |> AVal.force, 3, "count")
    assert'.equal(AList.isEmpty l |> AVal.force, false, "isEmpty")
    assert'.equal(AList.tryAt 1 l |> AVal.force, ValueSome 20, "tryAt middle")

    assert'.equal(
      AList.tryAt 9 l |> AVal.force,
      ValueNone,
      "tryAt out of range"
    )

    assert'.equal(AList.tryFirst l |> AVal.force, ValueSome 10, "tryFirst")
    assert'.equal(AList.tryLast l |> AVal.force, ValueSome 30, "tryLast")
)

QUnit.test(
  "AList.map translates positions incrementally",
  fun assert' ->
    let src = CList.ofSeq [ 1; 2; 3 ]
    let mutable mapped = 0

    let derived =
      src
      |> CList.value
      |> AList.map(fun x ->
        mapped <- mapped + 1
        x * 10)

    assert'.equal(showItems derived, "10,20,30", "initial")

    CList.append 4 src

    assert'.equal(
      showItems derived,
      "10,20,30,40",
      "append translated to the output"
    )

    assert'.equal(mapped, 4, "only the new element ran the mapping")

    CList.prepend 0 src

    assert'.equal(
      showItems derived,
      "0,10,20,30,40",
      "prepend shifted the output"
    )

    assert'.equal(mapped, 5, "prepend mapped only the new element")
)

QUnit.test(
  "AList.filter/choose/mapi/indexed",
  fun assert' ->
    let l = CList.ofSeq [ 1; 2; 3; 4 ] |> CList.value

    let evens = AList.filter (fun x -> x % 2 = 0) l |> AList.force

    assert'.equal(String.Join(",", evens), "2,4", "filter")

    let picked =
      AList.choose (fun x -> if x > 2 then Some(x * 10) else None) l
      |> AList.force

    assert'.equal(String.Join(",", picked), "30,40", "choose")

    let at = AList.mapi (fun i x -> i * 100 + x) l |> AList.force

    assert'.equal(
      String.Join(",", at),
      "1,102,203,304",
      "mapi sees input positions"
    )

    let indexed = AList.indexed l |> AList.force

    assert'.equal(
      String.Join(
        ",",
        indexed |> Seq.map(fun struct (i, v) -> sprintf "%d:%d" i v)
      ),
      "0:1,1:2,2:3,3:4",
      "indexed pairs"
    )
)

QUnit.test(
  "AList rev/pairwise/sort/sub/take/skip",
  fun assert' ->
    let l = CList.ofSeq [ 1; 2; 3 ] |> CList.value

    assert'.equal(showItems(AList.rev l), "3,2,1", "rev")
    assert'.equal(showPairs(AList.pairwise l), "1-2,2-3", "pairwise")

    let shuffled = CList.ofSeq [ 3; 1; 2 ] |> CList.value

    assert'.equal(showItems(AList.sort shuffled), "1,2,3", "sort")
    assert'.equal(showItems(AList.sub 1 2 l), "2,3", "sub window")
    assert'.equal(showItems(AList.take 2 l), "1,2", "take")
    assert'.equal(showItems(AList.skip 1 l), "2,3", "skip")
)

QUnit.test(
  "AList.append concatenates with cross-source order",
  fun assert' ->
    let left = CList.ofSeq [ 1; 2 ]
    let right = CList.ofSeq [ 3; 4 ]

    let combined = AList.append (CList.value left) (CList.value right)

    assert'.equal(showItems combined, "1,2,3,4", "initial")

    CList.prepend 0 left

    assert'.equal(
      showItems combined,
      "0,1,2,3,4",
      "left prepend shifts everything"
    )

    CList.insertAt 1 99 right

    assert'.equal(
      showItems combined,
      "0,1,2,3,99,4",
      "right insert lands at its absolute position"
    )
)

QUnit.test(
  "AList.toASet dedups with refcounts",
  fun assert' ->
    let src = CList.ofSeq [ 1; 2; 1 ]

    let dedup = AList.toASet(CList.value src)

    assert'.equal(ASet.count dedup |> AVal.force, 2, "duplicates collapse")

    CList.remove 1 src

    assert'.equal(
      ASet.count dedup |> AVal.force,
      2,
      "first occurrence leaves, 1 survives"
    )

    CList.remove 1 src

    assert'.equal(
      ASet.count dedup |> AVal.force,
      1,
      "last occurrence drops the element"
    )
)

QUnit.test(
  "AList.ofAVal emits the positional diff",
  fun assert' ->
    let value = CVal.create [ 1; 2; 3 ]
    let derived = AList.ofAVal(CVal.value value)

    assert'.equal(showItems derived, "1,2,3", "initial")

    CVal.set [ 1; 3 ] value

    assert'.equal(showItems derived, "1,3", "middle removed in place")

    CVal.set [ 9; 1; 3 ] value

    assert'.equal(showItems derived, "9,1,3", "prepend diff")
)

QUnit.test(
  "AList.bind rebuilds on the value or the inner list",
  fun assert' ->
    let selected = CVal.create 0
    let a = CList.ofSeq [ 1; 2 ]
    let b = CList.ofSeq [ 7 ]

    let visible =
      AList.bind
        (fun i -> if i = 0 then CList.value a else CList.value b)
        (CVal.value selected)

    assert'.equal(showItems visible, "1,2", "bound to a")

    CVal.set 1 selected

    assert'.equal(showItems visible, "7", "swapped to b")

    CList.append 8 b

    assert'.equal(showItems visible, "7,8", "inner change propagates")
)

QUnit.test(
  "AList.reduce maintains state per delta with recompute fallback",
  fun assert' ->
    let src = CList.ofSeq [ 1; 2; 3 ]
    let sum = AList.sum(CList.value src)

    assert'.equal(AVal.force sum, 6, "initial sum")

    CList.append 4 src
    assert'.equal(AVal.force sum, 10, "append adds incrementally")

    CList.removeAt 0 src
    assert'.equal(AVal.force sum, 9, "remove subtracts (group sum inverts)")

    // fold recomputes on every removal (non-invertible).
    let chars = CList.ofSeq [ "a"; "b" ]
    let concatenated = AList.fold (fun acc x -> acc + x) "" (CList.value chars)

    assert'.equal(AVal.force concatenated, "ab", "fold")
    CList.append "c" chars
    assert'.equal(AVal.force concatenated, "abc", "fold adds incrementally")
    CList.removeAt 0 chars

    assert'.equal(
      AVal.force concatenated,
      "bc",
      "fold recomputed after the removal"
    )
)

QUnit.test(
  "CList transactions replay in order; appends keep write order",
  fun assert' ->
    let src = CList.ofSeq [ 1; 2; 3 ]
    let mutable recomputeCount = 0

    let derived =
      src
      |> CList.value
      |> AList.count
      |> AVal.map(fun c ->
        recomputeCount <- recomputeCount + 1
        c)

    AVal.force derived |> ignore
    let baseline = recomputeCount

    Transaction.run(fun () ->
      CList.append 4 src
      CList.append 5 src
      CList.removeAt 0 src
      CList.insertAt 0 0 src)

    AVal.force derived |> ignore

    // Replay: [1,2,3] +4 +5 -> [1,2,3,4,5]; removeAt 0 -> [2,3,4,5];
    // insertAt 0 0 -> [0,2,3,4,5]. One batch, one delta.
    assert'.equal(
      String.Join(",", src |> CList.force),
      "0,2,3,4,5",
      "transaction replayed in order"
    )

    assert'.equal(recomputeCount - baseline, 1, "one batch delta")
)

QUnit.test(
  "CList posts apply as one batch; appends resolve at apply time",
  fun assert' ->
    let src = CList.ofSeq [ 1 ]

    CList.postAppend 2 src
    CList.postAppend 3 src
    CList.postPrepend 0 src

    assert'.equal(
      String.Join(",", src |> CList.force),
      "0,1,2,3",
      "posted appends land in write order after the prepend"
    )

    let derived = src |> CList.value |> AList.count
    CList.postRemoveAt 0 src

    assert'.equal(AVal.force derived, 3, "posted remove applies at the read")
)

QUnit.test(
  "CList perform applies a builder batch atomically",
  fun assert' ->
    let src = CList.ofSeq [ 1; 2; 3 ]
    let builder = ListDeltaBuilder<int>()
    builder.Remove(0)
    builder.Insert(1, 9)

    CList.perform builder src

    assert'.equal(
      String.Join(",", src |> CList.force),
      "2,9,3",
      "ops applied in order"
    )
)

QUnit.test(
  "SetToList/MapToAList/AMap.ofAList roundtrip conversions",
  fun assert' ->
    let s = CSet.ofSeq [ 3; 1; 2 ] |> CSet.value

    let fromSet = AList.ofASet s

    assert'.ok(has(AList.force fromSet, 3), "contains 3")
    assert'.ok(has(AList.force fromSet, 1), "contains 1")
    assert'.ok(has(AList.force fromSet, 2), "contains 2")
    assert'.equal(AList.count fromSet |> AVal.force, 3, "same element count")

    let m = CMap.ofSeq [ 1, 10; 2, 20 ] |> CMap.value

    let entries = AMap.toAList m

    assert'.equal(AList.count entries |> AVal.force, 2, "map entries as a list")

    let backToMap = AMap.ofAList entries

    assert'.equal(
      showMap(AMap.force backToMap),
      "1=10,2=20",
      "ofAList roundtrip"
    )
)
