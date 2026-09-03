module Demo.AdaptiveWorker

open System
open Browser.Types
open Mibo.Fable.Exports
open Mibo.Fable.Adaptive
open Mibo.Elmish
open Demo.Worker

// ─────────────────────────────────────────────────────────────────────────────
// The adaptive (signals) worker: State · Projection · Update · Force. Long-
// lived signal roots, the derived speed projection, the packed readonly
// frame, and the interpolated painter live here. The page posts "init" with
// this worker's OffscreenCanvas and "kick" for clicks (a posted intent); the
// status line is drawn inside the canvas, so nothing crosses back out.
// ─────────────────────────────────────────────────────────────────────────────

type WorkerIn =
  abstract kind: string
  abstract canvas: obj

/// State: long-lived signal roots, the composition root captured by both the
/// graph builder and the per-frame update.
type WorldRoots = {
  Pos: cval<(float * float)>
  Vel: cval<(float * float)>
  Bounces: cval<int>
  Frames: cval<int>
  Kicks: cval<int>
  Clock: cval<GameTime>
}

/// RenderFrame: everything the painter needs, packed once per step.
type RenderFrame = {|
  X: float
  Y: float
  Speed: float
  Bounces: int
  Frames: int
  Kicks: int
|}

let world = {
  Pos = CVal.create(40.0, 40.0)
  Vel = CVal.create(140.0, 100.0)
  Bounces = CVal.create 0
  Frames = CVal.create 0
  Kicks = CVal.create 0
  Clock =
    CVal.create {
      TotalTime = TimeSpan.Zero
      ElapsedGameTime = TimeSpan.Zero
    }
}

// Projection: speed is a derived, memoized view of the velocity root.
let speedProjection =
  world.Vel |> AVal.map(fun (vx, vy) -> sqrt(vx * vx + vy * vy))

let kickWorld() : unit =
  let vx, vy = world.Vel.Value
  CVal.set ((-vx * 1.4 |> capSpeed, -vy * 1.4 |> capSpeed)) world.Vel
  CVal.set (world.Kicks.Value + 1) world.Kicks

// Init: builds the derived graph and returns the frame force.
let init(_ctx: AdaptiveFrameContext) : AdaptiveInit<RenderFrame> =
  let inline force() : RenderFrame =
    let x, y = AVal.getValue world.Pos

    {|
      X = x
      Y = y
      Speed = AVal.getValue speedProjection
      Bounces = AVal.getValue world.Bounces
      Frames = AVal.getValue world.Frames
      Kicks = AVal.getValue world.Kicks
    |}

  AdaptiveInit.ofFrameBuilder force

// Update: reads projections, writes roots — no model is ever returned.
let update (_ctx: AdaptiveContext) (gt: GameTime) : unit =
  CVal.set gt world.Clock

  let px, py = world.Pos.Value
  let vx, vy = world.Vel.Value

  let x, y, vx, vy, bounced =
    stepPos gt.ElapsedGameTime.TotalMilliseconds px py vx vy

  CVal.set ((x, y)) world.Pos

  if bounced then
    CVal.set (vx, vy) world.Vel
    CVal.set (world.Bounces.Value + 1) world.Bounces

  CVal.set (world.Frames.Value + 1) world.Frames

let runner = createAdaptiveRunner init update (int Width) (int Height)

let mutable ctx = Unchecked.defaultof<CanvasRenderingContext2D>

// Primed from the runner's first forced frame, so prev = curr before the
// first step and the first paints are exact.
let mutable curr = adaptiveFrame runner
let mutable prev = curr

let snapshot() : unit =
  prev <- curr
  curr <- adaptiveFrame runner

let draw(alpha: float) =
  let x = lerp(prev.X, curr.X, alpha)
  let y = lerp(prev.Y, curr.Y, alpha)

  drawBall
    ctx
    x
    y
    $"signals · frames %d{curr.Frames} · bounces %d{curr.Bounces} · kicks %d{curr.Kicks} · speed %.0f{curr.Speed}"

let applySim() : unit =
  stepAdaptiveFrame simIntervalMs runner
  snapshot()

// Kicks are valid before "init": the runner exists from module load.
workerScope.onmessage <-
  fun ev ->
    let msg = unbox<WorkerIn> ev.data

    match msg.kind with
    | "init" ->
      ctx <- getContext2d msg.canvas
      start(applySim, draw)

    | "kick" -> postAdaptiveIntent kickWorld runner
    | _ -> ()
