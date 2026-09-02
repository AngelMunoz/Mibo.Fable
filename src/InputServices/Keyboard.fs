module Mibo.Input.Keyboard

open Mibo.Elmish

let listen
  (onPressed: KeyCode -> 'Msg)
  (onReleased: KeyCode -> 'Msg)
  (ctx: GameContext)
  : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Keyboard/listen"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .KeyboardDelta.Subscribe(fun delta ->
        for k in delta.Pressed do
          dispatch(onPressed k)

        for k in delta.Released do
          dispatch(onReleased k))

  Sub.Active(subId, subscribe)

let onPressed (handler: KeyCode -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Keyboard/onPressed"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .KeyboardDelta.Subscribe(fun delta ->
        for k in delta.Pressed do
          dispatch(handler k))

  Sub.Active(subId, subscribe)

let onReleased (handler: KeyCode -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Keyboard/onReleased"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .KeyboardDelta.Subscribe(fun delta ->
        for k in delta.Released do
          dispatch(handler k))

  Sub.Active(subId, subscribe)
