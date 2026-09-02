module Adaptive.Tests

// Port of Mibo.Core.Tests/AdaptiveHeadlessTests.fs behaviors to the web
// runner (AdaptiveHeadless). .NET-only behaviors (GC allocation budgets,
// dedicated-thread Run/RunAsync) have no web counterpart and stay out.

open System
open Fable.Core
open Mibo.Fable.Exports
open Mibo.Testing.QUnit
open Mibo.Fable.Adaptive
open Mibo.Elmish

/// Builds a program with two independent roots and two counted projections.
/// Returns the roots, the recompute counters, and the frame force inputs.
let mkTestProgram() =
  let pos = CVal.create 0.0
  let vel = CVal.create 1.0
  let mutable posRecomputes = 0
  let mutable velRecomputes = 0

  let posProj =
    AVal.map
      (fun p ->
        posRecomputes <- posRecomputes + 1
        p)
      pos

  let velProj =
    AVal.map
      (fun v ->
        velRecomputes <- velRecomputes + 1
        v)
      vel

  let program =
    AdaptiveProgram.mkProgram
      (fun _ctx ->
        AdaptiveInit.ofFrameBuilder(fun () ->
          struct (AVal.getValue posProj, AVal.getValue velProj)))
      (fun _ctx _gameTime -> ())

  struct (program,
          pos,
          vel,
          (fun () -> posRecomputes),
          (fun () -> velRecomputes))

/// A program whose frame is the time root's total time in seconds.
let mkTimeProgram() =
  AdaptiveProgram.mkProgram
    (fun ctx ->
      let totalSeconds =
        AVal.map (fun (gt: GameTime) -> gt.TotalTime.TotalSeconds) ctx.Time

      AdaptiveInit.ofFrameBuilder(fun () -> AVal.getValue totalSeconds))
    (fun _ctx _gameTime -> ())

let ms(n: float) = TimeSpan.FromMilliseconds n

type AsyncMvuMsg =
  | Kick
  | Loaded of int

type AsyncMvuModel = { Loaded: int voption; Kicks: int }

let mkAsyncRunner() =
  let init(_: GameContext) =
    struct ({ Loaded = ValueNone; Kicks = 0 }, Cmd.none)

  let update (msg: AsyncMvuMsg) (model: AsyncMvuModel) =
    match msg with
    | Kick ->
      struct ({ model with Kicks = model.Kicks + 1 },
              Cmd.ofAsync
                (async {
                  do! Async.Sleep 20
                  return 7
                })
                Loaded
                (fun _ -> Loaded -1))
    | Loaded n -> struct ({ model with Loaded = ValueSome n }, Cmd.none)

  let program = HeadlessProgram.mkHeadless init update
  new HeadlessRunner<AsyncMvuModel, AsyncMvuMsg>(program, 100, 100)

QUnit.test(
  "Step returns the forced frame with current root values",
  fun assert' ->
    let struct (program, pos, _vel, _, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    CVal.set 42.0 pos
    runner.Step(ms 16.0)
    let struct (p, v) = runner.Frame

    assert'.strictEqual(p, 42.0, "Frame should reflect the written root")
    assert'.strictEqual(v, 1.0, "Unchanged root should keep its value")
)

QUnit.test(
  "Many writes between steps settle to one recompute per force",
  fun assert' ->
    let struct (program, pos, _vel, posRecomputes, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    runner.Step(ms 16.0)
    let baseline = posRecomputes()

    for i = 1 to 10 do
      CVal.set (float i) pos

    runner.Step(ms 16.0)

    assert'.strictEqual(
      posRecomputes() - baseline,
      1,
      "Ten writes before one force cost one recompute"
    )

    let struct (p, _) = runner.Frame
    assert'.strictEqual(p, 10.0, "Frame should reflect the last write")
)

QUnit.test(
  "Only the dirty fan recomputes",
  fun assert' ->
    let struct (program, pos, _vel, posRecomputes, velRecomputes) =
      mkTestProgram()

    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    runner.Step(ms 16.0)
    let posBefore = posRecomputes()
    let velBefore = velRecomputes()

    CVal.set 5.0 pos
    runner.Step(ms 16.0)

    assert'.strictEqual(
      posRecomputes() - posBefore,
      1,
      "Position fan recomputes once"
    )

    assert'.strictEqual(
      velRecomputes() - velBefore,
      0,
      "Velocity fan must not recompute"
    )
)

QUnit.test(
  "Idle step recomputes nothing",
  fun assert' ->
    let struct (program, _pos, _vel, posRecomputes, velRecomputes) =
      mkTestProgram()

    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    runner.Step(ms 16.0)
    let posBefore = posRecomputes()
    let velBefore = velRecomputes()

    for _ = 1 to 10 do
      runner.Step(ms 16.0)

    assert'.strictEqual(
      posRecomputes() - posBefore,
      0,
      "Idle steps must not recompute the position projection"
    )

    assert'.strictEqual(
      velRecomputes() - velBefore,
      0,
      "Idle steps must not recompute the velocity projection"
    )
)

QUnit.test(
  "Time root advances with each step",
  fun assert' ->
    use runner = new AdaptiveHeadless<float>(mkTimeProgram())

    runner.Step(ms 100.0)
    runner.Step(ms 200.0)
    runner.Step(ms 50.0)

    assert'.ok(
      abs runner.Frame - 0.35 < 1e-6,
      "Time root accumulates across steps"
    )

    assert'.ok(
      abs runner.GameTime.TotalTime.TotalSeconds - 0.35 < 1e-6,
      "Runner game time accumulates across steps"
    )
)

QUnit.test(
  "Negative delta is clamped to zero",
  fun assert' ->
    use runner = new AdaptiveHeadless<float>(mkTimeProgram())

    runner.Step(ms -16.0)

    assert'.strictEqual(runner.GameTime.TotalTime.TotalSeconds, 0.0)
)

QUnit.test(
  "Update phase runs once per step with the frame's game time",
  fun assert' ->
    let mutable updates = 0
    let mutable lastElapsed = TimeSpan.Zero
    let mutable observedFromTimeRoot = 0.0

    let program =
      AdaptiveProgram.mkProgram
        (fun ctx ->
          let elapsed =
            AVal.map
              (fun (gt: GameTime) -> gt.ElapsedGameTime.TotalMilliseconds)
              ctx.Time

          AdaptiveInit.ofFrameBuilder(fun () ->
            observedFromTimeRoot <- AVal.getValue elapsed
            AVal.getValue elapsed))
        (fun ctx gameTime ->
          updates <- updates + 1
          lastElapsed <- gameTime.ElapsedGameTime

          let gt = ctx.Time.Value
          observedFromTimeRoot <- gt.ElapsedGameTime.TotalMilliseconds)

    use runner = new AdaptiveHeadless<float>(program)

    runner.Step(ms 16.0)
    runner.Step(ms 33.0)

    assert'.strictEqual(updates, 2, "Update should run once per step")

    assert'.strictEqual(
      lastElapsed,
      ms 33.0,
      "Update receives the frame's elapsed"
    )

    assert'.strictEqual(
      observedFromTimeRoot,
      33.0,
      "Time root already holds the frame's time when Update runs"
    )
)

QUnit.test(
  "ExitRequested stops the runner; post-quit Step is a no-op",
  fun assert' ->
    let mutable exitCell = Unchecked.defaultof<cval<bool>>
    let pos = CVal.create 0.0

    let program =
      AdaptiveProgram.mkProgram
        (fun ctx ->
          exitCell <- ctx.ExitRequested
          AdaptiveInit.ofFrameBuilder(fun () -> pos.Value))
        (fun _ctx _gameTime -> ())

    use runner = new AdaptiveHeadless<float>(program)

    CVal.set 5.0 pos
    runner.Step(ms 16.0)
    let timeBeforeQuit = runner.GameTime

    assert'.notOk(runner.ShouldQuit, "Should not quit yet")
    assert'.strictEqual(runner.Frame, 5.0)

    CVal.set true exitCell
    runner.Step(ms 16.0)

    assert'.ok(runner.ShouldQuit, "Should quit after the request")

    assert'.strictEqual(
      runner.Frame,
      5.0,
      "Post-quit step returns the cached frame"
    )

    assert'.strictEqual(
      runner.GameTime,
      timeBeforeQuit,
      "Post-quit time does not advance"
    )
)

QUnit.test(
  "StepN advances N frames",
  fun assert' ->
    let struct (program, pos, _vel, _, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    CVal.set 9.0 pos
    runner.StepN(5, ms 16.0)

    let struct (p, _) = runner.Frame
    assert'.strictEqual(p, 9.0)
    assert'.strictEqual(runner.GameTime.TotalTime.TotalMilliseconds, 80.0)
)

QUnit.test(
  "StepN with count 0 leaves the graph untouched",
  fun assert' ->
    let struct (program, pos, _vel, posRecomputes, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    CVal.set 3.0 pos
    runner.StepN(0, ms 16.0)

    assert'.strictEqual(runner.GameTime.TotalTime.TotalMilliseconds, 0.0)
    let struct (p, _) = runner.Frame
    assert'.strictEqual(p, 3.0, "Frame reads the written root")
)

QUnit.test(
  "StepUntil stops when the frame predicate is met",
  fun assert' ->
    let struct (program, pos, _vel, _, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    CVal.set 5.0 pos
    let met = runner.StepUntil((fun (struct (p, _)) -> p >= 5.0), ms 16.0, 10)

    assert'.ok(met, "Predicate met within the budget")

    let struct (p, _) = runner.Frame
    assert'.strictEqual(p, 5.0)
)

QUnit.test(
  "StepUntil returns false when maxFrames reached",
  fun assert' ->
    let struct (program, _pos, _vel, _, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    let met = runner.StepUntil((fun _ -> false), ms 16.0, 4)

    assert'.notOk(met, "Predicate never met")
    assert'.strictEqual(runner.GameTime.TotalTime.TotalMilliseconds, 64.0)
)

QUnit.test(
  "FixedStep runs Update once per sub-step and forces the frame once",
  fun assert' ->
    let mutable updateCount = 0
    let mutable observedElapsed = 0.0
    let counter = CVal.create 0

    let program =
      AdaptiveProgram.mkProgram
        (fun _ctx -> AdaptiveInit.ofFrameBuilder(fun () -> counter.Value))
        (fun _ctx gameTime ->
          updateCount <- updateCount + 1

          observedElapsed <-
            observedElapsed + gameTime.ElapsedGameTime.TotalSeconds

          CVal.set (counter.Value + 1) counter)
      |> AdaptiveProgram.withFixedStep {
        StepSeconds = 0.01f
        MaxStepsPerFrame = 5
        MaxFrameSeconds = ValueNone
      }

    use runner = new AdaptiveHeadless<int>(program)
    runner.Step(ms 50.0)

    assert'.strictEqual(updateCount, 5, "Update runs once per sub-step")
    assert'.strictEqual(runner.Frame, 5, "Counter increments once per sub-step")

    assert'.ok(
      abs observedElapsed - 0.05 < 1e-6,
      "Sub-steps accumulate to the frame delta"
    )
)

QUnit.test(
  "FixedStep caps sub-steps at MaxStepsPerFrame",
  fun assert' ->
    let mutable updateCount = 0
    let counter = CVal.create 0

    let program =
      AdaptiveProgram.mkProgram
        (fun _ctx -> AdaptiveInit.ofFrameBuilder(fun () -> counter.Value))
        (fun _ctx _gameTime ->
          updateCount <- updateCount + 1
          CVal.set (counter.Value + 1) counter)
      |> AdaptiveProgram.withFixedStep {
        StepSeconds = 0.01f
        MaxStepsPerFrame = 3
        MaxFrameSeconds = ValueNone
      }

    use runner = new AdaptiveHeadless<int>(program)
    runner.Step(ms 50.0)

    assert'.strictEqual(updateCount, 3, "Update is capped at MaxStepsPerFrame")
    assert'.strictEqual(runner.Frame, 3)
)

QUnit.test(
  "FixedStep with no accumulated time runs zero sub-steps",
  fun assert' ->
    let mutable updateCount = 0
    let counter = CVal.create 0

    let program =
      AdaptiveProgram.mkProgram
        (fun _ctx -> AdaptiveInit.ofFrameBuilder(fun () -> counter.Value))
        (fun _ctx _gameTime ->
          updateCount <- updateCount + 1
          CVal.set (counter.Value + 1) counter)
      |> AdaptiveProgram.withFixedStep {
        StepSeconds = 0.05f
        MaxStepsPerFrame = 5
        MaxFrameSeconds = ValueNone
      }

    use runner = new AdaptiveHeadless<int>(program)
    runner.Step(ms 10.0)

    assert'.strictEqual(updateCount, 0, "No sub-step below one step delta")
    assert'.strictEqual(runner.Frame, 0)
)

QUnit.test(
  "withFixedStep rejects non-positive StepSeconds",
  fun assert' ->
    assert'.throws(fun () ->
      AdaptiveProgram.mkProgram
        (fun _ctx -> AdaptiveInit.ofFrameBuilder(fun () -> 0.0))
        (fun _ctx _gameTime -> ())
      |> AdaptiveProgram.withFixedStep {
        StepSeconds = 0.0f
        MaxStepsPerFrame = 5
        MaxFrameSeconds = ValueNone
      }
      |> ignore)
)

QUnit.test(
  "Intents posted during Init run exactly once at the next boundary",
  fun assert' ->
    let mutable runs = 0
    let counter = CVal.create 0

    let program =
      AdaptiveProgram.mkProgram
        (fun ctx ->
          ctx.Intents.PostNextFrame(fun () ->
            runs <- runs + 1
            CVal.set 7 counter)

          AdaptiveInit.ofFrameBuilder(fun () -> counter.Value))
        (fun _ctx _gameTime -> ())

    use runner = new AdaptiveHeadless<int>(program)
    runner.Step(ms 16.0)

    assert'.strictEqual(runs, 1, "Init intent ran once at the first boundary")
    assert'.strictEqual(runner.Frame, 7)
    runner.Step(ms 16.0)
    assert'.strictEqual(runs, 1, "Intent does not re-run on later steps")
)

QUnit.test(
  "Observer receives one notification per step",
  fun assert' ->
    let struct (program, _pos, _vel, _, _) = mkTestProgram()
    use runner = new AdaptiveHeadless<struct (float * float)>(program)

    let mutable observed = 0

    runner.Observe(fun (struct (_ctx, _frame, _gt)) -> observed <- observed + 1)

    runner.StepN(5, ms 16.0)
    assert'.strictEqual(observed, 5, "Observer fires once per step")
)

QUnit.testAsync(
  "PostTask completion re-enters via the post drain",
  fun assert' ->
    let counter = CVal.create 0

    let program =
      AdaptiveProgram.mkProgram
        (fun _ctx -> AdaptiveInit.ofFrameBuilder(fun () -> counter.Value))
        (fun _ctx _gameTime -> ())

    use runner = new AdaptiveHeadless<int>(program)
    runner.Step(ms 16.0)

    let taskResult = CVal.create 0

    runner.Intents.PostTask(
      (fun () -> Promise.create(fun resolve _ -> resolve 7)),
      (fun v -> CVal.set v taskResult)
    )

    runner.Step(ms 16.0)

    assert'.strictEqual(taskResult.Value, 0, "Pending task applied nothing yet")

    Promise.sleep 20
    |> Promise.tap(fun () ->
      runner.Step(ms 16.0)

      assert'.strictEqual(
        taskResult.Value,
        7,
        "Completion applied at the post drain"
      ))
)

QUnit.testAsync(
  "MVU Cmd.ofAsync dispatches on a later frame",
  fun assert' ->
    let asyncRunner = mkAsyncRunner()

    asyncRunner.Dispatch Kick
    asyncRunner.Step(ms 16.0)
    let m1 = model asyncRunner

    assert'.strictEqual(m1.Kicks, 1, "Kick processed")
    assert'.ok(m1.Loaded = ValueNone, "Async cmd has not completed yet")

    Promise.sleep 40
    |> Promise.tap(fun () ->
      asyncRunner.Step(ms 16.0)
      let m2 = model asyncRunner

      assert'.strictEqual(
        m2.Loaded,
        ValueSome 7,
        "Completion dispatched on a later frame"
      ))
)
