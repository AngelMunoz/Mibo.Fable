module DemoThree.Main

open Browser.Types
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
