/// <summary>Small typed three.js surface for the foundation backend.</summary>
/// <remarks>
/// Each F# type maps to one three.js class, so the type checker rejects
/// wrong wiring (a light where a geometry belongs). Member names match the
/// three.js names exactly, as in Fable.Browser.Dom. Position and rotation
/// go through <see cref="M:Mibo.Fable.ThreeJS.Bindings.setPosition"/> and
/// <see cref="M:Mibo.Fable.ThreeJS.Bindings.setRotation"/>, which reach the
/// nested <c>position</c> and <c>rotation</c> objects.
/// Option records use the exact three.js field names.
/// </remarks>
module Mibo.Fable.ThreeJS.Bindings

open System
open Fable.Core

/// <summary>Options for <see cref="T:Mibo.Fable.ThreeJS.Bindings.WebGLRenderer"/>.</summary>
type WebGLRendererOptions = {
  /// <summary>Canvas the renderer draws to.</summary>
  canvas: Browser.Types.HTMLCanvasElement
  /// <summary>Whether to request antialiasing.</summary>
  antialias: bool
}

/// <summary>Options for <see cref="T:Mibo.Fable.ThreeJS.Bindings.MeshBasicMaterial"/>.</summary>
type MeshBasicMaterialOptions = {
  /// <summary>Base color as 0xRRGGBB.</summary>
  color: int
}

/// <summary>Options for <see cref="T:Mibo.Fable.ThreeJS.Bindings.MeshStandardMaterial"/>.</summary>
type MeshStandardMaterialOptions = {
  /// <summary>Base color as 0xRRGGBB.</summary>
  color: int
  /// <summary>Roughness in 0..1.</summary>
  roughness: float
  /// <summary>Metalness in 0..1.</summary>
  metalness: float
}

/// <summary>A three.js Vector3: position, scale, and direction.</summary>
[<Class>]
[<Import("Vector3", "three")>]
type Vector3 =
  new: x: float * y: float * z: float -> Vector3

  /// <summary>Sets the three components.</summary>
  member set: x: float * y: float * z: float -> unit

/// <summary>A three.js Euler: rotation in radians.</summary>
[<Class>]
[<Import("Euler", "three")>]
type Euler =
  /// <summary>Sets the three angles in radians.</summary>
  member set: x: float * y: float * z: float -> unit

/// <summary>A three.js Object3D: transform and children.</summary>
[<Class>]
[<Import("Object3D", "three")>]
type Object3D =
  new: unit -> Object3D

  /// <summary>Local position. Set components through it.</summary>
  member position: Vector3

  /// <summary>Local rotation in radians. Set angles through it.</summary>
  member rotation: Euler

  /// <summary>Adds a child to this object.</summary>
  member add: child: Object3D -> unit

  /// <summary>Removes a child from this object.</summary>
  member remove: child: Object3D -> unit

  /// <summary>Removes all children from this object.</summary>
  member clear: unit -> unit

  /// <summary>Rotates this object to face a world position.</summary>
  member lookAt: x: float * y: float * z: float -> unit

/// <summary>A three.js Scene: the root of what the renderer draws.</summary>
[<Class>]
[<Import("Scene", "three")>]
type Scene =
  inherit Object3D
  new: unit -> Scene

/// <summary>A three.js Camera.</summary>
[<Class>]
[<Import("Camera", "three")>]
type Camera =
  inherit Object3D

/// <summary>A three.js PerspectiveCamera.</summary>
[<Class>]
[<Import("PerspectiveCamera", "three")>]
type PerspectiveCamera =
  inherit Camera

  new:
    fov: float * aspect: float * near: float * far: float -> PerspectiveCamera

/// <summary>A three.js WebGLRenderer.</summary>
[<Class>]
[<Import("WebGLRenderer", "three")>]
type WebGLRenderer =
  new: options: WebGLRendererOptions -> WebGLRenderer

  /// <summary>Sets the clear color and alpha.</summary>
  member setClearColor: hex: int * alpha: float -> unit

  /// <summary>Sets the drawing buffer size in pixels.</summary>
  member setSize: width: float * height: float * updateStyle: bool -> unit

  /// <summary>Sets the pixel ratio.</summary>
  member setPixelRatio: ratio: float -> unit

  /// <summary>Renders a scene with a camera.</summary>
  member render: scene: Scene * camera: Camera -> unit

  /// <summary>Releases renderer resources.</summary>
  member dispose: unit -> unit
  interface IDisposable

/// <summary>A three.js BufferGeometry.</summary>
[<Class>]
[<Import("BufferGeometry", "three")>]
type BufferGeometry =
  new: unit -> BufferGeometry

  /// <summary>Releases geometry resources.</summary>
  member dispose: unit -> unit
  interface IDisposable

/// <summary>A three.js BoxGeometry.</summary>
[<Class>]
[<Import("BoxGeometry", "three")>]
type BoxGeometry =
  inherit BufferGeometry
  new: w: float * h: float * d: float -> BoxGeometry

/// <summary>A three.js Material.</summary>
[<Class>]
[<Import("Material", "three")>]
type Material =
  new: unit -> Material

  /// <summary>Releases material resources.</summary>
  member dispose: unit -> unit
  interface IDisposable

/// <summary>A three.js MeshBasicMaterial (unlit).</summary>
[<Class>]
[<Import("MeshBasicMaterial", "three")>]
type MeshBasicMaterial =
  inherit Material
  new: options: MeshBasicMaterialOptions -> MeshBasicMaterial

/// <summary>A three.js MeshStandardMaterial (lit).</summary>
[<Class>]
[<Import("MeshStandardMaterial", "three")>]
type MeshStandardMaterial =
  inherit Material
  new: options: MeshStandardMaterialOptions -> MeshStandardMaterial

/// <summary>A three.js Mesh: geometry plus material in the scene graph.</summary>
[<Class>]
[<Import("Mesh", "three")>]
type Mesh =
  inherit Object3D
  new: geometry: BufferGeometry * material: Material -> Mesh

/// <summary>A three.js Light.</summary>
[<Class>]
[<Import("Light", "three")>]
type Light =
  inherit Object3D

/// <summary>A three.js AmbientLight.</summary>
[<Class>]
[<Import("AmbientLight", "three")>]
type AmbientLight =
  inherit Light
  new: hex: int * intensity: float -> AmbientLight

/// <summary>A three.js DirectionalLight.</summary>
[<Class>]
[<Import("DirectionalLight", "three")>]
type DirectionalLight =
  inherit Light
  new: hex: int * intensity: float -> DirectionalLight

/// <summary>A three.js Texture handle.</summary>
[<Class>]
[<Import("Texture", "three")>]
type Texture =
  /// <summary>Releases texture resources.</summary>
  member dispose: unit -> unit
  interface IDisposable

/// <summary>A three.js TextureLoader.</summary>
[<Class>]
[<Import("TextureLoader", "three")>]
type TextureLoader =
  new: unit -> TextureLoader

  /// <summary>Loads a texture from a URL. Resolves with the texture.</summary>
  member loadAsync: url: string -> JS.Promise<Texture>
