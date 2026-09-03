module Mibo.Input.Gamepad

open Mibo.Elmish

let listen (handler: GamepadDelta -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Gamepad/listen"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .GamepadDelta.Subscribe(fun delta -> dispatch(handler delta))

  Sub.Active(subId, subscribe)

let listenPlayer
  (player: int)
  (handler: GamepadDelta -> 'Msg)
  (ctx: GameContext)
  : Sub<'Msg> =
  let subId = SubId.ofString $"Mibo/Input/Gamepad/listenPlayer/{player}"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .GamepadDelta.Subscribe(fun delta ->
        if delta.PlayerIndex = player then
          dispatch(handler delta))

  Sub.Active(subId, subscribe)

let onConnected (handler: int -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Gamepad/onConnected"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .GamepadConnection.Subscribe(fun conn ->
        if conn.IsConnected then
          dispatch(handler conn.PlayerIndex))

  Sub.Active(subId, subscribe)

let onDisconnected (handler: int -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Gamepad/onDisconnected"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .GamepadConnection.Subscribe(fun conn ->
        if not conn.IsConnected then
          dispatch(handler conn.PlayerIndex))

  Sub.Active(subId, subscribe)

let onConnectionChange
  (handler: GamepadConnection -> 'Msg)
  (ctx: GameContext)
  : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Gamepad/onConnectionChange"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .GamepadConnection.Subscribe(fun conn -> dispatch(handler conn))

  Sub.Active(subId, subscribe)
