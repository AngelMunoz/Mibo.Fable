/// <summary>Builder helpers for <see cref="T:Mibo.Elmish.GameConfig"/>.</summary>
module Mibo.Elmish.GameConfig

open Mibo.Windowing

/// The default configuration: 800x600, titled "Mibo F#", windowed, not resizable.
val defaultConfig: GameConfig

val withWidth: width: int -> config: GameConfig -> GameConfig

val withHeight: height: int -> config: GameConfig -> GameConfig

val withMinWidth: width: int -> config: GameConfig -> GameConfig

val withMinHeight: height: int -> config: GameConfig -> GameConfig

val withTitle: title: string -> config: GameConfig -> GameConfig

/// <summary>Cap the render rate at the given FPS (MonoGame fixed timestep at 1/fps,
/// Raylib <c>SetTargetFPS</c>). Omit it (the default) to leave the backend's
/// framerate behavior untouched.</summary>
val withTargetFPS: fps: int -> config: GameConfig -> GameConfig

/// <summary>Allow the user to resize the window.</summary>
val withResizable: config: GameConfig -> GameConfig

/// <summary>Set the window presentation mode at startup (windowed, borderless
/// fullscreen, or exclusive fullscreen).</summary>
val withWindowMode: mode: WindowMode -> config: GameConfig -> GameConfig
