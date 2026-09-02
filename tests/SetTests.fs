module SetTests

// ASet/CSet contracts against Mibo.Adaptive's public ASet/CSet surface.

open Mibo.Testing.QUnit
open Mibo.Signals

let contents(items: int array) : string =
  items |> Array.map(string) |> String.concat ","

QUnit.module'("CSet writes", ignore)

QUnit.test(
  "add inserts once, remove drops once",
  fun assert' ->
    let tags = CSet.empty<string>
    CSet.add "fire" tags
    assert'.strictEqual(Set.contains "fire" (CSet.force tags), true)
    CSet.add "fire" tags

    assert'.strictEqual(
      Set.count(CSet.force tags),
      1,
      "duplicate add is a no-op"
    )

    CSet.remove "fire" tags
    assert'.strictEqual(Set.contains "fire" (CSet.force tags), false)
    CSet.remove "fire" tags

    assert'.strictEqual(
      Set.count(CSet.force tags),
      0,
      "absent remove is a no-op"
    )
)

QUnit.test(
  "set replaces, updateTo skips equal targets",
  fun assert' ->
    let tags = CSet.ofSeq [ "a"; "b" ]
    CSet.set (Set [ "b"; "c" ]) tags
    assert'.strictEqual(Set.count(CSet.force tags), 2)
    assert'.strictEqual(Set.contains "c" (CSet.force tags), true)
    assert'.strictEqual(CSet.updateTo [ "b"; "c" ] tags, false)
    assert'.strictEqual(CSet.updateTo [ "b" ] tags, true)
)

QUnit.test(
  "unionWith, exceptWith and intersectWith mutate in one batch",
  fun assert' ->
    let tags = CSet.ofSeq [ 1; 2 ]
    CSet.unionWith [ 3; 4 ] tags
    let afterUnion = CSet.force tags
    assert'.strictEqual(Set.count afterUnion, 4)
    CSet.exceptWith [ 1; 4 ] tags
    let afterExcept = CSet.force tags
    assert'.strictEqual(Set.count afterExcept, 2)
    assert'.strictEqual(Set.contains 2 afterExcept, true)
    assert'.strictEqual(Set.contains 3 afterExcept, true)
    CSet.intersectWith [ 2; 9 ] tags
    let afterIntersect = CSet.force tags
    assert'.strictEqual(Set.count afterIntersect, 1)
    assert'.strictEqual(Set.contains 2 afterIntersect, true)
)

QUnit.test(
  "perform applies a builder batch atomically",
  fun assert' ->
    let tags = CSet.empty<string>
    let delta = SetDeltaBuilder<string>()
    delta.Add("a")
    delta.Add("a")
    delta.Remove("a")
    delta.Add("b")
    CSet.perform delta tags
    let current = CSet.force tags
    assert'.strictEqual(Set.count current, 1, "add+remove of a cancels")
    assert'.strictEqual(Set.contains "b" current, true)
)

QUnit.module'("ASet reads", ignore)

QUnit.test(
  "count follows the structure, force materializes a snapshot",
  fun assert' ->
    let tags = CSet.empty<string>
    let view = CSet.value tags
    let n = ASet.count view
    assert'.strictEqual(AVal.get n, 0)
    CSet.add "x" tags |> ignore
    assert'.strictEqual(AVal.get n, 1)

    let snapshot = ASet.force view
    CSet.add "y" tags |> ignore
    assert'.strictEqual(Set.count snapshot, 1, "snapshot is retained data")
    assert'.strictEqual(AVal.get n, 2)
)

QUnit.test(
  "packing inside a batch raises",
  fun assert' ->
    let tags = CSet.ofSeq [ "a" ]
    let view = CSet.value tags

    assert'.throws(
      (fun () -> Collections.batch(fun () -> ASet.force view |> ignore)),
      "pack inside a batch raises"
    )

    assert'.strictEqual(Set.count(ASet.force view), 1)
)

QUnit.module'("ASet custom and ofReader", ignore)

QUnit.test(
  "custom pulls a delta-builder compute on every read",
  fun assert' ->
    // The event queue stands in for the world: each read consumes one
    // event and reports it as add-or-remove against the current content.
    let events = ResizeArray([ "a"; "b"; "a" ])
    let sizes = ResizeArray<int>()

    let view =
      ASet.custom
        (fun (current: Set<string>) (builder: SetDeltaBuilder<string>) ->
          sizes.Add(Set.count current)

          if events.Count > 0 then
            let item = events.[0]
            events.RemoveAt(0)

            if Set.contains item current then
              builder.Remove(item)
            else
              builder.Add(item))

    assert'.strictEqual(Set.count(ASet.force view), 1, "first read adds a")
    assert'.strictEqual(Set.count(ASet.force view), 2, "second read adds b")
    assert'.strictEqual(Set.count(ASet.force view), 1, "third read removes a")
    assert'.strictEqual(sizes.[0], 0, "compute saw the previous content")
    assert'.strictEqual(sizes.[1], 1)
)

QUnit.test(
  "ofReader re-reads the world on every read",
  fun assert' ->
    let mutable world = Set [ 1 ]

    let view = ASet.ofReader(fun () -> world)

    assert'.strictEqual(Set.count(ASet.force view), 1)
    world <- Set [ 1; 2 ]

    assert'.strictEqual(
      Set.count(ASet.force view),
      2,
      "pull sees the new world"
    )
)

QUnit.module'("ASet derivations", ignore)

QUnit.test(
  "map derives a map keyed by the elements; sort orders them",
  fun assert' ->
    let numbers = CSet.ofSeq [ 3; 1 ]
    let labels = ASet.map (fun n -> n * 10) (CSet.value numbers)
    assert'.strictEqual(Map.count(AMap.force labels), 2)
    assert'.strictEqual(Map.find 3 (AMap.force labels), 30)

    let sorted = ASet.sort(CSet.value numbers)
    assert'.strictEqual(contents(AList.force sorted), "1,3")
    CSet.add 2 numbers |> ignore
    assert'.strictEqual(contents(AList.force sorted), "1,2,3", "sort follows")
)

QUnit.module'("CSet boundary intents", ignore)

QUnit.test(
  "postAdd applies at the next read; postSet supersedes the batch",
  fun assert' ->
    let tags = CSet.empty<string>
    CSet.postAdd "a" tags

    assert'.strictEqual(
      Set.contains "a" (CSet.force tags),
      true,
      "read applies"
    )

    CSet.postAdd "b" tags
    CSet.postSet [ "c" ] tags
    let current = CSet.force tags
    assert'.strictEqual(Set.count current, 1, "replace wins")
    assert'.strictEqual(Set.contains "c" current, true)
)

// ─── Ports of Mibo.Adaptive.Tests collection behaviors ───

QUnit.module'("Mibo.Adaptive parity", ignore)

QUnit.test(
  "ASet union updates with add and remove",
  fun assert' ->
    let left = CSet.ofSeq [ 1; 2 ]
    let right = CSet.ofSeq [ 2; 3 ]
    let unioned = ASet.union (CSet.value left) (CSet.value right)
    assert'.strictEqual(Set.count(ASet.force unioned), 3)

    CSet.add 4 left
    assert'.strictEqual(Set.count(ASet.force unioned), 4, "add propagates")

    CSet.remove 4 left
    assert'.strictEqual(Set.count(ASet.force unioned), 3, "remove propagates")
)

QUnit.test(
  "ASet union keeps an element present in either source",
  fun assert' ->
    let left = CSet.ofSeq [ 1; 2 ]
    let right = CSet.ofSeq [ 2; 3 ]
    let unioned = ASet.union (CSet.value left) (CSet.value right)
    assert'.strictEqual(Set.count(ASet.force unioned), 3)

    // Removing the shared element from one side keeps it in the union.
    CSet.remove 2 left
    assert'.strictEqual(Set.contains 2 (ASet.force unioned), true)

    // Removing it from both sides drops it.
    CSet.remove 2 right
    let current = ASet.force unioned
    assert'.strictEqual(Set.count current, 2)
    assert'.strictEqual(Set.contains 2 current, false)
)

QUnit.test(
  "ASet map responds to CSet.set",
  fun assert' ->
    let source = CSet.ofSeq [ 1; 2 ]
    let mapped = ASet.map (fun v -> v + 1) (CSet.value source)

    let initial = AMap.force mapped
    assert'.strictEqual(Map.count initial, 2)
    assert'.strictEqual(Map.find 1 initial, 2)

    CSet.set (Set [ 3; 4 ]) source
    let after = AMap.force mapped
    assert'.strictEqual(Map.count after, 2)
    assert'.strictEqual(Map.find 3 after, 4)
    assert'.strictEqual(Map.tryFind 1 after, ValueNone)
)

QUnit.test(
  "batch defers nothing to the reader: the net content lands after it",
  fun assert' ->
    let left = CSet.ofSeq [ 1; 2 ]
    let right = CSet.ofSeq [ 2; 3 ]
    let unioned = ASet.union (CSet.value left) (CSet.value right)

    Collections.batch(fun () ->
      CSet.set (Set [ 5 ]) left
      CSet.set (Set [ 6 ]) right)

    let after = ASet.force unioned
    assert'.strictEqual(Set.count after, 2)
    assert'.strictEqual(Set.contains 5 after, true)
    assert'.strictEqual(Set.contains 6 after, true)
)

QUnit.test(
  "ASet countBy and sums follow the elements",
  fun assert' ->
    let numbers = CSet.ofSeq [ 1; 2; 3; 4 ]
    let view = CSet.value numbers
    let evens = ASet.countBy (fun n -> n % 2 = 0) view
    let total = ASet.sumBy float view
    let avg = ASet.averageBy float view

    assert'.strictEqual(AVal.get evens, 2)
    assert'.strictEqual(AVal.get total, 10.0)
    assert'.strictEqual(AVal.get avg, 2.5)

    CSet.add 5 numbers
    assert'.strictEqual(AVal.get evens, 2)
    assert'.strictEqual(AVal.get total, 15.0)
)
