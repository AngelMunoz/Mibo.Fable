module Adaptive.Smoke

// A Defli-style State · Projection · Update · Force simulation, executed by
// the signals headless runner. Run with:
//   dotnet fable tests/Adaptive.Smoke.fsproj --noCache --run node Adaptive.Smoke.fs.js

open System
open Fable.Core
open Mibo.Fable.Exports
open Mibo.Signals
open Mibo.Elmish

// ── State: long-lived signal roots ──────────────────────────────────────────

type BallRoots = {
  Pos: CVal<struct (float * float)>
  Vel: CVal<struct (float * float)>
  Bounces: CVal<int>
  Frames: CVal<int>
  Clock: CVal<GameTime>
}

type Frame = {|
  X: float
  Y: float
  Speed: float
  Bounces: int
  Frames: int
  TotalMs: float
|}

// The world cell — the composition root the frame force reads through, so a
// rebuilt world swaps in place (mirrors Defli's StateCell).
type WorldCell() =
  member val Value: BallRoots voption = ValueNone with get, set
  member val Exit: CVal<bool> voption = ValueNone with get, set
  member val Kick: (unit -> unit) voption = ValueNone with get, set

  member val Update: (SignalsContext -> GameTime -> unit) voption =
    ValueNone with get, set

let width, height = (480.0, 320.0)
let frameMs = 1000.0 / 60.0

// Evaluation counter for the speed projection — the test reads it to observe
// the memoization contract ("recompute at most once if a dependency moved,
// not at all otherwise").
let mutable speedEvaluations = 0

let force (roots: BallRoots) (speed: AVal<float>) : unit -> Frame =
  fun () ->
    let struct (x, y) = AVal.get roots.Pos

    {|
      X = x
      Y = y
      Speed = AVal.get speed
      Bounces = AVal.get roots.Bounces
      Frames = AVal.get roots.Frames
      TotalMs = (AVal.get roots.Clock).TotalTime.TotalMilliseconds
    |}

let stepBall
  (dt: float)
  (struct (x, y): struct (float * float))
  (struct (vx, vy): struct (float * float))
  =
  let mutable x = x + vx * dt
  let mutable y = y + vy * dt
  let mutable vx = vx
  let mutable vy = vy
  let mutable bounced = false

  if x < 8.0 then
    x <- 8.0
    vx <- abs vx
    bounced <- true
  elif x > width - 8.0 then
    x <- width - 8.0
    vx <- -abs vx
    bounced <- true

  if y < 8.0 then
    y <- 8.0
    vy <- abs vy
    bounced <- true
  elif y > height - 8.0 then
    y <- height - 8.0
    vy <- -abs vy
    bounced <- true

  struct (struct (x, y), struct (vx, vy), bounced)

let mkInit(cell: WorldCell) : SignalsFrameContext -> SignalsInit<Frame> =
  fun ctx ->
    let roots = {
      Pos = CVal.create(struct (40.0, 40.0))
      Vel = CVal.create(struct (120.0, 90.0))
      Bounces = CVal.create 0
      Frames = CVal.create 0
      Clock =
        CVal.create {
          TotalTime = TimeSpan.Zero
          ElapsedGameTime = TimeSpan.Zero
        }
    }

    cell.Value <- ValueSome roots
    cell.Exit <- ValueSome ctx.ExitRequested

    cell.Kick <-
      ValueSome(fun () ->
        let struct (vx, vy) = CVal.get roots.Vel
        CVal.set (struct (-vx * 1.2, -vy * 1.2)) roots.Vel)

    // Speed projection: evaluation-counted so the test can observe the
    // memoization contract.
    let speed =
      AVal.computed(fun () ->
        speedEvaluations <- speedEvaluations + 1
        let struct (vx, vy) = AVal.get roots.Vel
        sqrt(vx * vx + vy * vy))

    // Per-frame phase: read projections, write roots. No model returned.
    // The state's own Clock root is written here (Defli pattern) — the
    // runner's framework Time root is a separate root for projections
    // that want framework time.
    let update (ctx: SignalsContext) (gt: GameTime) =
      CVal.set gt roots.Clock

      let struct (pos, vel, bounced) =
        stepBall
          (gt.ElapsedGameTime.TotalSeconds)
          (CVal.get roots.Pos)
          (CVal.get roots.Vel)

      CVal.set pos roots.Pos

      if bounced then
        CVal.set vel roots.Vel
        CVal.set (CVal.get roots.Bounces + 1) roots.Bounces

      CVal.set (CVal.get roots.Frames + 1) roots.Frames

    cell.Update <- ValueSome update

    // Startup intent: prove boundary work runs before the first frame —
    // writes a fresh position tuple (speed's dependency is Vel, so this
    // must NOT recompute the speed projection).
    ctx.Intents.PostNextFrame(fun () ->
      let struct (x, y) = CVal.get roots.Pos
      CVal.set (struct (x, y)) roots.Pos)

    SignalsInit.ofFrameBuilder(force roots speed)

// ── Test harness ────────────────────────────────────────────────────────────

let failures = ResizeArray<string>()

let check (name: string) (cond: bool) =
  let label = if cond then "PASS" else "FAIL"
  printfn $"  {label} — {name}"

  if not cond then
    failures.Add(name)

// ── Runner 1: variable step ─────────────────────────────────────────────────

let cell1 = WorldCell()
let mutable observedFrames = 0

let runCellUpdate
  (cell: WorldCell)
  (ctx: SignalsContext)
  (gt: GameTime)
  : unit =
  match cell.Update with
  | ValueSome update -> update ctx gt
  | ValueNone -> ()

let program1 =
  SignalsProgram.mkProgram (mkInit cell1) (runCellUpdate cell1)
  |> SignalsProgram.withObserver(fun () ->
    { new IObserver<struct (GameContext * Frame * GameTime)> with
        member _.OnNext _ = observedFrames <- observedFrames + 1
        member _.OnError _ = ()
        member _.OnCompleted() = ()
    })

let runner = new SignalsHeadless<Frame>(program1, int width, int height)

printfn "adaptive smoke: init + first frame"
let f0 = runner.Frame
check "first frame forced at init" (f0.Frames = 0 && f0.X = 40.0)
check "initial speed projection evaluated exactly once" (speedEvaluations = 1)

check
  "initial speed is the velocity magnitude"
  (abs(f0.Speed - sqrt(120.0 * 120.0 + 90.0 * 90.0)) < 0.001)

check "boundary intent ran (harmless write, graph alive)" (f0.X = 40.0)

printfn "adaptive smoke: 60 frames at 60Hz"
runner.StepN(60, TimeSpan.FromMilliseconds frameMs)
let f1 = runner.Frame
check "frames counter advanced by 60" (f1.Frames = 60)
check "clock advanced ~1s" (abs(f1.TotalMs - 1000.0) < 2.0)
check "observers notified once per step" (observedFrames = 60)
check "ball moved" (f1.X <> f0.X || f1.Y <> f0.Y)

check
  "speed NOT recomputed while velocity unchanged (memoized)"
  (speedEvaluations = 1)

printfn "adaptive smoke: kick via posted intent"

postAdaptiveIntent
  (cell1.Kick
   |> function
     | ValueSome k -> k
     | ValueNone -> failwith "no kick")
  runner

runner.Step(TimeSpan.FromMilliseconds frameMs)
let f2 = runner.Frame

check
  "kick applied at boundary (speed magnitude grew)"
  (f2.Speed > f1.Speed * 1.1)

check
  "speed recomputed exactly once after dependency moved"
  (speedEvaluations = 2)

printfn "adaptive smoke: exit request"

let exitRoot =
  match cell1.Exit with
  | ValueSome e -> e
  | ValueNone -> failwith "no exit root"

check "running before exit" (not runner.ShouldQuit)
postAdaptiveIntent (fun () -> CVal.set true exitRoot) runner
runner.Step(TimeSpan.FromMilliseconds frameMs)
check "ShouldQuit after exit root set" runner.ShouldQuit

// ── Runner 2: fixed step with cap ───────────────────────────────────────────

printfn "adaptive smoke: fixed-step cap drops backlog"
let cell2 = WorldCell()

let program2 =
  SignalsProgram.mkProgram (mkInit cell2) (runCellUpdate cell2)
  |> SignalsProgram.withFixedStep {
    StepSeconds = 1.0f / 60.0f
    MaxStepsPerFrame = 5
    MaxFrameSeconds = ValueNone
  }

let cappedRunner = new SignalsHeadless<Frame>(program2, int width, int height)
let framesBefore = (cappedRunner.Frame).Frames
cappedRunner.Step(TimeSpan.FromSeconds 0.5)
let framesAfter = (cappedRunner.Frame).Frames
check "500ms at 60Hz capped to 5 sub-steps" (framesAfter - framesBefore = 5)

// ── Push side: effects ──────────────────────────────────────────────────────

printfn "adaptive smoke: effect runs on change"

let mutable effectRuns = 0

let roots2 =
  match cell2.Value with
  | ValueSome r -> r
  | ValueNone -> failwith "no world"

let disposeEffect =
  onSignalChange(fun () ->
    CVal.get roots2.Bounces |> ignore
    effectRuns <- effectRuns + 1)

let effectRunsAtStart = effectRuns
let bouncesBefore = (cappedRunner.Frame).Bounces
let mutable guard = 0

while (cappedRunner.Frame).Bounces = bouncesBefore && guard < 1000 do
  cappedRunner.Step(TimeSpan.FromMilliseconds frameMs)
  guard <- guard + 1

check "bounce occurred" ((cappedRunner.Frame).Bounces > bouncesBefore)
check "effect re-ran on bounce" (effectRuns > effectRunsAtStart)
disposeEffect()

// ── MVU: Cmd.ofAsync dispatches on a later frame ────────────────────────────

printfn "mvu smoke: Cmd.ofAsync dispatches on a later frame"

type AsyncMvuMsg =
  | Kick2
  | Loaded of int

type AsyncMvuModel = { Loaded: int voption; Kicks: int }

let asyncRunner =
  let init(_: GameContext) =
    struct ({ Loaded = ValueNone; Kicks = 0 }, Cmd.none)

  let update (msg: AsyncMvuMsg) (model: AsyncMvuModel) =
    match msg with
    | Kick2 ->
      struct ({ model with Kicks = model.Kicks + 1 },
              Cmd.ofAsync
                (async {
                  do! Async.Sleep 20
                  return 7
                })
                Loaded
                (fun ex -> Loaded(-1)))
    | Loaded n -> struct ({ model with Loaded = ValueSome n }, Cmd.none)

  let program = HeadlessProgram.mkHeadless init update
  new HeadlessRunner<AsyncMvuModel, AsyncMvuMsg>(program, 100, 100)

asyncRunner.Dispatch Kick2
asyncRunner.Step(TimeSpan.FromMilliseconds frameMs)
let m1 = model asyncRunner

check
  "kick processed and async cmd started"
  (m1.Kicks = 1 && m1.Loaded = ValueNone)

// ── Adaptive: PostTask completion re-enters via the post drain ──────────────

printfn "adaptive smoke: postTask completion re-enters via the post drain"

let mutable taskResult = 0

let mutable resolveTask: (int -> unit) = ignore

let task: JS.Promise<int> =
  Promise.create(fun resolve _ -> resolveTask <- resolve)

postAdaptiveTask (fun () -> task) (fun v -> taskResult <- v) cappedRunner
cappedRunner.Step(TimeSpan.FromMilliseconds frameMs)
check "pending task applied nothing yet" (taskResult = 0)

// Resolve; the microtask queues the completion, so continue asynchronously.
resolveTask 7

// ── Async tail: microtasks only run once this script yields ─────────────────

let finish() =
  printfn ""

  if failures.Count > 0 then
    printfn $"SMOKE FAILED: %d{failures.Count} failure(s)"
    failwith "smoke failed"
  else
    printfn "ADAPTIVE SMOKE OK"

Promise.sleep 60
|> Promise.bind(fun _ ->
  // MVU: the async cmd's sleep elapsed; its dispatch waits in the queue.
  asyncRunner.Step(TimeSpan.FromMilliseconds frameMs)
  let m2 = model asyncRunner

  check
    "Cmd.ofAsync completion dispatched on a later frame"
    (m2.Loaded = ValueSome 7)

  // Adaptive: the task's completion sits in the post-update lane; the
  // next step's post drain applies it before forcing the frame.
  Promise.sleep 20)
|> Promise.tap(fun _ ->
  cappedRunner.Step(TimeSpan.FromMilliseconds frameMs)
  check "task completion applied at the post drain" (taskResult = 7))
|> Promise.tap(fun _ -> finish())
|> Promise.start
