namespace Mibo.Elmish

open System
open System.Threading.Tasks
open Fable.Core

type Effect<'Msg> = delegate of ('Msg -> unit) -> unit

[<Struct>]
type Cmd<'Msg> =
  | Empty
  | Msg of msg: 'Msg
  | Single of single: Effect<'Msg>
  | Batch of batch: Effect<'Msg>[]
  | DeferNextFrame of batch: Effect<'Msg>[]
  | NowAndDeferNextFrame of now: Effect<'Msg>[] * next: Effect<'Msg>[]
  | Quit
