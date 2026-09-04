module DemoThree.Main

open Browser.Types
open Fable.Core
open Mibo
open Mibo.Elmish
open Mibo.Fable.Exports
open Mibo.Fable.Adaptive
open Mibo.Fable.ThreeJS.Conversions
open Mibo.Fable.ThreeJS.Bindings
open Mibo.Fable.ThreeJS.ThreeHost

let private getCanvas(id: string) : HTMLCanvasElement =
  Browser.Dom.document.getElementById id :?> HTMLCanvasElement

let private makeCubeScene (host: ThreeHost) (hex: int) : obj =
  let ambient = newAmbientLight 0xffffff 0.9
  let dir = newDirectionalLight 0xffffff 1.2
  setPosition dir 2.0 3.0 4.0
  addToScene host.Scene ambient
  addToScene host.Scene dir
  let geometry = newBoxGeometry 1.4 1.4 1.4
  let material = newMeshStandardMaterial hex 0.6 0.1
  let mesh = newMesh geometry material
  setPosition mesh 0.0 0.0 0.0
  addToScene host.Scene mesh
  mesh

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

let mvuCanvas = getCanvas "stage-three-mvu"

let mvuHost =
  create {
    Canvas = Some mvuCanvas
    ClearColor = 0x10141a
    ClearAlpha = 1.0
    Antialias = true
  }

let mvuMesh = makeCubeScene mvuHost (colorToHexRgb Color.Blue)

let mvuRunner =
  createRunnerWithTick initModel updateModel (fun dtMs -> Tick dtMs) 480 320

let renderMvu(model: Model) : unit =
  setRotation mvuMesh (model.Angle * 0.5) model.Angle 0.0
  render mvuHost

onCanvasClick mvuHost (fun () -> dispatch Kick mvuRunner) |> ignore
startMvuLoop mvuHost mvuRunner 16.6 renderMvu |> ignore

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

let adaptiveCanvas = getCanvas "stage-three-adaptive"

let adaptiveHost =
  create {
    Canvas = Some adaptiveCanvas
    ClearColor = 0x10141a
    ClearAlpha = 1.0
    Antialias = true
  }

let adaptiveMesh = makeCubeScene adaptiveHost (colorToHexRgb Color.Green)

let adaptiveRunner = createAdaptiveRunner initAdaptive updateAdaptive 480 320

let renderAdaptive(frame: RenderFrame) : unit =
  setRotation adaptiveMesh (frame.Angle * 0.5) frame.Angle 0.0
  render adaptiveHost

onCanvasClick adaptiveHost (fun () ->
  postAdaptiveIntent kickWorld adaptiveRunner)
|> ignore

startAdaptiveLoop adaptiveHost adaptiveRunner 16.6 renderAdaptive |> ignore

// ── Worker-driven cubes: sim runs in a worker, render stays on main ─────────
// Workers post plain snapshots (angle, speed, frames, kicks). three.js
// handles never cross threads: each worker cube has its own host and mesh
// on the main thread, updated from the latest snapshot each frame.

type IWorkerMessageEvent =
  abstract data: obj

type ISimWorker =
  abstract postMessage: data: obj -> unit
  abstract onmessage: (IWorkerMessageEvent -> unit) with get, set

[<Emit("new Worker(new URL('./MvuThreeWorker.fs.js', import.meta.url), { type: 'module' })")>]
let createMvuThreeWorker() : ISimWorker = jsNative

[<Emit("new Worker(new URL('./AdaptiveThreeWorker.fs.js', import.meta.url), { type: 'module' })")>]
let createAdaptiveThreeWorker() : ISimWorker = jsNative

type SnapshotIn =
  abstract kind: string
  abstract angle: float
  abstract speed: float
  abstract frames: int
  abstract kicks: int

let mvuWorkerCanvas = getCanvas "stage-three-mvu-worker"

let mvuWorkerHost =
  create {
    Canvas = Some mvuWorkerCanvas
    ClearColor = 0x10141a
    ClearAlpha = 1.0
    Antialias = true
  }

let mvuWorkerMesh = makeCubeScene mvuWorkerHost (colorToHexRgb Color.Red)

let mutable mvuWorkerSnap: Model = {
  Angle = 0.0
  Speed = 1.2
  Frames = 0
  Kicks = 0
}

let mvuThreeWorker = createMvuThreeWorker()

mvuThreeWorker.onmessage <-
  fun ev ->
    let msg = unbox<SnapshotIn> ev.data

    if msg.kind = "frame" then
      mvuWorkerSnap <- {
        Angle = msg.angle
        Speed = msg.speed
        Frames = msg.frames
        Kicks = msg.kicks
      }

let adaptiveWorkerCanvas = getCanvas "stage-three-adaptive-worker"

let adaptiveWorkerHost =
  create {
    Canvas = Some adaptiveWorkerCanvas
    ClearColor = 0x10141a
    ClearAlpha = 1.0
    Antialias = true
  }

let adaptiveWorkerMesh =
  makeCubeScene adaptiveWorkerHost (colorToHexRgb Color.White)

let mutable adaptiveWorkerSnap: RenderFrame = {
  Angle = 0.0
  Speed = 1.2
  Frames = 0
  Kicks = 0
}

let adaptiveThreeWorker = createAdaptiveThreeWorker()

adaptiveThreeWorker.onmessage <-
  fun ev ->
    let msg = unbox<SnapshotIn> ev.data

    if msg.kind = "frame" then
      adaptiveWorkerSnap <- {
        Angle = msg.angle
        Speed = msg.speed
        Frames = msg.frames
        Kicks = msg.kicks
      }

onCanvasClick mvuWorkerHost (fun () ->
  mvuThreeWorker.postMessage {| kind = "kick" |})
|> ignore

onCanvasClick adaptiveWorkerHost (fun () ->
  adaptiveThreeWorker.postMessage {| kind = "kick" |})
|> ignore

startLoop(fun _dt ->
  setRotation mvuWorkerMesh (mvuWorkerSnap.Angle * 0.5) mvuWorkerSnap.Angle 0.0
  render mvuWorkerHost

  setRotation
    adaptiveWorkerMesh
    (adaptiveWorkerSnap.Angle * 0.5)
    adaptiveWorkerSnap.Angle
    0.0

  render adaptiveWorkerHost)
|> ignore
