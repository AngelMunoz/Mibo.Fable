/// <summary>Small three.js surface for the foundation backend.</summary>
module Mibo.Fable.ThreeJS.Bindings

open Fable.Core

/// <summary>Creates a new three.js Scene handle.</summary>
val newScene: unit -> obj

/// <summary>Creates a perspective camera handle.</summary>
val newPerspectiveCamera:
  fov: float -> aspect: float -> near: float -> far: float -> obj

/// <summary>Creates a WebGLRenderer handle for the given canvas.</summary>
val newWebGLRenderer:
  canvas: Browser.Types.HTMLCanvasElement -> antialias: bool -> obj

/// <summary>Sets the clear color from a hex int.</summary>
val setClearColor: renderer: obj -> hex: int -> alpha: float -> unit

/// <summary>Sets the renderer size in pixels.</summary>
val setRendererSize:
  renderer: obj -> width: float -> height: float -> updateStyle: bool -> unit

/// <summary>Sets the renderer pixel ratio.</summary>
val setPixelRatio: renderer: obj -> ratio: float -> unit

/// <summary>Renders a scene with a camera.</summary>
val renderScene: renderer: obj -> scene: obj -> camera: obj -> unit

/// <summary>Releases renderer resources.</summary>
val disposeRenderer: renderer: obj -> unit

/// <summary>Clears all children from a scene.</summary>
val clearScene: scene: obj -> unit

/// <summary>Adds a child handle to a scene or group handle.</summary>
val addToScene: scene: obj -> child: obj -> unit

/// <summary>Removes a child handle from a scene handle.</summary>
val removeFromScene: scene: obj -> child: obj -> unit

/// <summary>Creates a box geometry handle.</summary>
val newBoxGeometry: w: float -> h: float -> d: float -> obj

/// <summary>Releases a geometry handle.</summary>
val disposeGeometry: geometry: obj -> unit

/// <summary>Creates a basic (unlit) material handle from a hex int.</summary>
val newMeshBasicMaterial: hex: int -> obj

/// <summary>Creates a standard (lit) material handle from a hex int.</summary>
val newMeshStandardMaterial:
  hex: int -> roughness: float -> metalness: float -> obj

/// <summary>Releases a material handle.</summary>
val disposeMaterial: material: obj -> unit

/// <summary>Creates a mesh handle from geometry and material handles.</summary>
val newMesh: geometry: obj -> material: obj -> obj

/// <summary>Sets the position of a mesh, camera, or light handle.</summary>
val setPosition: target: obj -> x: float -> y: float -> z: float -> unit

/// <summary>Sets the rotation (radians) of a mesh handle.</summary>
val setRotation: target: obj -> x: float -> y: float -> z: float -> unit

/// <summary>Points a camera handle at a world position.</summary>
val lookAt: camera: obj -> x: float -> y: float -> z: float -> unit

/// <summary>Creates an ambient light handle.</summary>
val newAmbientLight: hex: int -> intensity: float -> obj

/// <summary>Creates a directional light handle.</summary>
val newDirectionalLight: hex: int -> intensity: float -> obj

/// <summary>Creates a three.js Color handle from a hex int.</summary>
val newColor: hex: int -> obj

/// <summary>Loads a texture from a URL. Resolves with a texture handle.</summary>
val loadTextureAsync: url: string -> JS.Promise<obj>
