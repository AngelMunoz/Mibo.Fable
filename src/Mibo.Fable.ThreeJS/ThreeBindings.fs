module Mibo.Fable.ThreeJS.Bindings

open System
open Fable.Core

type WebGLRendererOptions = {
  canvas: Browser.Types.HTMLCanvasElement
  antialias: bool
}

type MeshBasicMaterialOptions = { color: int }

type MeshStandardMaterialOptions = {
  color: int
  roughness: float
  metalness: float
}

[<Import("Vector3", "three")>]
type Vector3(x: float, y: float, z: float) =
  member _.set(x: float, y: float, z: float) : unit = jsNative

[<Import("Euler", "three")>]
type Euler() =
  member _.set(x: float, y: float, z: float) : unit = jsNative

[<Import("Object3D", "three")>]
type Object3D() =
  member _.position: Vector3 = jsNative
  member _.rotation: Euler = jsNative
  member _.add(child: Object3D) : unit = jsNative
  member _.remove(child: Object3D) : unit = jsNative
  member _.clear() : unit = jsNative
  member _.lookAt(x: float, y: float, z: float) : unit = jsNative

[<Import("Scene", "three")>]
type Scene() =
  inherit Object3D()

[<Import("Camera", "three")>]
type Camera() =
  inherit Object3D()

[<Import("PerspectiveCamera", "three")>]
type PerspectiveCamera(fov: float, aspect: float, near: float, far: float) =
  inherit Camera()

[<Import("WebGLRenderer", "three")>]
type WebGLRenderer(options: WebGLRendererOptions) =
  member _.setClearColor(hex: int, alpha: float) : unit = jsNative

  member _.setSize(width: float, height: float, updateStyle: bool) : unit =
    jsNative

  member _.setPixelRatio(ratio: float) : unit = jsNative
  member _.render(scene: Scene, camera: Camera) : unit = jsNative
  member _.dispose() : unit = jsNative

  interface IDisposable with
    member this.Dispose() = this.dispose()

[<Import("BufferGeometry", "three")>]
type BufferGeometry() =
  member _.dispose() : unit = jsNative

  interface IDisposable with
    member this.Dispose() = this.dispose()

[<Import("BoxGeometry", "three")>]
type BoxGeometry(w: float, h: float, d: float) =
  inherit BufferGeometry()

[<Import("Material", "three")>]
type Material() =
  member _.dispose() : unit = jsNative

  interface IDisposable with
    member this.Dispose() = this.dispose()

[<Import("MeshBasicMaterial", "three")>]
type MeshBasicMaterial(options: MeshBasicMaterialOptions) =
  inherit Material()

[<Import("MeshStandardMaterial", "three")>]
type MeshStandardMaterial(options: MeshStandardMaterialOptions) =
  inherit Material()

[<Import("Mesh", "three")>]
type Mesh(geometry: BufferGeometry, material: Material) =
  inherit Object3D()

[<Import("Light", "three")>]
type Light() =
  inherit Object3D()

[<Import("AmbientLight", "three")>]
type AmbientLight(hex: int, intensity: float) =
  inherit Light()

[<Import("DirectionalLight", "three")>]
type DirectionalLight(hex: int, intensity: float) =
  inherit Light()

[<Import("Texture", "three")>]
type Texture() =
  member _.dispose() : unit = jsNative

  interface IDisposable with
    member this.Dispose() = this.dispose()

[<Import("TextureLoader", "three")>]
type TextureLoader() =
  member _.loadAsync(url: string) : JS.Promise<Texture> = jsNative
