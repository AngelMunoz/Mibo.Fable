namespace Mibo.Elmish

open System
open System.Collections.Generic
open FSharp.UMX

/// <summary>
/// Subscription identifier used as the key for subscription diffing.
/// </summary>
/// <remarks>
/// The Elmish runtime uses SubIds to determine which subscriptions to start,
/// stop, or keep running across frames. Use stable, unique IDs for each subscription.
/// Keep this allocation-free in hot paths (avoid list-based IDs).
/// </remarks>
[<Measure>]
type subId

/// A typed string wrapper for subscription identifiers.
type SubId = string<subId>

/// Functions for creating and manipulating subscription identifiers.
module SubId =
  /// <summary>Wraps a raw string into a <see cref="T:Mibo.Elmish.SubId"/>.</summary>
  val inline ofString: value: string -> SubId
  /// <summary>Extracts the raw string value from a <see cref="T:Mibo.Elmish.SubId"/>.</summary>
  val inline value: id: SubId -> string

  /// <summary>
  /// Prefixes a SubId with a namespace for parent-child subscription composition.
  /// </summary>
  /// <example>
  /// <code>
  /// // Creates "Player/moveInput"
  /// SubId.prefix "Player" (SubId.ofString "moveInput")
  /// </code>
  /// </example>
  val inline prefix: prefix: string -> id: SubId -> SubId

/// <summary>A function that dispatches messages to the Elmish update loop.</summary>
type Dispatch<'Msg> = 'Msg -> unit
/// <summary>
/// A function that sets up a subscription and returns a disposable for cleanup.
/// </summary>
/// <remarks>
/// When the runtime calls this, it passes the dispatch function. The returned
/// <see cref="T:System.IDisposable"/> will be called when the subscription is no longer needed.
/// </remarks>
type Subscribe<'Msg> = Dispatch<'Msg> -> IDisposable

/// <summary>
/// Represents a subscription that listens for external events and dispatches messages.
/// </summary>
/// <remarks>
/// Subscriptions are the Elmish way to handle external event sources (input devices,
/// timers, network events). The runtime diffs subscriptions by SubId to determine
/// which to start/stop across frames.
/// </remarks>
[<NoComparison>]
[<Struct>]
type Sub<'Msg> =
  /// No subscription (use <see cref="M:Mibo.Elmish.Sub.none"/>)
  | NoSub
  /// An active subscription with a unique ID
  | Active of SubId * Subscribe<'Msg>
  /// Multiple subscriptions combined
  | BatchSub of Sub<'Msg>[]

/// <summary>
/// Functions for creating and composing Elmish subscriptions.
/// </summary>
/// <remarks>
/// Subscriptions connect external event sources to the Elmish update loop.
/// The runtime automatically manages subscription lifecycle based on SubId diffing.
/// </remarks>
module Sub =
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
