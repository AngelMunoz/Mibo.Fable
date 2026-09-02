module Mibo.Input.Touch

open Mibo.Elmish

let listen (handler: TouchPoint[] -> 'Msg) (ctx: GameContext) : Sub<'Msg> =
  let subId = SubId.ofString "Mibo/Input/Touch/listen"

  let subscribe(dispatch: Dispatch<'Msg>) =
    (Input.getService ctx)
      .TouchDelta.Subscribe(fun delta -> dispatch(handler delta.Touches))

  Sub.Active(subId, subscribe)
