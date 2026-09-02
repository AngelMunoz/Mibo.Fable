module Map.Core.Smoke

// Core AMap/CMap contracts from ADAPTIVE_COLLECTIONS.md (dry-run findings
// 5, 6, 8, 9 and the write-trio decision). Run with: pnpm test:map

open Fable.Core
open Mibo.Testing.QUnit
open Mibo.Signals

type User = { Name: string; Age: int }

QUnit.module'("write trio", ignore)

QUnit.test(
  "add inserts a new key and refuses an existing one",
  fun assert' ->
    let users = CMap<string, int>()
    assert'.strictEqual(CMap.add "ana" 1 users, true)
    assert'.strictEqual(CMap.add "ana" 2 users, false, "existing key refuses")
    assert'.strictEqual(CMap.tryGetValue "ana" users, ValueSome 1)
)

QUnit.test(
  "set writes an existing key and refuses an absent one",
  fun assert' ->
    let users = CMap<string, int>()
    assert'.strictEqual(CMap.set "ana" 1 users, false, "absent key refuses")
    CMap.add "ana" 1 users |> ignore
    assert'.strictEqual(CMap.set "ana" 9 users, true)
    assert'.strictEqual(CMap.item "ana" users, 9)
)

QUnit.test(
  "addOrUpdate upserts and skips reference-equal-in-value writes",
  fun assert' ->
    let users = CMap.ofSeq [ ("ana", { Name = "Ana"; Age = 30 }) ]

    assert'.strictEqual(
      CMap.addOrUpdate "ana" { Name = "Ana"; Age = 31 } users,
      true
    )

    assert'.strictEqual(
      CMap.addOrUpdate "ana" { Name = "Ana"; Age = 31 } users,
      false
    )

    assert'.strictEqual(
      CMap.addOrUpdate "bob" { Name = "Bob"; Age = 20 } users,
      true
    )
)

QUnit.module'("reads", ignore)

QUnit.test(
  "count ignores value writes and follows structure only",
  fun assert' ->
    let users = CMap.empty<string, int>
    let n = AMap.count(CMap.value users)
    assert'.strictEqual(AVal.get n, 0)
    CMap.add "a" 1 users |> ignore
    CMap.add "b" 2 users |> ignore
    assert'.strictEqual(AVal.get n, 2)
    CMap.set "a" 100 users |> ignore
    assert'.strictEqual(AVal.get n, 2, "value write does not bump count")
    CMap.remove "a" users |> ignore
    assert'.strictEqual(AVal.get n, 1)
)

QUnit.test(
  "tryFind is per-key precise: other keys' writes do not recompute",
  fun assert' ->
    let users = CMap.empty<string, int>
    let evaluations = ResizeArray<int voption>()
    let ana = AMap.tryFind "ana" (CMap.value users)

    let watch =
      AVal.computed(fun () ->
        let v = AVal.get ana
        evaluations.Add v
        v)

    AVal.get watch |> ignore
    assert'.strictEqual(evaluations.Count, 1, "initial read")
    CMap.add "bob" 2 users |> ignore
    AVal.get watch |> ignore

    assert'.strictEqual(
      evaluations.Count,
      1,
      "unrelated key add does not recompute"
    )

    CMap.add "ana" 1 users |> ignore
    AVal.get watch |> ignore
    assert'.strictEqual(evaluations.Count, 2, "own key add recomputes")
    assert'.strictEqual(AVal.get ana, ValueSome 1)
    CMap.set "bob" 5 users |> ignore
    AVal.get watch |> ignore

    assert'.strictEqual(
      evaluations.Count,
      2,
      "unrelated value write does not recompute"
    )

    CMap.set "ana" 7 users |> ignore
    assert'.strictEqual(AVal.get ana, ValueSome 7, "own value write recomputes")
)

QUnit.test(
  "force and toMap materialize the current state",
  fun assert' ->
    let users = CMap.ofSeq [ ("a", 1); ("b", 2) ]
    let snapshot = AMap.force(CMap.value users)
    assert'.strictEqual(Map.count snapshot, 2)
    assert'.strictEqual(Map.find "b" snapshot, 2)
    CMap.set "b" 20 users |> ignore
    assert'.strictEqual(Map.find "b" snapshot, 2, "snapshot is retained data")
    assert'.strictEqual(Map.find "b" (AMap.toMap(CMap.value users)), 20)
)

QUnit.module'("batching and sinks", ignore)

QUnit.test(
  "one net delta per batch, immediate outside a batch",
  fun assert' ->
    let users = CMap.empty<string, int>
    let deltas = ResizeArray<MapDelta<string, int>>()
    let disposer = AMap.observe (fun d -> deltas.Add d) (CMap.value users)

    assert'.strictEqual(
      deltas.Count,
      1,
      "registration delivers initial content"
    )

    assert'.strictEqual(deltas.[0].Sets.Length, 0)

    Collections.batch(fun () ->
      CMap.add "a" 1 users |> ignore
      CMap.add "b" 2 users |> ignore
      CMap.addOrUpdate "a" 10 users |> ignore)

    assert'.strictEqual(deltas.Count, 2, "batch collapses to one delta")
    let batch = deltas.[1]
    assert'.strictEqual(batch.Sets.Length, 2, "add+update coalesce per key")
    assert'.strictEqual(snd batch.Sets.[0], 10)

    CMap.remove "b" users |> ignore
    assert'.strictEqual(deltas.Count, 3)
    assert'.deepEqual(deltas.[2].Removes, [| "b" |])

    disposer.Dispose()
    CMap.add "c" 3 users |> ignore
    assert'.strictEqual(deltas.Count, 3, "disposed sink stops notifying")
)

QUnit.test(
  "materializing inside a batch raises",
  fun assert' ->
    let users = CMap.ofSeq [ ("a", 1) ]
    let view = CMap.value users

    assert'.throws(
      (fun () -> Collections.batch(fun () -> AMap.force view |> ignore)),
      "pack inside a batch raises"
    )

    // outside the batch the same read is fine
    assert'.strictEqual(Map.count(AMap.force view), 1)
)

QUnit.module'("boundary intents", ignore)

QUnit.test(
  "post* applies at the next operation; postSet supersedes earlier intents",
  fun assert' ->
    let users = CMap.empty<string, int>
    CMap.postAddOrUpdate "a" 1 users

    assert'.strictEqual(
      CMap.tryGetValue "a" users,
      ValueSome 1,
      "read applies the intent"
    )

    CMap.postAddOrUpdate "b" 2 users
    CMap.postSet [ ("c", 3) ] users
    CMap.postClear users
    CMap.force users |> ignore

    assert'.strictEqual(
      Map.count(CMap.force users),
      0,
      "clear after replace wins"
    )
)

QUnit.module'("ofExternal", ignore)

QUnit.test(
  "snapshot runs at most once per invalidate and diffs against previous",
  fun assert' ->
    let mutable current = Map [ ("a", 1) ]
    let mutable reads = 0

    let world, invalidate =
      AMap.ofExternal(fun () ->
        reads <- reads + 1
        current)

    assert'.strictEqual(Map.count(AMap.force world), 1)
    assert'.strictEqual(reads, 1)

    assert'.strictEqual(
      Map.count(AMap.force world),
      1,
      "cached read does not re-snapshot"
    )

    assert'.strictEqual(reads, 1)

    current <- Map [ ("a", 1); ("b", 2) ]
    invalidate()
    assert'.strictEqual(Map.count(AMap.force world), 2)
    assert'.strictEqual(reads, 2, "invalidate triggers exactly one snapshot")
)
