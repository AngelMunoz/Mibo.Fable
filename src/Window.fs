namespace Mibo.Windowing

open Mibo.Elmish

// ─────────────────────────────────────────────────────────────────────────────
// Window management: the startup contract (WindowMode on GameConfig) and the
// runtime contract (IWindow, registered by every runtime host).
//
// The contract is defined in Mibo.Core; each backend (Mibo.Raylib,
// Mibo.MonoGame) supplies a concrete IWindow over its native window API and
// registers it into the GameContext at startup, unconditionally (like
// IAssets). User code retrieves it via the Window accessors below.
// ─────────────────────────────────────────────────────────────────────────────

type WindowMode =
  | Windowed
  | BorderlessFullscreen
  | Fullscreen

type IWindow =
  abstract Mode: WindowMode
  abstract IsFullscreen: bool
  abstract SetMode: mode: WindowMode -> unit
  abstract ToggleFullscreen: unit -> unit
  abstract SetSize: width: int * height: int -> unit

/// <summary>Service accessors for the registered <see cref="T:Mibo.Windowing.IWindow"/>.</summary>
module Window =

  /// <summary>Attempts to get the registered <see cref="T:Mibo.Windowing.IWindow"/> service.</summary>
  let inline tryGetService(ctx: GameContext) : IWindow voption =
    GameContext.tryGetService<IWindow> ctx

  /// <summary>Gets the registered <see cref="T:Mibo.Windowing.IWindow"/> service.</summary>
  /// <exception cref="T:System.Exception">Thrown when no IWindow is registered.</exception>
  let inline getService(ctx: GameContext) : IWindow =
    match tryGetService ctx with
    | ValueSome w -> w
    | ValueNone ->
      failwith "IWindow service not registered by the runtime host."
