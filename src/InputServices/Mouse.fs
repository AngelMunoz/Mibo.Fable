module Mibo.Input.Mouse

open Mibo.Vectors
open Mibo.Elmish

let listen (handler: MouseDelta -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Mouse/listen"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .MouseDelta.Subscribe(fun delta -> dispatch(handler delta))

  Sub.Active(subId, subscribe)

let onMove (handler: Vector2 -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Mouse/onMove"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .MouseDelta.Subscribe(fun delta ->
        if delta.PositionDelta.X <> 0.0f || delta.PositionDelta.Y <> 0.0f then
          dispatch(handler delta.Position))

  Sub.Active(subId, subscribe)

let onButton
  (handler: MouseButtonCode * Vector2 -> 'Msg)
  (ctx: GameContext)
  : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Mouse/onButton"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .MouseDelta.Subscribe(fun delta ->
        for btn in delta.Buttons.Pressed do
          dispatch(handler(btn, delta.Position)))

  Sub.Active(subId, subscribe)

let onLeftClick (handler: Vector2 -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Mouse/onLeftClick"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .MouseDelta.Subscribe(fun delta ->
        if delta.Buttons.Pressed |> Array.contains MouseButtonCode.Left then
          dispatch(handler delta.Position))

  Sub.Active(subId, subscribe)

let onRightClick (handler: Vector2 -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Mouse/onRightClick"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .MouseDelta.Subscribe(fun delta ->
        if delta.Buttons.Pressed |> Array.contains MouseButtonCode.Right then
          dispatch(handler delta.Position))

  Sub.Active(subId, subscribe)

let onScroll (handler: float32 -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Mouse/onScroll"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .MouseDelta.Subscribe(fun delta ->
        if delta.ScrollDelta <> 0.0f then
          dispatch(handler delta.ScrollDelta))

  Sub.Active(subId, subscribe)
