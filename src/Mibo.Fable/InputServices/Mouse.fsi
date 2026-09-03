/// <summary>Mouse subscription helpers.</summary>
module Mibo.Input.Mouse

open Mibo.Vectors
open Mibo.Elmish

/// <summary>Dispatches a message with the full mouse delta each frame it fires.</summary>
val listen: handler: (MouseDelta -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
/// <summary>Dispatches a message with the cursor position whenever it moves.</summary>
val onMove: handler: (Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

/// <summary>Dispatches a message for each mouse button press.</summary>
val onButton:
  handler: (MouseButtonCode * Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>

/// <summary>Dispatches a message for each left button press.</summary>
val onLeftClick: handler: (Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
/// <summary>Dispatches a message for each right button press.</summary>
val onRightClick: handler: (Vector2 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
/// <summary>Dispatches a message for each scroll-wheel delta.</summary>
val onScroll: handler: (float32 -> 'Msg) -> ctx: GameContext -> Sub<'Msg>
