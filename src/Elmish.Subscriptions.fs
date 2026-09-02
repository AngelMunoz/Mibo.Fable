namespace Mibo.Elmish

open System
open System.Collections.Generic
open FSharp.UMX

type Dispatch<'Msg> = 'Msg -> unit

type Subscribe<'Msg> = Dispatch<'Msg> -> IDisposable

[<Struct>]
type Sub<'Msg> =
  | NoSub
  | Active of SubId * Subscribe<'Msg>
  | BatchSub of Sub<'Msg>[]
