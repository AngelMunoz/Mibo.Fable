/// <summary>Touch subscription helpers.</summary>
module Mibo.Input.Touch

open Mibo.Elmish

/// <summary>Dispatches a message with all active touch points each frame it fires.</summary>
val listen: handler: (TouchPoint[] -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
