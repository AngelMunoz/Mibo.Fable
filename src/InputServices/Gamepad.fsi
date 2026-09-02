/// <summary>Gamepad subscription helpers.</summary>
module Mibo.Input.Gamepad

open Mibo.Elmish

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
