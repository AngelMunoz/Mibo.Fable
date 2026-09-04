/// <summary>Minimal three.js host for Mibo.Fable simulations.</summary>
module Mibo.Fable.ThreeJS.ThreeHost

open System
open Fable.Core
open Mibo.Elmish
open Mibo.Fable.ThreeJS.Bindings

/// <summary>Opaque handle to a three.js renderer, scene, and camera.</summary>
[<Class>]
type ThreeHost =
  new:
    renderer: WebGLRenderer *
    scene: Scene *
    camera: PerspectiveCamera *
    canvas: Browser.Types.HTMLCanvasElement *
    width: int *
    height: int ->
      ThreeHost

  /// <summary>The three.js WebGLRenderer.</summary>
  member Renderer: WebGLRenderer

  /// <summary>The three.js Scene.</summary>
  member Scene: Scene

  /// <summary>The three.js camera.</summary>
  member Camera: PerspectiveCamera

  /// <summary>The canvas the renderer draws to.</summary>
  member Canvas: Browser.Types.HTMLCanvasElement

  /// <summary>Width of the drawing surface in pixels.</summary>
  member Width: int

  /// <summary>Height of the drawing surface in pixels.</summary>
  member Height: int

  /// <summary>Releases the renderer.</summary>
  member Dispose: unit -> unit
  interface IDisposable

/// <summary>Options to create a host.</summary>
type ThreeHostOptions = {
  /// <summary>Canvas to draw to. When None, the host creates one.</summary>
  Canvas: Browser.Types.HTMLCanvasElement option
  /// <summary>Clear color as 0xRRGGBB.</summary>
  ClearColor: int
  /// <summary>Clear alpha in 0..1.</summary>
  ClearAlpha: float
  /// <summary>Whether to request antialiasing.</summary>
  Antialias: bool
}

/// <summary>Creates a host with a WebGLRenderer, Scene, and PerspectiveCamera.</summary>
val create: options: ThreeHostOptions -> ThreeHost

/// <summary>Renders the host scene with its camera.</summary>
val render: host: ThreeHost -> unit

/// <summary>Resizes the renderer and updates the camera aspect.</summary>
val resize: host: ThreeHost -> width: int -> height: int -> unit

/// <summary>Sets the clear color and alpha.</summary>
val setClearColor: host: ThreeHost -> hex: int -> alpha: float -> unit

/// <summary>Runs one MVU frame: steps the runner, then renders the model.</summary>
val stepMvu:
  host: ThreeHost ->
  runner: HeadlessRunner<'Model, 'Msg> ->
  ms: float ->
  render: ('Model -> unit) ->
    unit

/// <summary>Runs one signals frame: steps the runner, then renders the frame.</summary>
val stepAdaptive:
  host: ThreeHost ->
  runner: Mibo.Fable.Adaptive.AdaptiveHeadless<'Frame> ->
  ms: float ->
  render: ('Frame -> unit) ->
    unit

/// <summary>Starts a requestAnimationFrame loop for an MVU runner. Returns a stop function.</summary>
val startMvuLoop:
  host: ThreeHost ->
  runner: HeadlessRunner<'Model, 'Msg> ->
  ms: float ->
  renderFn: ('Model -> unit) ->
    (unit -> unit)

/// <summary>Starts a requestAnimationFrame loop for a signals runner. Returns a stop function.</summary>
val startAdaptiveLoop:
  host: ThreeHost ->
  runner: Mibo.Fable.Adaptive.AdaptiveHeadless<'Frame> ->
  ms: float ->
  renderFn: ('Frame -> unit) ->
    (unit -> unit)

/// <summary>Runs a custom requestAnimationFrame loop. Returns a stop function.</summary>
val startLoop: tick: (float -> unit) -> (unit -> unit)

/// <summary>Attaches a click handler to the host canvas. Returns a dispose handle.</summary>
val onCanvasClick: host: ThreeHost -> handler: (unit -> unit) -> IDisposable

/// <summary>Attaches a keydown handler to the window. The handler gets the key string.</summary>
val onKeyDown: handler: (string -> unit) -> IDisposable

/// <summary>Loads a texture from a URL. Resolves with the texture.</summary>
val loadTexture: url: string -> JS.Promise<Texture>
