/// <summary>Keyboard subscription helpers.</summary>
module Mibo.Input.Keyboard

open Mibo.Elmish

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
