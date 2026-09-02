module QUnit.Tests

// Harness check for the QUnit bindings over the Mibo.Signals surface.
// Run with: pnpm test:qunit

open Fable.Core
open Mibo.Testing.QUnit
open Mibo.Signals

QUnit.module'("QUnit bindings", ignore)

QUnit.test(
  "assert primitives",
  fun assert' ->
    assert'.ok(true, "ok passes on true")
    assert'.notOk(false, "notOk passes on false")
    assert'.equal(1 + 1, 2, "loose equality")
    assert'.strictEqual("a", "a", "strict equality")

    assert'.deepEqual(
      [| 1; 2; 3 |],
      [| 1; 2; 3 |],
      "structural array comparison"
    )

    assert'.throws((fun () -> failwith "boom"), "throws catches failures")
)

QUnit.testAsync(
  "async tests settle on the promise",
  fun assert' ->
    Async.StartAsPromise(async { assert'.ok(true, "async assertion") })
)

QUnit.module'("Signals", ignore)

QUnit.test(
  "writable roots notify computed views",
  fun assert' ->
    let source = CVal.create 1
    let doubled = AVal.map (fun v -> v * 2) source

    assert'.strictEqual(AVal.get doubled, 2, "initial derivation")
    CVal.set 21 source
    assert'.strictEqual(AVal.get doubled, 42, "derivation follows the write")
)

QUnit.test(
  "reads outside a write do not recompute",
  fun assert' ->
    let source = CVal.create 10
    let evaluations = ResizeArray<int>()

    let view =
      AVal.map
        (fun v ->
          evaluations.Add v
          v)
        source

    AVal.get view |> ignore
    AVal.get view |> ignore
    assert'.strictEqual(evaluations.Count, 1, "memoized after the first read")
    CVal.set 10 source
    CVal.set 10 source

    assert'.strictEqual(
      evaluations.Count,
      1,
      "no recompute when the write is reference-equal"
    )
)
