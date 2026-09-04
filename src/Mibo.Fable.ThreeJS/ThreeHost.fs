module Mibo.Fable.ThreeJS.ThreeHost

open System
open Fable.Core
open Fable.Core.JsInterop
open Mibo.Elmish
open Mibo.Fable.ThreeJS.Bindings

type ThreeHostOptions = {
  Canvas: Browser.Types.HTMLCanvasElement option
  ClearColor: int
  ClearAlpha: float
  Antialias: bool
}

[<Emit("$0.width")>]
let private canvasWidth(canvas: Browser.Types.HTMLCanvasElement) : float =
  jsNative

[<Emit("$0.height")>]
let private canvasHeight(canvas: Browser.Types.HTMLCanvasElement) : float =
  jsNative

[<Emit("$0.addEventListener($1, $2)")>]
let private addListener (target: obj) (name: string) (handler: obj) : unit =
  jsNative

[<Emit("$0.removeEventListener($1, $2)")>]
let private removeListener (target: obj) (name: string) (handler: obj) : unit =
  jsNative

[<Emit("window.devicePixelRatio || 1")>]
let private devicePixelRatio() : float = jsNative

[<Emit("$0.key")>]
let private keyOf(ev: obj) : string = jsNative

type private ListenerHandle(target: obj, name: string, handler: obj) =
  let mutable disposed = false

  member _.Dispose() : unit =
    if not disposed then
      disposed <- true
      removeListener target name handler

  interface IDisposable with
    member this.Dispose() = this.Dispose()

type ThreeHost
  (
    renderer: WebGLRenderer,
    scene: Scene,
    camera: PerspectiveCamera,
    canvas: Browser.Types.HTMLCanvasElement,
    width: int,
    height: int
  ) =
  member _.Renderer = renderer
  member _.Scene = scene
  member _.Camera = camera
  member _.Canvas = canvas
  member _.Width = width
  member _.Height = height

  member _.Dispose() : unit = renderer.dispose()

  interface IDisposable with
    member this.Dispose() = this.Dispose()

let create(options: ThreeHostOptions) : ThreeHost =
  let canvas: Browser.Types.HTMLCanvasElement =
    options.Canvas
    |> Option.defaultWith(fun () ->
      let el = Browser.Dom.document.createElement "canvas"
      el :?> Browser.Types.HTMLCanvasElement)

  let width = int(canvasWidth canvas)
  let height = int(canvasHeight canvas)
  let safeWidth = if width <= 0 then 480 else width
  let safeHeight = if height <= 0 then 320 else height

  let renderer =
    new WebGLRenderer(
      {
        canvas = canvas
        antialias = options.Antialias
      }
    )

  let scene = Scene()
  let aspect = float safeWidth / float safeHeight
  let camera = PerspectiveCamera(75.0, aspect, 0.1, 1000.0)
  renderer.setClearColor(options.ClearColor, options.ClearAlpha)
  renderer.setPixelRatio(devicePixelRatio())
  renderer.setSize(float safeWidth, float safeHeight, false)
  camera.position.set(0.0, 0.0, 5.0)
  camera.lookAt(0.0, 0.0, 0.0)
  new ThreeHost(renderer, scene, camera, canvas, safeWidth, safeHeight)

let render(host: ThreeHost) : unit =
  host.Renderer.render(host.Scene, host.Camera :> Camera)

let resize (host: ThreeHost) (width: int) (height: int) : unit =
  if width > 0 && height > 0 then
    host.Renderer.setSize(float width, float height, false)

let setClearColor (host: ThreeHost) (hex: int) (alpha: float) : unit =
  host.Renderer.setClearColor(hex, alpha)

let stepMvu
  (host: ThreeHost)
  (runner: HeadlessRunner<'Model, 'Msg>)
  (ms: float)
  (render: 'Model -> unit)
  : unit =
  Mibo.Fable.Exports.stepFrame ms runner
  render(Mibo.Fable.Exports.model runner)

let stepAdaptive
  (host: ThreeHost)
  (runner: Mibo.Fable.Adaptive.AdaptiveHeadless<'Frame>)
  (ms: float)
  (render: 'Frame -> unit)
  : unit =
  Mibo.Fable.Exports.stepAdaptiveFrame ms runner
  render(Mibo.Fable.Exports.adaptiveFrame runner)

let startLoop(tick: float -> unit) : (unit -> unit) =
  let mutable running = true
  let mutable last = -1.0

  let rec frame(now: float) : unit =
    if running then
      let dt = if last < 0.0 then 16.6 else now - last

      last <- now
      tick dt
      Browser.Dom.window.requestAnimationFrame frame |> ignore

  Browser.Dom.window.requestAnimationFrame frame |> ignore
  (fun () -> running <- false)

let startMvuLoop
  (host: ThreeHost)
  (runner: HeadlessRunner<'Model, 'Msg>)
  (ms: float)
  (renderFn: 'Model -> unit)
  : (unit -> unit) =
  host |> ignore
  startLoop(fun _dt -> stepMvu host runner ms renderFn)

let startAdaptiveLoop
  (host: ThreeHost)
  (runner: Mibo.Fable.Adaptive.AdaptiveHeadless<'Frame>)
  (ms: float)
  (renderFn: 'Frame -> unit)
  : (unit -> unit) =
  host |> ignore
  startLoop(fun _dt -> stepAdaptive host runner ms renderFn)

let onCanvasClick (host: ThreeHost) (handler: unit -> unit) : IDisposable =
  let wrapped: obj = (fun (_ev: obj) -> handler()) |> unbox
  addListener (host.Canvas |> unbox) "click" wrapped
  new ListenerHandle(host.Canvas |> unbox, "click", wrapped)

let onKeyDown(handler: string -> unit) : IDisposable =
  let wrapped: obj = (fun (ev: obj) -> handler(keyOf ev)) |> unbox
  addListener (Browser.Dom.window |> unbox) "keydown" wrapped
  new ListenerHandle(Browser.Dom.window |> unbox, "keydown", wrapped)

let loadTexture(url: string) : JS.Promise<Texture> =
  TextureLoader().loadAsync(url)
