module Demo.Main

open System
open Browser
open Browser.Types
open Fable.Core
open Mibo.Fable.Exports
open Mibo.Signals
open Mibo.Elmish

// ─────────────────────────────────────────────────────────────────────────────
// One bouncing-ball simulation, two execution models, one renderer:
//
//   left  — MVU: update returns a new model each frame (HeadlessRunner)
//   right — adaptive: long-lived signal roots mutated in place, derived
//           projections, and a frame force that publishes a readonly
//           RenderFrame each step (SignalsHeadless — the State · Projection
//           · Update · Force model from Defli/Kimo)
//
// The page is identical for both: step the runner each animation frame,
// paint the model/frame. The simulation never touches the DOM.
// ─────────────────────────────────────────────────────────────────────────────

[<Literal>]
let Width = 480.0

[<Literal>]
let Height = 320.0

type Msg =
  | Tick of dtMs: float
  | Kick

// ── Shared physics ──────────────────────────────────────────────────────────

let stepPos (dtMs: float) (x: float) (y: float) (vx: float) (vy: float) =
  let dt = dtMs / 1000.0
  let mutable x = x + vx * dt
  let mutable y = y + vy * dt
  let mutable vx = vx
  let mutable vy = vy
  let mutable bounced = false

  if x < 8.0 then
    x <- 8.0
    vx <- abs vx
    bounced <- true
  elif x > Width - 8.0 then
    x <- Width - 8.0
    vx <- -abs vx
    bounced <- true

  if y < 8.0 then
    y <- 8.0
    vy <- abs vy
    bounced <- true
  elif y > Height - 8.0 then
    y <- Height - 8.0
    vy <- -abs vy
    bounced <- true

  struct (x, y, vx, vy, bounced)

let capSpeed(v: float) = Math.Clamp(v, -320.0, 320.0)

// ── Shared renderer: draws any (x, y, status) onto a canvas ─────────────────

let drawBall
  (ctx: CanvasRenderingContext2D)
  (x: float)
  (y: float)
  (status: string)
  =
  ctx.fillStyle <- U3.Case1 "#10141a"
  ctx.fillRect(0.0, 0.0, Width, Height)

  ctx.beginPath()
  ctx.arc(x, y, 8.0, 0.0, Math.PI * 2.0)
  ctx.fillStyle <- U3.Case1 "#4fc3f7"
  ctx.fill()

  ctx.fillStyle <- U3.Case1 "#e6edf3"
  ctx.font <- "12px monospace"
  ctx.fillText(status, 10.0, Height - 10.0)

let frameMs = 1000.0 / 60.0

// ── Left panel: the MVU runner ──────────────────────────────────────────────

type Ball = {
  X: float
  Y: float
  VX: float
  VY: float
  Bounces: int
}

type MvuModel = { Ball: Ball; Frames: int; Kicks: int }

let mvuInit() : MvuModel = {
  Ball = {
    X = 40.0
    Y = 40.0
    VX = 140.0
    VY = 100.0
    Bounces = 0
  }
  Frames = 0
  Kicks = 0
}

let mvuUpdate (msg: Msg) (model: MvuModel) : MvuModel =
  match msg with
  | Tick dtMs ->
    let struct (x, y, vx, vy, bounced) =
      stepPos dtMs model.Ball.X model.Ball.Y model.Ball.VX model.Ball.VY

    {
      model with
          Ball = {
            X = x
            Y = y
            VX = vx
            VY = vy
            Bounces = model.Ball.Bounces + (if bounced then 1 else 0)
          }
          Frames = model.Frames + 1
    }
  | Kick ->
      {
        model with
            Kicks = model.Kicks + 1
            Ball = {
              model.Ball with
                  VX = model.Ball.VX * -1.4 |> capSpeed
                  VY = model.Ball.VY * -1.4 |> capSpeed
            }
      }

let mvuCanvas = document.getElementById "stage-mvu" :?> HTMLCanvasElement
let mvuCtx = mvuCanvas.getContext_2d()

let mvuRunner =
  createRunnerWithTick
    mvuInit
    mvuUpdate
    (fun dtMs -> Tick dtMs)
    (int Width)
    (int Height)

mvuCanvas.addEventListener("click", fun _ -> dispatch Kick mvuRunner)

let drawMvu() =
  let m = model mvuRunner

  drawBall
    mvuCtx
    m.Ball.X
    m.Ball.Y
    $"MVU · frames %d{m.Frames} · bounces %d{m.Ball.Bounces} · kicks %d{m.Kicks}"

// ── Right panel: the adaptive (signals) runner ──────────────────────────────

/// State: long-lived signal roots. Created up front — the composition root —
/// so both the graph builder (init) and the per-frame update capture it,
/// exactly like Defli's State + StateCell.
type WorldRoots = {
  Pos: CVal<struct (float * float)>
  Vel: CVal<struct (float * float)>
  Bounces: CVal<int>
  Frames: CVal<int>
  Kicks: CVal<int>
  Clock: CVal<GameTime>
}

/// RenderFrame: everything the renderer needs, packed once per step.
type RenderFrame = {|
  X: float
  Y: float
  Speed: float
  Bounces: int
  Frames: int
  Kicks: int
|}

let adaptiveCanvas =
  document.getElementById "stage-adaptive" :?> HTMLCanvasElement

let adaptiveCtx = adaptiveCanvas.getContext_2d()

let world = {
  Pos = CVal.create(struct (40.0, 40.0))
  Vel = CVal.create(struct (140.0, 100.0))
  Bounces = CVal.create 0
  Frames = CVal.create 0
  Kicks = CVal.create 0
  Clock =
    CVal.create {
      TotalTime = TimeSpan.Zero
      ElapsedGameTime = TimeSpan.Zero
    }
}

// Projection: speed is a derived, memoized view of the velocity root —
// it only recomputes when the velocity actually changes.
let speedProjection =
  AVal.computed(fun () ->
    let struct (vx, vy) = AVal.get world.Vel
    sqrt(vx * vx + vy * vy))

let kickWorld() : unit =
  let struct (vx, vy) = CVal.get world.Vel
  CVal.set (struct (-vx * 1.4 |> capSpeed, -vy * 1.4 |> capSpeed)) world.Vel
  CVal.set (CVal.get world.Kicks + 1) world.Kicks

// Init: builds the derived graph and returns the frame force — the readonly
// view packed once per step, after which drawing is plain data reads.
let adaptiveInit(_ctx: SignalsFrameContext) : SignalsInit<RenderFrame> =
  let force() : RenderFrame =
    let struct (x, y) = AVal.get world.Pos

    {|
      X = x
      Y = y
      Speed = AVal.get speedProjection
      Bounces = AVal.get world.Bounces
      Frames = AVal.get world.Frames
      Kicks = AVal.get world.Kicks
    |}

  SignalsInit.ofFrameBuilder force

// Update: reads projections, writes roots — no model is ever returned.
let adaptiveUpdate (_ctx: SignalsContext) (gt: GameTime) : unit =
  CVal.set gt world.Clock

  let struct (px, py) = CVal.get world.Pos
  let struct (vx, vy) = CVal.get world.Vel

  let struct (x, y, vx, vy, bounced) =
    stepPos gt.ElapsedGameTime.TotalMilliseconds px py vx vy

  CVal.set (struct (x, y)) world.Pos

  if bounced then
    CVal.set (struct (vx, vy)) world.Vel
    CVal.set (CVal.get world.Bounces + 1) world.Bounces

  CVal.set (CVal.get world.Frames + 1) world.Frames

let adaptiveRunner =
  createAdaptiveRunnerWithFixedStep
    adaptiveInit
    adaptiveUpdate
    (1.0 / 60.0)
    5
    (int Width)
    (int Height)

adaptiveCanvas.addEventListener(
  "click",
  fun _ -> postAdaptiveIntent kickWorld adaptiveRunner
)

let drawAdaptive() =
  let f = adaptiveFrame adaptiveRunner

  drawBall
    adaptiveCtx
    f.X
    f.Y
    $"signals · frames %d{f.Frames} · bounces %d{f.Bounces} · kicks %d{f.Kicks} · speed %.0f{f.Speed}"

// ── The host loop: one rAF, both simulations, both painters ─────────────────

let stepBoth() =
  stepFrame frameMs mvuRunner
  stepAdaptiveFrame frameMs adaptiveRunner

let rec loop() =
  stepBoth()
  drawMvu()
  drawAdaptive()
  window.requestAnimationFrame(fun _ -> loop()) |> ignore

loop()
