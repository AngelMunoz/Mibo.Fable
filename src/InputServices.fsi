namespace Mibo.Input

open System
open Mibo.Vectors
open Mibo.Elmish

/// <summary>
/// Service accessors and the concrete <c>IInput</c> factory hook.
/// </summary>
/// <remarks>
/// <c>create</c> stays backend-local (it polls the native API). The accessors
/// here let user and framework code retrieve the registered <see cref="T:Mibo.Input.IInput"/>
/// from a <see cref="T:Mibo.Elmish.GameContext"/> without referencing a backend.
/// </remarks>
module Input =
  /// <summary>Attempts to get the registered <see cref="T:Mibo.Input.IInput"/> service.</summary>
  val tryGetService: ctx: GameContext -> IInput voption
  /// <summary>Gets the registered <see cref="T:Mibo.Input.IInput"/> service.</summary>
  /// <exception cref="T:System.Exception">Thrown when no IInput is registered (use <see cref="M:Mibo.Elmish.Program.withInput"/>).</exception>
  val getService: ctx: GameContext -> IInput

/// <summary>Keyboard subscription helpers.</summary>
module Keyboard =
  /// <summary>Dispatches a message for each key press and key release.</summary>
  val listen:
    onPressed: (KeyCode -> 'Msg) ->
    onReleased: (KeyCode -> 'Msg) ->
    ctx: GameContext ->
      Sub<'Msg>

  /// <summary>Dispatches a message for each key press.</summary>
  val onPressed: handler: (KeyCode -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
  /// <summary>Dispatches a message for each key release.</summary>
  val onReleased: handler: (KeyCode -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

/// <summary>Mouse subscription helpers.</summary>
module Mouse =
  /// <summary>Dispatches a message with the full mouse delta each frame it fires.</summary>
  val listen: handler: (MouseDelta -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
  /// <summary>Dispatches a message with the cursor position whenever it moves.</summary>
  val onMove: handler: (Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

  /// <summary>Dispatches a message for each mouse button press.</summary>
  val onButton:
    handler: (MouseButtonCode * Vector2 -> 'Msg) ->
    ctx: GameContext ->
      Sub<'Msg>

  /// <summary>Dispatches a message for each left button press.</summary>
  val onLeftClick: handler: (Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
  /// <summary>Dispatches a message for each right button press.</summary>
  val onRightClick: handler: (Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
  /// <summary>Dispatches a message for each scroll-wheel delta.</summary>
  val onScroll: handler: (float32 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

/// <summary>Touch subscription helpers.</summary>
module Touch =
  /// <summary>Dispatches a message with all active touch points each frame it fires.</summary>
  val listen: handler: (TouchPoint[] -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

/// <summary>Gamepad subscription helpers.</summary>
module Gamepad =
  /// <summary>Dispatches a message with every gamepad delta (all players).</summary>
  val listen: handler: (GamepadDelta -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

  /// <summary>Dispatches a message with gamepad deltas for one player only.</summary>
  val listenPlayer:
    player: int ->
    handler: (GamepadDelta -> 'Msg) ->
    ctx: GameContext ->
      Sub<'Msg>

  /// <summary>Dispatches a message with the player index when a gamepad connects.</summary>
  val onConnected: handler: (int -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
  /// <summary>Dispatches a message with the player index when a gamepad disconnects.</summary>
  val onDisconnected: handler: (int -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

  /// <summary>Dispatches a message for every gamepad connection state change.</summary>
  val onConnectionChange:
    handler: (GamepadConnection -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

/// <summary>Gesture subscription helpers.</summary>
module Gesture =
  /// <summary>Dispatches a message with each detected gesture delta.</summary>
  val listen: handler: (GestureDelta -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
