module Mibo.Fable.ThreeJS.Bindings

open Fable.Core
open Fable.Core.JsInterop

[<Import("Scene", "three")>]
type private SceneCtor() = class end

[<Import("PerspectiveCamera", "three")>]
type private CameraCtor(fov: float, aspect: float, near: float, far: float) =
  class end

[<Import("WebGLRenderer", "three")>]
type private RendererCtor(options: obj) = class end

[<Import("BoxGeometry", "three")>]
type private BoxGeometryCtor(w: float, h: float, d: float) = class end

[<Import("MeshBasicMaterial", "three")>]
type private MeshBasicMaterialCtor(options: obj) = class end

[<Import("MeshStandardMaterial", "three")>]
type private MeshStandardMaterialCtor(options: obj) = class end

[<Import("Mesh", "three")>]
type private MeshCtor(geometry: obj, material: obj) = class end

[<Import("AmbientLight", "three")>]
type private AmbientLightCtor(hex: int, intensity: float) = class end

[<Import("DirectionalLight", "three")>]
type private DirectionalLightCtor(hex: int, intensity: float) = class end

[<Import("Color", "three")>]
type private ColorCtor(hex: int) = class end

[<Import("TextureLoader", "three")>]
type private TextureLoaderCtor() = class end

[<Emit("$0.setClearColor($1, $2)")>]
let private emitSetClearColor(renderer: obj, hex: int, alpha: float) : unit =
  jsNative

[<Emit("$0.setSize($1, $2, $3)")>]
let private emitSetSize
  (renderer: obj, width: float, height: float, updateStyle: bool)
  : unit =
  jsNative

[<Emit("$0.setPixelRatio($1)")>]
let private emitSetPixelRatio(renderer: obj, ratio: float) : unit = jsNative

[<Emit("$0.render($1, $2)")>]
let private emitRender(renderer: obj, scene: obj, camera: obj) : unit = jsNative

[<Emit("$0.dispose()")>]
let private emitDispose(target: obj) : unit = jsNative

[<Emit("$0.clear()")>]
let private emitClearScene(scene: obj) : unit = jsNative

[<Emit("$0.add($1)")>]
let private emitAdd(scene: obj, child: obj) : unit = jsNative

[<Emit("$0.remove($1)")>]
let private emitRemove(scene: obj, child: obj) : unit = jsNative

[<Emit("$0.position.set($1, $2, $3)")>]
let private emitSetPosition(target: obj, x: float, y: float, z: float) : unit =
  jsNative

[<Emit("$0.rotation.set($1, $2, $3)")>]
let private emitSetRotation(target: obj, x: float, y: float, z: float) : unit =
  jsNative

[<Emit("$0.lookAt($1, $2, $3)")>]
let private emitLookAt(camera: obj, x: float, y: float, z: float) : unit =
  jsNative

[<Emit("$0.loadAsync($1)")>]
let private emitLoadAsync(loader: obj, url: string) : JS.Promise<obj> = jsNative

let newScene() : obj = SceneCtor() |> unbox

let newPerspectiveCamera
  (fov: float)
  (aspect: float)
  (near: float)
  (far: float)
  : obj =
  CameraCtor(fov, aspect, near, far) |> unbox

let newWebGLRenderer
  (canvas: Browser.Types.HTMLCanvasElement)
  (antialias: bool)
  : obj =
  RendererCtor(createObj [ "canvas" ==> canvas; "antialias" ==> antialias ])
  |> unbox

let setClearColor (renderer: obj) (hex: int) (alpha: float) : unit =
  emitSetClearColor(renderer, hex, alpha)

let setRendererSize
  (renderer: obj)
  (width: float)
  (height: float)
  (updateStyle: bool)
  : unit =
  emitSetSize(renderer, width, height, updateStyle)

let setPixelRatio (renderer: obj) (ratio: float) : unit =
  emitSetPixelRatio(renderer, ratio)

let renderScene (renderer: obj) (scene: obj) (camera: obj) : unit =
  emitRender(renderer, scene, camera)

let disposeRenderer(renderer: obj) : unit = emitDispose renderer

let clearScene(scene: obj) : unit = emitClearScene scene

let addToScene (scene: obj) (child: obj) : unit = emitAdd(scene, child)

let removeFromScene (scene: obj) (child: obj) : unit = emitRemove(scene, child)

let newBoxGeometry (w: float) (h: float) (d: float) : obj =
  BoxGeometryCtor(w, h, d) |> unbox

let disposeGeometry(geometry: obj) : unit = emitDispose geometry

let newMeshBasicMaterial(hex: int) : obj =
  MeshBasicMaterialCtor(createObj [ "color" ==> hex ]) |> unbox

let newMeshStandardMaterial
  (hex: int)
  (roughness: float)
  (metalness: float)
  : obj =
  MeshStandardMaterialCtor(
    createObj [
      "color" ==> hex
      "roughness" ==> roughness
      "metalness" ==> metalness
    ]
  )
  |> unbox

let disposeMaterial(material: obj) : unit = emitDispose material

let newMesh (geometry: obj) (material: obj) : obj =
  MeshCtor(geometry, material) |> unbox

let setPosition (target: obj) (x: float) (y: float) (z: float) : unit =
  emitSetPosition(target, x, y, z)

let setRotation (target: obj) (x: float) (y: float) (z: float) : unit =
  emitSetRotation(target, x, y, z)

let lookAt (camera: obj) (x: float) (y: float) (z: float) : unit =
  emitLookAt(camera, x, y, z)

let newAmbientLight (hex: int) (intensity: float) : obj =
  AmbientLightCtor(hex, intensity) |> unbox

let newDirectionalLight (hex: int) (intensity: float) : obj =
  DirectionalLightCtor(hex, intensity) |> unbox

let newColor(hex: int) : obj = ColorCtor(hex) |> unbox

let loadTextureAsync(url: string) : JS.Promise<obj> =
  let loader: obj = TextureLoaderCtor() |> unbox
  emitLoadAsync(loader, url)
