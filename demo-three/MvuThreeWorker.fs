module DemoThree.MvuThreeWorker

open Fable.Core
open Mibo.Fable.Exports

type IWorkerMessageEvent =
  abstract data: obj

type IWorkerScope =
  abstract onmessage: (IWorkerMessageEvent -> unit) with get, set
  abstract postMessage: data: obj -> unit

[<Emit("self")>]
let workerScope: IWorkerScope = jsNative

[<Emit("setTimeout($0, $1)")>]
let setTimeout(callback: unit -> unit, ms: int) : unit = jsNative

type WorkerIn =
  abstract kind: string

type Msg =
  | Tick of dtMs: float
  | Kick

type Model = {
  Angle: float
  Speed: float
  Frames: int
  Kicks: int
}

let initModel() : Model = {
  Angle = 0.0
  Speed = 1.2
  Frames = 0
  Kicks = 0
}

let updateModel (msg: Msg) (model: Model) : Model =
  match msg with
  | Tick dtMs ->
    let dt = dtMs / 1000.0

    {
      model with
          Angle = model.Angle + model.Speed * dt
          Frames = model.Frames + 1
    }
  | Kick ->
      {
        model with
            Speed = -model.Speed
            Kicks = model.Kicks + 1
      }

let runner =
  createRunnerWithTick initModel updateModel (fun dtMs -> Tick dtMs) 480 320

let simIntervalMs = 1000.0 / 30.0

let postFrame() : unit =
  let m = model runner

  workerScope.postMessage {|
    kind = "frame"
    angle = m.Angle
    speed = m.Speed
    frames = m.Frames
    kicks = m.Kicks
  |}

let rec loop() : unit =
  stepFrame simIntervalMs runner
  postFrame()
  setTimeout(loop, 33)

postFrame()
loop()

workerScope.onmessage <-
  fun ev ->
    let msg = unbox<WorkerIn> ev.data

    if msg.kind = "kick" then
      dispatch Kick runner
