module Mibo.Input.Gesture

open Mibo.Elmish

let listen (handler: GestureDelta -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Gesture/listen"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .GestureDelta.Subscribe(fun delta -> dispatch(handler delta))

  Sub.Active(subId, subscribe)
