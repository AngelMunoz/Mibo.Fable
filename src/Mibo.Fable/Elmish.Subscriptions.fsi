namespace Mibo.Elmish

open System
open System.Collections.Generic
open FSharp.UMX

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
