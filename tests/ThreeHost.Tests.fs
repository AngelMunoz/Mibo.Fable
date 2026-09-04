module ThreeHost.Tests

open Mibo.Testing.QUnit
open Mibo.Fable.ThreeJS.ThreeHost

let private deltas(stamps: float array) : float array =
  let mutable last = ValueNone

  stamps
  |> Array.map(fun t ->
    let d = frameDelta last t
    last <- ValueSome t
    d)

let private stream60Hz start count =
  let step = 1000.0 / 60.0
  Array.init count (fun i -> start + float i * step)

QUnit.``module`` "ThreeHost frame stepping"

QUnit.test(
  "a 60 Hz frame stream steps the simulation in step with wall time",
  fun assert' ->
    let step = 1000.0 / 60.0
    let stamps = stream60Hz 1000.0 120
    let total = stamps |> deltas |> Array.sum
    let wall = stamps.[119] - stamps.[0] + step
    // The first frame steps the nominal 60 Hz delta; every later frame is real.
    let expected = wall - step + 16.6

    assert'.ok(
      abs(total - expected) < 0.001,
      sprintf
        "stepped %.4f ms, expected %.4f ms across %.4f ms of wall time"
        total
        expected
        wall
    )
)

QUnit.test(
  "a 144 Hz frame stream does not fast-forward the simulation",
  fun assert' ->
    let step = 1000.0 / 144.0
    let stamps = Array.init 120 (fun i -> 500.0 + float i * step)
    let total = stamps |> deltas |> Array.sum
    let wall = stamps.[119] - stamps.[0] + step
    let expected = wall - step + 16.6

    assert'.ok(
      abs(total - expected) < 0.001,
      sprintf
        "stepped %.4f ms, expected %.4f ms across %.4f ms of wall time"
        total
        expected
        wall
    )
)

QUnit.test(
  "a hidden tab gap steps at most 100 ms and leaves no catch-up debt",
  fun assert' ->
    let hz60 = 1000.0 / 60.0
    let gapEnd = 59.0 * hz60 + 8000.0

    let stamps =
      Array.concat [|
        stream60Hz 0.0 60
        [| gapEnd |]
        Array.init 59 (fun i -> gapEnd + float(i + 1) * hz60)
      |]

    let ds = deltas stamps
    assert'.equal(ds.[60], 100.0, "the gap frame clamps to 100 ms")

    assert'.ok(
      abs(ds.[61] - hz60) < 1.0e-9,
      "the next frame steps the real delta again"
    )
)
