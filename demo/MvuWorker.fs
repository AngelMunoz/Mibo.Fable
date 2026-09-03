module Demo.MvuWorker

open Browser.Types
open Mibo.Fable.Exports
open Mibo.Elmish
open Demo.Worker

// ─────────────────────────────────────────────────────────────────────────────
// The MVU worker: update returns a new model each step. The runner, its
// snapshot views, and the interpolated painter live here. The page posts
// "init" with this worker's OffscreenCanvas and "kick" for clicks; the
// status line is drawn inside the canvas, so nothing crosses back out.
// ─────────────────────────────────────────────────────────────────────────────

type WorkerIn =
  abstract kind: string
  abstract canvas: obj

type Msg =
  | Tick of dtMs: float
  | Kick

type Ball = {
  X: float
  Y: float
  VX: float
  VY: float
  Bounces: int
}

type Model = { Ball: Ball; Frames: int; Kicks: int }

let init() : Model = {
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

let update (msg: Msg) (model: Model) : Model =
  match msg with
  | Tick dtMs ->
    let x, y, vx, vy, bounced =
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

let mutable ctx = Unchecked.defaultof<CanvasRenderingContext2D>

let runner =
  createRunnerWithTick
    init
    update
    (fun dtMs -> Tick dtMs)
    (int Width)
    (int Height)

/// What the painter blends: positions interpolate, counters never do.
type View = {
  X: float
  Y: float
  Bounces: int
  Frames: int
  Kicks: int
}

let view(m: Model) : View = {
  X = m.Ball.X
  Y = m.Ball.Y
  Bounces = m.Ball.Bounces
  Frames = m.Frames
  Kicks = m.Kicks
}

let mutable curr = view(model runner)
let mutable prev = curr

let snapshot() : unit =
  prev <- curr
  curr <- view(model runner)

let draw(alpha: float) =
  let x = lerp(prev.X, curr.X, alpha)
  let y = lerp(prev.Y, curr.Y, alpha)

  drawBall
    ctx
    x
    y
    $"MVU · frames %d{curr.Frames} · bounces %d{curr.Bounces} · kicks %d{curr.Kicks}"

let applySim() : unit =
  stepFrame simIntervalMs runner
  snapshot()

// Kicks are valid before "init": the runner exists from module load.
workerScope.onmessage <-
  fun ev ->
    let msg = unbox<WorkerIn> ev.data

    match msg.kind with
    | "init" ->
      ctx <- getContext2d msg.canvas
      start(applySim, draw)

    | "kick" -> dispatch Kick runner
    | _ -> ()
