module private Poc.Main

open System
open Fable.Core
open Fable.Core.JsInterop
open Mibo.Elmish
open Mibo.Fable.Phaser

type Msg =
  private
  | Tick of dtMs: float
  | Kick

type Model = private {
  X: float
  Dir: float
  Kicks: int
  Frames: int
}

let private width = 320.0

let private init() : Model = {
  X = 40.0
  Dir = 1.0
  Kicks = 0
  Frames = 0
}

let private update msg model =
  match msg with
  | Tick dtMs ->
    let nx = model.X + model.Dir * 140.0 * dtMs / 1000.0

    let nx, dir =
      if nx > width - 24.0 then (width - 24.0, -1.0)
      elif nx < 24.0 then (24.0, 1.0)
      else (nx, model.Dir)

    struct ({
              model with
                  X = nx
                  Dir = dir
                  Frames = model.Frames + 1
            },
            Cmd.none)
  | Kick ->
    struct ({
              model with
                  Dir = -model.Dir
                  Kicks = model.Kicks + 1
            },
            Cmd.none)

let private program =
  HeadlessProgram.mkHeadless (fun _ctx -> struct (init(), Cmd.none)) update

let private runner = new HeadlessRunner<Model, Msg>(program, int width, 240)

let private globalObj: obj = emitJsExpr () "globalThis"
let private logs: obj = emitJsExpr () "[]"

type private SimScene(runner: HeadlessRunner<Model, Msg>) =
  inherit Scene()

  let mutable gfx: Graphics voption = ValueNone
  let mutable label: Text voption = ValueNone

  override this.create() =
    logs?push("create")
    let g = this.add.graphics()
    g.fillStyle(0x4fc3f7) |> ignore
    g.fillRect(-16.0, -16.0, 32.0, 32.0) |> ignore
    gfx <- ValueSome g

    let t =
      this.add.text(
        8.0,
        8.0,
        "booting",
        createObj [ "fontSize" ==> "10px"; "color" ==> "#e8e8f0" ]
      )

    label <- ValueSome t
    this.input.on("pointerdown", fun _ -> runner.Dispatch(Kick))
    this.scene?launch("canvas-scene")

  override this.update(time: float, delta: float) =
    // Phaser owns the frame pacing; the simulation steps once per scene tick.
    logs?push("update")
    runner.Dispatch(Tick delta)
    runner.Step(TimeSpan.FromMilliseconds delta)

    let m = runner.Model

    gfx |> ValueOption.iter(fun g -> g.setPosition(m.X, 140.0) |> ignore)

    label
    |> ValueOption.iter(fun t ->
      t.setText(
        sprintf "x=%.0f dir=%.0f kicks=%d frames=%d" m.X m.Dir m.Kicks m.Frames
      )
      |> ignore)

/// The signature-file-friendly alternative: assign the lifecycle hooks as
/// dynamic properties instead of subclassing Scene.
let private phaser: obj = importAll "phaser"

let private newScene(config: obj) : Scene =
  emitJsExpr (phaser, config) "new ($0.Scene)($1)"

let private makeCanvasScene() : Scene =
  let s = newScene(createObj [ "key" ==> "canvas-scene" ])
  let mutable status: Text voption = ValueNone

  s?create <-
    fun () ->
      let g = s.add.graphics()
      g.fillStyle(0x7cf29c) |> ignore
      g.fillCircle(60.0, 60.0, 18.0) |> ignore

      let t =
        s.add.text(
          8.0,
          220.0,
          "composition scene booting",
          createObj [ "fontSize" ==> "10px"; "color" ==> "#7cf29c" ]
        )

      status <- ValueSome t

  s?update <-
    fun () ->
      status
      |> ValueOption.iter(fun t -> t.setText("composition scene ok") |> ignore)

  s

let private scene = SimScene(runner)

let private canvasScene = makeCanvasScene()

let private config =
  createObj [
    "type" ==> Constants.CANVAS
    "width" ==> int width
    "height" ==> 240
    "backgroundColor" ==> "#0f1220"
    "parent" ==> "app"
    // The embedded preview pane may throttle requestAnimationFrame; a
    // setTimeout loop keeps the game ticking regardless.
    "fps" ==> createObj [ "forceSetTimeOut" ==> true; "target" ==> 60 ]
    "scene" ==> [| box scene; box canvasScene |]
  ]

let private game = Game(config)

globalObj?__poc <-
  createObj [
    "logs" ==> logs
    "game" ==> game
    "canvasScene" ==> canvasScene
    "modelX" ==> fun () -> runner.Model.X
    "frames" ==> fun () -> runner.Model.Frames
    "kicks" ==> fun () -> runner.Model.Kicks
  ]
