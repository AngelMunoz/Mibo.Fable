module Demo.AdaptiveThreeWorker3D

open System
open Fable.Core
open Mibo.Fable.Exports
open Mibo.Fable.Adaptive
open Mibo.Elmish

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

type WorldRoots = {
  Angle: cval<float>
  Speed: cval<float>
  Frames: cval<int>
  Kicks: cval<int>
}

type RenderFrame = {
  Angle: float
  Speed: float
  Frames: int
  Kicks: int
}

let world: WorldRoots = {
  Angle = CVal.create 0.0
  Speed = CVal.create 1.2
  Frames = CVal.create 0
  Kicks = CVal.create 0
}

let kickWorld() : unit =
  CVal.set (-world.Speed.Value) world.Speed
  CVal.set (world.Kicks.Value + 1) world.Kicks

let initAdaptive(_ctx: AdaptiveFrameContext) : AdaptiveInit<RenderFrame> =
  let force() : RenderFrame = {
    Angle = AVal.getValue(CVal.value world.Angle)
    Speed = AVal.getValue(CVal.value world.Speed)
    Frames = AVal.getValue(CVal.value world.Frames)
    Kicks = AVal.getValue(CVal.value world.Kicks)
  }

  AdaptiveInit.ofFrameBuilder force

let updateAdaptive (_ctx: AdaptiveContext) (gt: GameTime) : unit =
  let dt = gt.ElapsedGameTime.TotalMilliseconds / 1000.0
  CVal.set (world.Angle.Value + world.Speed.Value * dt) world.Angle
  CVal.set (world.Frames.Value + 1) world.Frames

let runner = createAdaptiveRunner initAdaptive updateAdaptive 480 320

let simIntervalMs = 1000.0 / 30.0

let postFrame() : unit =
  let f = adaptiveFrame runner

  workerScope.postMessage {|
    kind = "frame"
    angle = f.Angle
    speed = f.Speed
    frames = f.Frames
    kicks = f.Kicks
  |}

let rec loop() : unit =
  stepAdaptiveFrame simIntervalMs runner
  postFrame()
  setTimeout(loop, 33)

postFrame()
loop()

workerScope.onmessage <-
  fun ev ->
    let msg = unbox<WorkerIn> ev.data

    if msg.kind = "kick" then
      postAdaptiveIntent kickWorld runner
