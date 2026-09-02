module Mibo.Elmish.GameContext

open System
open System.Collections.Generic

let internal create(width: int, height: int) = GameContext(width, height)

let inline internal register<'T> (svc: 'T) (ctx: GameContext) =
  ctx.Services[typeof<'T>] <- box svc

let inline tryGetService<'T>(ctx: GameContext) : 'T voption =
  match ctx.Services.TryGetValue(typeof<'T>) with
  | true, svc -> ValueSome(unbox<'T> svc)
  | _ -> ValueNone

let inline getService<'T>(ctx: GameContext) : 'T =
  match tryGetService<'T> ctx with
  | ValueSome svc -> svc
  | ValueNone ->
    failwithf "Service %s not registered in GameContext" typeof<'T>.Name
