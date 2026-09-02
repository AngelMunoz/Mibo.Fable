module Set.Tests

// Semantic tests for the ported adaptive sets (Mibo.Adaptive Core/Collections
// behaviors on the web runtime): content, incremental recomputation, deltas,
// transactions, and posts. .NET-only surfaces (GC budgets, FrozenSet identity)
// stay out.

open System
open System.Collections.Generic
open Fable.Core
open Mibo.Fable.Adaptive
open Mibo.Testing.QUnit

let private sortToList(set: HashSet<int>) = set |> Seq.sort |> List.ofSeq

let private countSet(set: HashSet<int>) = set.Count

QUnit.test(
  "ASet force materializes and the gates read the source",
  fun assert' ->
    let s = CSet.ofSeq [ 3; 1; 2 ] |> CSet.value
    let forced = ASet.force s

    assert'.equal(countSet forced, 3, "force sees all elements")
    assert'.deepEqual(box(sortToList forced), box [ 1; 2; 3 ], "sorted content")
    assert'.equal(ASet.count s |> AVal.force, 3, "count is incremental")
    assert'.equal(ASet.isEmpty s |> AVal.force, false, "isEmpty")
    assert'.equal(ASet.contains 2 s |> AVal.force, true, "contains present")
    assert'.equal(ASet.contains 9 s |> AVal.force, false, "contains absent")
)

QUnit.test(
  "ASet.map maps only the elements a delta touched (incremental)",
  fun assert' ->
    let src = CSet.ofSeq [ 1; 2; 3 ]
    let mutable mapped = 0

    let derived =
      src
      |> CSet.value
      |> ASet.map(fun x ->
        mapped <- mapped + 1
        x * 10)

    assert'.equal(ASet.count derived |> AVal.force, 3, "initial content")
    let baseline = mapped

    CSet.add 4 src

    let head = derived |> ASet.force |> Seq.contains 40

    assert'.ok(head, "new element mapped")

    assert'.equal(
      mapped - baseline,
      1,
      "only the added element ran the mapping"
    )
)

QUnit.test(
  "ASet transactions net one delta: add+remove of the same element cancels",
  fun assert' ->
    let src = CSet.ofSeq [ 1; 2 ]
    let mutable recomputeCount = 0

    let derived =
      src
      |> CSet.value
      |> ASet.count
      |> AVal.map(fun c ->
        recomputeCount <- recomputeCount + 1
        c)

    AVal.force derived |> ignore
    let baseline = recomputeCount

    Transaction.run(fun () ->
      CSet.add 3 src
      CSet.remove 3 src
      CSet.add 4 src)

    assert'.equal(AVal.force derived, 3, "net content after the batch")
    assert'.equal(recomputeCount - baseline, 1, "one net delta, one recompute")
)

QUnit.test(
  "ASet union keeps refcounts: an element leaves only when both sides drop it",
  fun assert' ->
    let left = CSet.ofSeq [ 1; 2 ]
    let right = CSet.ofSeq [ 2; 3 ]

    let u = ASet.union (CSet.value left) (CSet.value right)

    assert'.deepEqual(
      box(u |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 1; 2; 3 ],
      "initial union"
    )

    CSet.remove 2 left

    assert'.ok(
      u |> ASet.force |> Seq.contains 2,
      "still present from the right side"
    )

    CSet.remove 2 right

    assert'.notOk(
      u |> ASet.force |> Seq.contains 2,
      "gone once both sides dropped it"
    )
)

QUnit.test(
  "ASet difference/intersect/xor produce the set algebra",
  fun assert' ->
    let l = CSet.ofSeq [ 1; 2; 3 ] |> CSet.value
    let r = CSet.ofSeq [ 2; 3; 4 ] |> CSet.value

    let contents(s: aset<int>) =
      s |> ASet.force |> Seq.sort |> List.ofSeq

    assert'.deepEqual(
      box(contents(ASet.difference l r)),
      box [ 1 ],
      "difference"
    )

    assert'.deepEqual(
      box(contents(ASet.intersect l r)),
      box [ 2; 3 ],
      "intersect"
    )

    assert'.deepEqual(box(contents(ASet.xor l r)), box [ 1; 4 ], "xor")
)

QUnit.test(
  "ASet.collect unions dynamic inner sets with refcounts",
  fun assert' ->
    let buckets = CSet.ofSeq [ 0; 1 ]
    let odds = CSet.ofSeq [ 1; 3 ]
    let evens = CSet.ofSeq [ 2 ]

    let all =
      buckets
      |> CSet.value
      |> ASet.collect(fun b ->
        if b % 2 = 0 then CSet.value evens else CSet.value odds)

    assert'.deepEqual(
      box(all |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 1; 2; 3 ],
      "1 from odds (bucket 1), 2 from evens, 3 from odds"
    )

    CSet.remove 3 odds

    assert'.notOk(
      all |> ASet.force |> Seq.contains 3,
      "inner removal propagates"
    )

    CSet.remove 1 buckets
    CSet.remove 1 odds

    assert'.notOk(
      all |> ASet.force |> Seq.contains 1,
      "element dies with its last reference"
    )
)

QUnit.test(
  "ASet.bind swaps the whole inner set when the value changes",
  fun assert' ->
    let selected = CVal.create 0
    let a = CSet.ofSeq [ 1; 2 ]
    let b = CSet.ofSeq [ 7 ]

    let visible =
      ASet.bind
        (fun i -> if i = 0 then CSet.value a else CSet.value b)
        (CVal.value selected)

    assert'.deepEqual(
      box(visible |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 1; 2 ],
      "bound to a"
    )

    CVal.set 1 selected

    assert'.deepEqual(
      box(visible |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 7 ],
      "swapped to b; the old inner no longer leaks"
    )

    CSet.add 99 a

    assert'.notOk(
      visible |> ASet.force |> Seq.contains 99,
      "unbound inner is detached"
    )
)

QUnit.test(
  "ASet.ofAVal emits the diff, not a full replace",
  fun assert' ->
    let value = CVal.create [ 1; 2 ]
    let derived = ASet.ofAVal(CVal.value value)
    let mutable recomputeCount = 0

    let counts =
      derived
      |> ASet.count
      |> AVal.map(fun c ->
        recomputeCount <- recomputeCount + 1
        c)

    AVal.force counts |> ignore
    let baseline = recomputeCount

    CVal.set [ 2; 3 ] value

    assert'.deepEqual(
      box(derived |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 2; 3 ],
      "rebuilt content"
    )

    AVal.force counts |> ignore

    assert'.equal(recomputeCount - baseline, 1, "one diff delivery downstream")
)

QUnit.test(
  "ASet.exists/forall/sum/reduce are delta-driven",
  fun assert' ->
    let src = CSet.ofSeq [ 1; 2; 3 ]
    let srcSet = CSet.value src

    assert'.equal(
      ASet.exists (fun x -> x = 2) srcSet |> AVal.force,
      true,
      "exists"
    )

    assert'.equal(
      ASet.forall (fun x -> x > 0) srcSet |> AVal.force,
      true,
      "forall"
    )

    assert'.equal(ASet.sum srcSet |> AVal.force, 6, "sum")

    let product = ASet.fold (fun acc x -> acc * x) 1 srcSet |> AVal.force

    assert'.equal(product, 6, "fold product")

    CSet.add 4 src |> ignore
    assert'.equal(ASet.sum srcSet |> AVal.force, 10, "sum is incremental")
)

QUnit.test(
  "CSet.perform applies a builder batch atomically",
  fun assert' ->
    let src = CSet.ofSeq [ 1; 2; 3 ]
    let builder = SetDeltaBuilder<int>()
    builder.Remove(1)
    builder.Add(1) // cancels
    builder.Add(4)

    CSet.perform builder src

    assert'.deepEqual(
      box(src |> CSet.force |> Seq.sort |> List.ofSeq),
      box [ 2; 3; 4 ],
      "add+remove of the same element cancels inside the batch"
    )
)

QUnit.test(
  "CSet posts apply as one batch at the next graph operation",
  fun assert' ->
    let src = CSet.ofSeq [ 1 ]
    let derived = src |> CSet.value |> ASet.count

    CSet.postAdd 2 src
    CSet.postAdd 3 src
    CSet.postRemove 1 src

    assert'.equal(
      AVal.force derived,
      2,
      "posts drained as one net delta at the read"
    )
)

QUnit.test(
  "ASet.custom drives content from a compute function",
  fun assert' ->
    let pending = ResizeArray<int>([ 1; 2 ])

    let custom =
      ASet.custom(fun (view: HashSet<int>) (builder: SetDeltaBuilder<int>) ->
        for x in pending do
          if not(view.Contains x) then
            builder.Add(x)

        pending.Clear())

    assert'.deepEqual(
      box(custom |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 1; 2 ],
      "first poll applies the batch"
    )

    pending.Add(3)

    assert'.deepEqual(
      box(custom |> ASet.force |> Seq.sort |> List.ofSeq),
      box [ 1; 2; 3 ],
      "next poll applies the new batch"
    )
)
