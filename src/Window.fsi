namespace Mibo.Windowing

open Mibo.Elmish

/// <summary>Window presentation mode.</summary>
/// <remarks>
/// <c>Fullscreen</c> is an exclusive video-mode switch (raylib
/// <c>FLAG_FULLSCREEN_MODE</c>, MonoGame <c>HardwareModeSwitch = true</c>);
/// the drawn backbuffer can differ from the display size, and pointer
/// coordinates are in display pixels while the viewport is in backbuffer
/// pixels. <c>BorderlessFullscreen</c> keeps the desktop mode and matches
/// the backbuffer to the display, so sizes stay consistent. Prefer it for
/// runtime toggles.
/// </remarks>
type WindowMode =
  /// A regular windowed window.
  | Windowed
  /// Fullscreen window in the desktop video mode. The backbuffer matches the
  /// display, so window sizes and pointer coordinates stay consistent.
  | BorderlessFullscreen
  /// Exclusive fullscreen with a video-mode switch.
  | Fullscreen

/// <summary>Runtime window control, registered by the host in the GameContext.</summary>
/// <remarks>
/// Toggles and mode changes take effect immediately; the runtime's resize
/// tracking updates the context dimensions on the next frame.
/// </remarks>
type IWindow =
  /// The current presentation mode.
  abstract Mode: WindowMode
  /// True when the window is in either fullscreen mode.
  abstract IsFullscreen: bool
  /// Switch the presentation mode.
  abstract SetMode: mode: WindowMode -> unit
  /// <summary>Toggle between <c>Windowed</c> and fullscreen.</summary>
  /// <remarks>
  /// Entering fullscreen uses the mode set in the startup
  /// <see cref="T:Mibo.Elmish.GameConfig"/>; a windowed startup toggles to
  /// <c>BorderlessFullscreen</c>.
  /// </remarks>
  abstract ToggleFullscreen: unit -> unit
  /// <summary>Set the window size in pixels. No effect outside <c>Windowed</c>.</summary>
  abstract SetSize: width: int * height: int -> unit

/// <summary>Service accessors for the registered <see cref="T:Mibo.Windowing.IWindow"/>.</summary>
module Window =

  /// <summary>Attempts to get the registered <see cref="T:Mibo.Windowing.IWindow"/> service.</summary>
  val inline tryGetService: ctx: GameContext -> IWindow voption

  /// <summary>Gets the registered <see cref="T:Mibo.Windowing.IWindow"/> service.</summary>
  /// <exception cref="T:System.Exception">Thrown when no IWindow is registered.</exception>
  val inline getService: ctx: GameContext -> IWindow
