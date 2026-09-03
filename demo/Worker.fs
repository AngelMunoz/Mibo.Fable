module Demo.Worker

open System
open Fable.Core
open Browser.Types

// ─────────────────────────────────────────────────────────────────────────────
// Shared worker-scope machinery for the demo's two worker entries: the
// browser interop bindings, the physics both simulations share, the Canvas2D
// painter, and the loop that steps a simulation at 30 Hz while painting an
// interpolated ~60 fps. The page never imports this module: it binds `self`,
// which only exists inside a worker.
// ─────────────────────────────────────────────────────────────────────────────

type IWorkerMessageEvent =
  abstract data: obj

type IWorkerScope =
  abstract onmessage: (IWorkerMessageEvent -> unit) with get, set
  abstract postMessage: data: obj * ?transfer: obj array -> unit

[<Emit("self")>]
let workerScope: IWorkerScope = jsNative

[<Emit("setTimeout($0, $1)")>]
let setTimeout(callback: unit -> unit, ms: int) : unit = jsNative

[<Emit("performance.now()")>]
let nowMs() : float = jsNative

[<Emit("$0.getContext('2d')")>]
let getContext2d(offscreen: obj) : CanvasRenderingContext2D = jsNative

[<Literal>]
let Width = 480.0

[<Literal>]
let Height = 320.0

// ── Physics: one bouncing-ball integration shared by both execution models ──

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

  x, y, vx, vy, bounced

let capSpeed(v: float) = Math.Clamp(v, -320.0, 320.0)

let lerp(a: float, b: float, t: float) : float = a + (b - a) * t

// ── Painter: draws any (x, y, status) onto the transferred canvas ───────────

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

// ── The loop: paint every ~16 ms; step the simulation whenever a 33.3 ms
//    slice of real time accumulates (at most 4 catch-up steps, then drop the
//    backlog so a hidden-tab timer throttle never spirals). The draw callback
//    receives alpha, the elapsed fraction of the pending slice — the blend
//    between the last two snapshots. ──────────────────────────────────────────

let simIntervalMs = 1000.0 / 30.0

let start(step: unit -> unit, draw: float -> unit) : unit =
  let mutable lastMs = nowMs()
  let mutable accMs = 0.0

  let rec loop() =
    let now = nowMs()
    accMs <- accMs + (now - lastMs)
    lastMs <- now

    let mutable steps = 0

    while accMs >= simIntervalMs && steps < 4 do
      step()
      accMs <- accMs - simIntervalMs
      steps <- steps + 1

    if accMs > simIntervalMs then
      accMs <- simIntervalMs

    draw(accMs / simIntervalMs)
    setTimeout(loop, 16)

  loop()
