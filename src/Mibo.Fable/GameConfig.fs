[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Elmish.GameConfig

open Mibo.Windowing

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

/// <summary>Cap the render rate at the given FPS (MonoGame fixed timestep at 1/fps,
/// Raylib <c>SetTargetFPS</c>). Omit it (the default) to leave the backend's
/// framerate behavior untouched.</summary>
let withTargetFPS fps config = {
  config with
      TargetFPS = ValueSome fps
}

/// <summary>Allow the user to resize the window.</summary>
let withResizable config = { config with Resizable = true }

/// <summary>Set the window presentation mode at startup (windowed, borderless
/// fullscreen, or exclusive fullscreen).</summary>
let withWindowMode mode config = { config with WindowMode = mode }
