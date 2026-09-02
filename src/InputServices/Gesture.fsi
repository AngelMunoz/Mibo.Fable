/// <summary>Gesture subscription helpers.</summary>
module Mibo.Input.Gesture

open Mibo.Elmish

/// <summary>Dispatches a message with each detected gesture delta.</summary>
val listen: handler: (GestureDelta -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
