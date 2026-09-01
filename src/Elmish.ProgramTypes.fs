namespace Mibo.Elmish

open System
open Mibo.Diagnostics
open Mibo.Windowing

[<Struct>]
type GameConfig = {
  Width: int
  Height: int
  Title: string
  TargetFPS: int voption
  MinWidth: int voption
  MinHeight: int voption
  Resizable: bool
  WindowMode: WindowMode
}

module GameConfig =
  let defaultConfig = {
    Width = 800
    Height = 600
    Title = "Mibo F#"
    TargetFPS = ValueNone
    MinWidth = ValueNone
    MinHeight = ValueNone
    Resizable = false
    WindowMode = Windowed
  }

  let withWidth width config = { config with Width = width }
  let withHeight height config = { config with Height = height }

  let withMinWidth width config = {
    config with
        MinWidth = ValueSome width
  }

  let withMinHeight height config = {
    config with
        MinHeight = ValueSome height
  }

  let withTitle title config = { config with Title = title }

  /// Cap the render rate at the given FPS (MonoGame fixed timestep at 1/fps,
  /// Raylib <c>SetTargetFPS</c>). Omit it (the default) to leave the backend's
  /// framerate behavior untouched.
  let withTargetFPS fps config = {
    config with
        TargetFPS = ValueSome fps
  }

  /// Allow the user to resize the window.
  let withResizable config = { config with Resizable = true }

  /// Set the window presentation mode at startup (windowed, borderless
  /// fullscreen, or exclusive fullscreen).
  let withWindowMode mode config = { config with WindowMode = mode }

type Program<'Model, 'Msg> = {
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  Update: 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  UpdateCtx:
    (GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) voption
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  Config: (GameConfig -> GameConfig) list
  Renderers: (unit -> IRenderer<'Model>) list
  Tick: (GameTime -> 'Msg) voption
  FixedStep: FixedStepConfig<'Msg> voption
  DispatchMode: DispatchMode
  AssetsBasePath: string voption
  HasInput: bool
  HasInputMapper: bool
  ServiceRegistrations: (GameContext -> unit) list
  Profiler: FrameProfiler voption
}
