module MapTests

// AMap/CMap contracts against Mibo.Adaptive's public AMap/CMap surface.

open Mibo.Testing.QUnit
open Mibo.Signals

QUnit.module'("CMap writes", ignore)

QUnit.test(
  "addOrUpdate upserts; a repeated write leaves the value alone",
  fun assert' ->
    let users = CMap<string, int>()
    CMap.addOrUpdate "ana" 1 users
    assert'.strictEqual(CMap.tryGetValue "ana" users, ValueSome 1)

    CMap.addOrUpdate "ana" 1 users
    assert'.strictEqual(CMap.tryGetValue "ana" users, ValueSome 1, "no-op")

    CMap.addOrUpdate "ana" 2 users
    assert'.strictEqual(CMap.tryGetValue "ana" users, ValueSome 2)
)

QUnit.test(
  "set replaces the whole content, updateTo skips equal targets",
  fun assert' ->
    let users = CMap.ofSeq [ ("a", 1); ("b", 2) ]
    CMap.set (Map [ ("b", 20); ("c", 30) ]) users
    let current = CMap.force users
    assert'.strictEqual(Map.count current, 2)
    assert'.strictEqual(Map.find "c" current, 30)
    assert'.strictEqual(CMap.updateTo [ ("b", 20); ("c", 30) ] users, false)
    assert'.strictEqual(CMap.updateTo [ ("b", 20) ] users, true)
)

QUnit.test(
  "remove drops a key and is a no-op when absent",
  fun assert' ->
    let users = CMap.ofSeq [ ("a", 1) ]
    CMap.remove "a" users
    assert'.strictEqual(CMap.tryGetValue "a" users, ValueNone)
    CMap.remove "a" users
    assert'.strictEqual(Map.count(CMap.force users), 0)
    CMap.clear users
    assert'.strictEqual(Map.count(CMap.force users), 0)
)

QUnit.test(
  "perform applies a builder batch atomically",
  fun assert' ->
    let users = CMap<string, int>()
    let delta = MapDeltaBuilder<string, int>()
    delta.Set("a", 1)
    delta.Set("b", 2)
    delta.Set("b", 20)
    delta.Remove("a")
    CMap.perform delta users
    let current = CMap.force users
    assert'.strictEqual(Map.count current, 1)
    assert'.strictEqual(Map.find "b" current, 20)
)

QUnit.module'("AMap reads", ignore)

QUnit.test(
  "count follows structure only, tryFind is per-key precise",
  fun assert' ->
    let users = CMap<string, int>()
    let view = CMap.value users
    let n = AMap.count view
    let ana = AMap.tryFind "ana" view
    assert'.strictEqual(AVal.get n, 0)
    CMap.addOrUpdate "ana" 1 users |> ignore
    CMap.addOrUpdate "bob" 2 users |> ignore
    assert'.strictEqual(AVal.get n, 2)
    assert'.strictEqual(AVal.get ana, ValueSome 1)
    CMap.addOrUpdate "bob" 20 users |> ignore
    assert'.strictEqual(AVal.get n, 2, "value write does not move count")

    assert'.strictEqual(
      AVal.get ana,
      ValueSome 1,
      "other key's write is isolated"
    )
)

QUnit.test(
  "force materializes a snapshot; packing inside a batch raises",
  fun assert' ->
    let users = CMap.ofSeq [ ("a", 1) ]
    let view = CMap.value users
    let snapshot = AMap.force view
    CMap.addOrUpdate "a" 10 users |> ignore
    assert'.strictEqual(Map.find "a" snapshot, 1, "snapshot is retained data")

    assert'.throws(
      (fun () -> Collections.batch(fun () -> AMap.force view |> ignore)),
      "pack inside a batch raises"
    )

    assert'.strictEqual(Map.find "a" (AMap.force view), 10)
)

QUnit.module'("AMap custom", ignore)

QUnit.test(
  "custom pulls a delta-builder compute on every read",
  fun assert' ->
    // The event queue stands in for the world: each read consumes it.
    let events = ResizeArray([ ("a", 1); ("b", 2) ])
    let seen = ResizeArray<int>()

    let view =
      AMap.custom
        (fun (current: Map<string, int>) (builder: MapDeltaBuilder<string, int>) ->
          seen.Add(Map.count current)

          if events.Count > 0 then
            let (k, v) = events.[0]
            events.RemoveAt(0)
            builder.Set(k, v))

    assert'.strictEqual(Map.count(AMap.force view), 1, "first read consumes a")
    assert'.strictEqual(Map.count(AMap.force view), 2, "second read consumes b")
    assert'.strictEqual(Map.count(AMap.force view), 2, "no events, no change")
    assert'.strictEqual(seen.[0], 0, "compute saw the previous content")
    assert'.strictEqual(seen.[1], 1)
)

QUnit.module'("AMap derivations", ignore)

QUnit.test(
  "map and filter re-derive per entry",
  fun assert' ->
    let users = CMap.ofSeq [ ("a", 1); ("b", 2); ("c", 3) ]
    let view = CMap.value users
    let doubled = AMap.map (fun _ v -> v * 2) view
    let even = AMap.filter (fun _ v -> v % 2 = 0) view

    assert'.strictEqual(Map.count(AMap.force doubled), 3)
    assert'.strictEqual(Map.count(AMap.force even), 1)
    assert'.strictEqual(Map.find "c" (AMap.force doubled), 6)

    CMap.addOrUpdate "a" 10 users |> ignore
    assert'.strictEqual(Map.count(AMap.force even), 2, "filter follows writes")
)

QUnit.test(
  "union, intersect and difference follow the key sets",
  fun assert' ->
    let left = AMap.ofSeq [ ("a", 1); ("b", 2) ]
    let right = AMap.ofSeq [ ("b", 20); ("c", 3) ]

    assert'.strictEqual(Map.count(AMap.force(AMap.union left right)), 3)
    assert'.strictEqual(Map.find "b" (AMap.force(AMap.union left right)), 20)
    assert'.strictEqual(Map.count(AMap.force(AMap.intersect left right)), 1)
    assert'.strictEqual(Map.count(AMap.force(AMap.difference left right)), 1)

    assert'.strictEqual(
      Map.find "a" (AMap.force(AMap.difference left right)),
      1
    )
)

QUnit.test(
  "joinOn pairs outer entries with inner lookups",
  fun assert' ->
    let orders = AMap.ofSeq [ (1, "tea"); (2, "cup") ]
    let prices = AMap.ofSeq [ ("tea", 2.5); ("cup", 1.0) ]
    let priced = AMap.joinOn id orders prices
    let current = AMap.force priced
    assert'.strictEqual(Map.count current, 2)
    let struct (name, price) = Map.find 1 current
    assert'.strictEqual(name, "tea")
    assert'.strictEqual(price, 2.5)
)

QUnit.module'("AMap ofExternal", ignore)

QUnit.test(
  "snapshot runs at most once per invalidate",
  fun assert' ->
    let mutable current = Map [ ("a", 1) ]
    let mutable reads = 0

    let world, invalidate =
      AMap.ofExternal(fun () ->
        reads <- reads + 1
        current)

    assert'.strictEqual(Map.count(AMap.force world), 1)
    assert'.strictEqual(reads, 1)
    assert'.strictEqual(Map.count(AMap.force world), 1, "cached read")
    assert'.strictEqual(reads, 1)

    current <- Map [ ("a", 1); ("b", 2) ]
    invalidate()
    assert'.strictEqual(Map.count(AMap.force world), 2)
    assert'.strictEqual(reads, 2, "invalidate triggers exactly one snapshot")
)

QUnit.module'("CMap boundary intents", ignore)

QUnit.test(
  "post* applies at the next operation; postClear wins",
  fun assert' ->
    let users = CMap<string, int>()
    CMap.postAddOrUpdate "a" 1 users
    assert'.strictEqual(CMap.tryGetValue "a" users, ValueSome 1, "read applies")

    CMap.postAddOrUpdate "b" 2 users
    CMap.postClear users
    assert'.strictEqual(Map.count(CMap.force users), 0, "clear wipes the batch")
)

// ─── Ports of Mibo.Adaptive.Tests collection behaviors ───

let toMapString(map: Map<int, int>) : string =
  map |> Seq.map(fun kv -> sprintf "%d=%d" kv.Key kv.Value) |> String.concat ";"

QUnit.module'("Mibo.Adaptive parity", ignore)

QUnit.test(
  "AMap map and filter respond to updates",
  fun assert' ->
    let source = CMap.ofSeq [ (1, 10); (2, 20); (3, 30) ]
    let mapped = AMap.map (fun _ v -> v + 1) (CMap.value source)
    let filtered = AMap.filter (fun _ v -> v > 15) mapped

    // Initial: 2->21, 3->31 pass the filter.
    assert'.strictEqual(Map.count(AMap.force filtered), 2)
    assert'.strictEqual(Map.find 2 (AMap.force filtered), 21)
    assert'.strictEqual(Map.find 3 (AMap.force filtered), 31)

    CMap.addOrUpdate 4 40 source
    assert'.strictEqual(Map.count(AMap.force filtered), 3, "add flows through")
    assert'.strictEqual(Map.find 4 (AMap.force filtered), 41)

    CMap.remove 3 source

    assert'.strictEqual(
      Map.count(AMap.force filtered),
      2,
      "remove flows through"
    )

    assert'.strictEqual(Map.tryFind 3 (AMap.force filtered), ValueNone)
)

QUnit.test(
  "AMap filter ignores non-matching updates",
  fun assert' ->
    let source = CMap.ofSeq [ (1, 5); (2, 20) ]
    let filtered = AMap.filter (fun _ v -> v > 10) (CMap.value source)
    assert'.strictEqual(Map.count(AMap.force filtered), 1)

    CMap.addOrUpdate 1 8 source
    CMap.addOrUpdate 3 9 source

    let current = AMap.force filtered
    assert'.strictEqual(Map.count current, 1, "non-matching entries stay out")
    assert'.strictEqual(Map.tryFind 1 current, ValueNone)
    assert'.strictEqual(Map.tryFind 3 current, ValueNone)
)

QUnit.test(
  "AMap filter updates on removals and crossing writes",
  fun assert' ->
    let source = CMap.ofSeq [ (1, 10); (2, 20); (3, 30) ]
    let filtered = AMap.filter (fun _ v -> v >= 20) (CMap.value source)
    assert'.strictEqual(Map.count(AMap.force filtered), 2)

    CMap.remove 3 source
    assert'.strictEqual(Map.count(AMap.force filtered), 1)

    CMap.addOrUpdate 1 25 source
    let current = AMap.force filtered

    assert'.strictEqual(
      Map.count current,
      2,
      "write crossing the threshold enters"
    )

    assert'.strictEqual(Map.find 1 current, 25)
)

QUnit.test(
  "AMap filter drops entries when the value falls below the threshold",
  fun assert' ->
    let source = CMap.ofSeq [ (1, 5); (2, 15); (3, 25) ]
    let filtered = AMap.filter (fun _ v -> v >= 10) (CMap.value source)
    assert'.strictEqual(Map.count(AMap.force filtered), 2)

    CMap.addOrUpdate 2 8 source
    let current = AMap.force filtered
    assert'.strictEqual(Map.count current, 1)
    assert'.strictEqual(Map.tryFind 2 current, ValueNone)
)

QUnit.test(
  "AMap mapA follows entry avals and structural edits",
  fun assert' ->
    let m = CMap.ofSeq [ ("A", 1); ("B", 2); ("C", 3) ]
    let flag = CVal.create true

    let res =
      AMap.mapA
        (fun _ v -> AVal.map (fun f -> if f then v else -1) flag)
        (CMap.value m)

    assert'.strictEqual(Map.find "B" (AMap.force res), 2)

    // A scalar flip re-derives every entry (each cell tracks the flag).
    CVal.set false flag
    assert'.strictEqual(Map.find "A" (AMap.force res), -1)
    assert'.strictEqual(Map.find "C" (AMap.force res), -1)

    // A whole-map replace is picked up by the derived cells.
    CMap.set (Map [ ("A", 2); ("B", 4); ("C", 6) ]) m
    CVal.set true flag
    assert'.strictEqual(Map.find "B" (AMap.force res), 4)

    CMap.remove "B" m
    assert'.strictEqual(Map.count(AMap.force res), 2)

    CMap.addOrUpdate "D" 8 m
    assert'.strictEqual(Map.find "D" (AMap.force res), 8)
)

QUnit.test(
  "AMap map responds to CMap.set",
  fun assert' ->
    let source = CMap.ofSeq [ (1, 10); (2, 20) ]
    let mapped = AMap.map (fun k v -> v + k) (CMap.value source)
    assert'.strictEqual(Map.find 2 (AMap.force mapped), 22)

    CMap.set (Map [ (2, 5); (3, 7) ]) source
    let current = AMap.force mapped
    assert'.strictEqual(Map.count current, 2)
    assert'.strictEqual(Map.find 2 current, 7)
    assert'.strictEqual(Map.find 3 current, 10)
)

QUnit.test(
  "batch coalesces writes: the derived view recomputes once",
  fun assert' ->
    let source = CMap<string, int>()
    let filtered = AMap.filter (fun _ v -> v >= 10) (CMap.value source)
    let evaluations = ResizeArray<int>()

    let watch =
      AVal.computed(fun () ->
        let n = Map.count(AMap.force filtered)
        evaluations.Add n
        n)

    AVal.get watch |> ignore
    let before = evaluations.Count

    Collections.batch(fun () ->
      CMap.addOrUpdate "a" 5 source |> ignore
      CMap.addOrUpdate "b" 10 source |> ignore
      CMap.addOrUpdate "a" 50 source |> ignore)

    AVal.get watch |> ignore

    assert'.strictEqual(
      evaluations.Count - before,
      1,
      "one read after the batch re-derives once"
    )

    assert'.strictEqual(AVal.get watch, 2)
)
