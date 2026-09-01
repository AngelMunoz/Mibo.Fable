module Demo.Main

open System
open Browser
open Fable.Core
open Browser.Types
open Mibo.Fable.Exports

// ─────────────────────────────────────────────────────────────────────────────
// The simulation: a ball bouncing inside the canvas.
//
// The model and messages are pure data and know nothing about the DOM. The
// browser host steps the simulation through the headless runner each
// requestAnimationFrame and reads the model back to draw it — exactly the
// split a three.js or Phaser frontend would use, just with Canvas2D to keep
// the demo dependency-free.
// ─────────────────────────────────────────────────────────────────────────────

type Msg =
    | Tick of dtMs: float
    | Kick

type Ball =
    { X: float
      Y: float
      VX: float
      VY: float
      Bounces: int }

type Model =
    { Ball: Ball
      Frames: int
      Kicks: int }

[<Literal>]
let Width = 480.

[<Literal>]
let Height = 320.

let init () : Model =
    { Ball = { X = 40.; Y = 40.; VX = 140.; VY = 100.; Bounces = 0 }
      Frames = 0
      Kicks = 0 }

let stepBall (dtMs: float) (b: Ball) : Ball =
    let dt = dtMs / 1000.
    let mutable x = b.X + b.VX * dt
    let mutable y = b.Y + b.VY * dt
    let mutable vx = b.VX
    let mutable vy = b.VY
    let mutable bounces = b.Bounces

    if x < 8. then
        x <- 8.
        vx <- abs vx
        bounces <- bounces + 1
    elif x > Width - 8. then
        x <- Width - 8.
        vx <- -abs vx
        bounces <- bounces + 1

    if y < 8. then
        y <- 8.
        vy <- abs vy
        bounces <- bounces + 1
    elif y > Height - 8. then
        y <- Height - 8.
        vy <- -abs vy
        bounces <- bounces + 1

    { X = x; Y = y; VX = vx; VY = vy; Bounces = bounces }

let update (msg: Msg) (model: Model) : Model =
    match msg with
    | Tick dtMs ->
        { model with
            Ball = stepBall dtMs model.Ball
            Frames = model.Frames + 1 }
    | Kick ->
        // Reverse and boost, capped so the demo stays calm no matter how
        // often the user clicks.
        let cap (v: float) = Math.Clamp(v, -320., 320.)
        { model with
            Kicks = model.Kicks + 1
            Ball =
                { model.Ball with
                    VX = model.Ball.VX * -1.4 |> cap
                    VY = model.Ball.VY * -1.4 |> cap } }

// ─────────────────────────────────────────────────────────────────────────────
// The host: drives the simulation with the headless runner and paints the
// resulting model. All DOM access lives below this line.
// ─────────────────────────────────────────────────────────────────────────────

let canvas = document.getElementById "stage" :?> HTMLCanvasElement
let ctx = canvas.getContext_2d ()

let draw (m: Model) =
    ctx.fillStyle <- U3.Case1 "#10141a"
    ctx.fillRect(0., 0., Width, Height)

    ctx.beginPath()
    ctx.arc(m.Ball.X, m.Ball.Y, 8., 0., Math.PI * 2.)
    ctx.fillStyle <- U3.Case1 "#4fc3f7"
    ctx.fill()

    ctx.fillStyle <- U3.Case1 "#e6edf3"
    ctx.font <- "12px monospace"
    ctx.fillText($"frames %d{m.Frames} · bounces %d{m.Ball.Bounces} · kicks %d{m.Kicks}", 10., Height - 10.)

let runner =
    createRunnerWithTick init update (fun dtMs -> Tick dtMs) (int Width) (int Height)

canvas.addEventListener ("click", fun _ -> dispatch Kick runner)

// Step a fixed 60 Hz frame on every animation frame, then paint the model.
let frameMs = 1000. / 60.

let rec loop () =
    stepFrame frameMs runner
    draw (model runner)
    window.requestAnimationFrame(fun _ -> loop ()) |> ignore

loop ()
