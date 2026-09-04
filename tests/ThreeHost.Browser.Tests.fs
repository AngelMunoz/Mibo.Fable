module ThreeHost.Browser.Tests

open Mibo.Elmish
open Mibo.Testing.QUnit
open Mibo.Fable.ThreeJS.ThreeHost

type Msg =
  | Tick of dtMs: float
  | Kick

type Model = {
  Elapsed: float
  Frames: int
  Kicks: int
}

let private init() : Model = { Elapsed = 0.0; Frames = 0; Kicks = 0 }

let private update (msg: Msg) (model: Model) : struct (Model * Cmd<Msg>) =
  match msg with
  | Tick dtMs ->
    struct ({
              model with
                  Elapsed = model.Elapsed + dtMs
                  Frames = model.Frames + 1
            },
            Cmd.none)
  | Kick -> struct ({ model with Kicks = model.Kicks + 1 }, Cmd.none)

let private newRunner() =
  let program =
    HeadlessProgram.mkHeadless (fun _ -> struct (init(), Cmd.none)) update
    |> HeadlessProgram.withTick(fun time ->
      time.ElapsedGameTime.TotalMilliseconds |> Tick)

  new HeadlessRunner<Model, Msg>(program, 320, 240)

let private newHost() =
  let canvas =
    Browser.Dom.document.createElement "canvas"
    :?> Browser.Types.HTMLCanvasElement

  canvas.width <- 320
  canvas.height <- 240

  (Browser.Dom.document.getElementById "qunit-fixture").appendChild canvas
  |> ignore

  create {
    Canvas = Some canvas
    ClearColor = 0x10141a
    ClearAlpha = 1.0
    Antialias = false
  }

QUnit.``module`` "ThreeHost browser loop"

QUnit.testAsync(
  "the loop steps the runner with wall time and renders the model",
  fun assert' -> promise {
    let runner = newRunner()
    let host = newHost()
    let mutable renderCalls = 0
    let mutable lastSeen = init()

    let stop =
      startMvuLoop runner (fun m ->
        renderCalls <- renderCalls + 1
        lastSeen <- m)

    // The first frame can be late while the GPU context warms up, so the
    // measurement starts at the first stepped frame, not at page load.
    let mutable waits = 0

    while runner.Model.Frames = 0 && waits < 200 do
      do! Promise.sleep 50
      waits <- waits + 1

    let framesAtStart = runner.Model.Frames
    let elapsedAtStart = runner.Model.Elapsed
    let rendersAtStart = renderCalls
    do! Promise.sleep 500
    stop()
    let frames = runner.Model.Frames - framesAtStart
    let elapsed = runner.Model.Elapsed - elapsedAtStart
    let renders = renderCalls - rendersAtStart

    assert'.ok(frames > 10, sprintf "stepped %d frames in 500 ms" frames)

    assert'.ok(
      abs(elapsed - 500.0) < 250.0,
      sprintf
        "stepped %.1f ms of simulation time in 500 ms of wall time"
        elapsed
    )

    assert'.ok(renders > 10, sprintf "rendered %d frames" renders)

    assert'.equal(
      lastSeen.Frames,
      runner.Model.Frames,
      "the last render saw the current model"
    )

    host.Dispose()
  }
)

QUnit.testAsync(
  "a canvas click dispatches into the running simulation",
  fun assert' -> promise {
    let runner = newRunner()
    let host = newHost()
    let handle = onCanvasClick host (fun () -> runner.Dispatch(Kick))
    let stop = startMvuLoop runner ignore
    do! Promise.sleep 120

    host.Canvas.dispatchEvent(Browser.Event.Event.Create("click")) |> ignore

    do! Promise.sleep 120
    stop()
    handle.Dispose()
    host.Dispose()

    assert'.equal(
      runner.Model.Kicks,
      1,
      "one click reached the model as one kick"
    )
  }
)
