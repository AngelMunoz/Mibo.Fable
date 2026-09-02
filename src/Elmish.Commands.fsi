namespace Mibo.Elmish

open System
open System.Threading.Tasks
open Fable.Core

/// <summary>
/// Represents a side effect that can dispatch messages to the Elmish runtime.
/// </summary>
/// <remarks>
/// Effects are the building blocks of commands. They are executed asynchronously
/// by the runtime and can dispatch one or more messages back to the update loop.
/// </remarks>
/// <example>
/// <code>
/// let myEffect = Effect&lt;MyMsg&gt;(fun dispatch -&gt;
///     // Do some side effect work
///     dispatch (DataLoaded result)
/// )
/// </code>
/// </example>
type Effect<'Msg> = delegate of ('Msg -> unit) -> unit

/// <summary>
/// Represents a command that produces side effects in the Elmish runtime.
/// </summary>
/// <remarks>
/// Commands are returned from <c>init</c> and <c>update</c> functions to schedule
/// side effects that run outside the pure update cycle. They can dispatch
/// messages back into the runtime, either immediately or deferred.
/// </remarks>
[<NoComparison>]
[<Struct>]
type Cmd<'Msg> =
  /// No-op command (use <see cref="M:Mibo.Elmish.Cmd.none"/>)
  | Empty
  /// A message to dispatch directly without wrapping in an effect
  | Msg of msg: 'Msg
  /// Single effect to execute
  | Single of single: Effect<'Msg>
  /// Multiple effects to execute in this frame
  | Batch of batch: Effect<'Msg>[]
  /// Effects deferred until the next frame begins
  | DeferNextFrame of batch: Effect<'Msg>[]
  /// Combination of immediate and deferred effects
  | NowAndDeferNextFrame of now: Effect<'Msg>[] * next: Effect<'Msg>[]
  /// Signals the runtime to exit after this frame
  | Quit
