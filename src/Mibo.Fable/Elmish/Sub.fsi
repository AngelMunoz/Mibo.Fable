/// <summary>
/// Functions for creating and composing Elmish subscriptions.
/// </summary>
/// <remarks>
/// Subscriptions connect external event sources to the Elmish update loop.
/// The runtime automatically manages subscription lifecycle based on SubId diffing.
/// </remarks>
module Mibo.Elmish.Sub

open System
open System.Collections.Generic

/// <summary>An empty subscription that does nothing. Use when no subscriptions are needed.</summary>
val none: Sub<'a>

[<TailCall>]
val internal flatten:
  stack: ResizeArray<Sub<'Msg>> ->
  results: ResizeArray<struct (SubId * Subscribe<'Msg>)> ->
    unit

[<NoComparison>]
[<Struct>]
type private MapWork<'A> =
  | Visit of sub: Sub<'A>
  | BuildBatch of len: int

/// <summary>
/// Combines multiple subscriptions into a single subscription.
/// </summary>
/// <remarks>
/// Subscriptions are merged efficiently and duplicates are not filtered.
/// Use unique SubIds to ensure proper subscription diffing.
/// </remarks>
val batch: subs: seq<Sub<'Msg>> -> Sub<'Msg>

val inline batch2: a: Sub<'Msg> * b: Sub<'Msg> -> Sub<'Msg>
val inline batch3: a: Sub<'Msg> * b: Sub<'Msg> * c: Sub<'Msg> -> Sub<'Msg>

val inline batch4:
  a: Sub<'Msg> * b: Sub<'Msg> * c: Sub<'Msg> * d: Sub<'Msg> -> Sub<'Msg>

/// <summary>
/// Maps a subscription producing messages of type 'A to produce messages of type 'Msg.
/// </summary>
/// <remarks>
/// This is essential for parent-child composition where child modules have
/// their own message types. The idPrefix is prepended to all subscription IDs
/// to namespace them properly.
/// </remarks>
/// <example>
/// <code>
/// // In parent module:
/// let childSub = Child.subscribe ctx |> Sub.map "child" ChildMsg
/// </code>
/// </example>
val map: idPrefix: string -> f: ('A -> 'Msg) -> sub: Sub<'A> -> Sub<'Msg>
