module Mibo.Elmish.Cmd

open System.Threading.Tasks
open Fable.Core

/// <summary>An empty command that does nothing. Use when no side effects are needed.</summary>
val none: Cmd<'Msg>

/// <summary>Signals the runtime to exit after the current frame completes.</summary>
val signalExit: Cmd<'Msg>

/// <summary>Wraps a raw effect delegate into a command.</summary>
val inline ofEffect: eff: Effect<'Msg> -> Cmd<'Msg>

/// <summary>
/// Creates a command that immediately dispatches the given message.
/// </summary>
/// <remarks>
/// Useful for triggering follow-up messages from within the update cycle.
/// </remarks>
val inline ofMsg: msg: 'Msg -> Cmd<'Msg>

/// <summary>
/// Defer command execution until the next frame.
/// </summary>
/// <remarks>
/// In the runtime, deferred commands are executed at the start of the next frame,
/// before <c>Tick</c> is enqueued. This is useful for avoiding infinite update loops
/// or for scheduling work that should happen after the current frame completes.
/// </remarks>
val inline deferNextFrame: cmd: Cmd<'Msg> -> Cmd<'Msg>

/// Splits a command into its immediate and deferred effect arrays.
val inline split: cmd: Cmd<'Msg> -> struct (Effect<'Msg>[] * Effect<'Msg>[])

/// <summary>
/// Map a command producing messages of type 'A into a command producing messages of type 'Msg.
/// </summary>
/// <remarks>
/// This is the command equivalent of <see cref="M:Mibo.Elmish.Sub.map"/> and is required for parent-child composition
/// in nested Elmish architectures where child modules have their own message types.
/// </remarks>
val map: f: ('A -> 'Msg) -> cmd: Cmd<'A> -> Cmd<'Msg>

/// <summary>
/// Combines multiple commands into a single command.
/// </summary>
/// <remarks>
/// Commands are merged efficiently, preserving the distinction between
/// immediate and deferred effects. Use this when returning multiple commands
/// from a single update branch.
/// </remarks>
val batch: cmds: seq<Cmd<'Msg>> -> Cmd<'Msg>

val batch2: a: Cmd<'Msg> * b: Cmd<'Msg> -> Cmd<'Msg>
val batch3: a: Cmd<'Msg> * b: Cmd<'Msg> * c: Cmd<'Msg> -> Cmd<'Msg>

val batch4:
  a: Cmd<'Msg> * b: Cmd<'Msg> * c: Cmd<'Msg> * d: Cmd<'Msg> -> Cmd<'Msg>

/// Creates a command from an F# async workflow.
///
/// The async is started immediately and the result is mapped to a message.
/// If the async throws, the error handler is invoked instead.
///
/// ## Example
/// ```fsharp
/// Cmd.ofAsync (loadDataAsync url) DataLoaded LoadError
/// ```
val ofAsync:
  task: Async<'T> ->
  ofSuccess: ('T -> 'Msg) ->
  ofError: (exn -> 'Msg) ->
    Cmd<'Msg>

/// <summary>
/// Creates a command from a .NET Task.
/// </summary>
/// <remarks>
/// The arguments are passed separately due to tasks being hot by nature.
/// This guarantees that the execution is not made in the loop's thread.
/// The task result is awaited and mapped to a message.
/// If the task throws, the error handler is invoked instead.
/// </remarks>
/// <example>
/// <code>
/// let work url =
///   httpClient.GetAsync url
/// Cmd.ofTask work url ResponseReceived RequestFailed
/// </code>
/// </example>
val ofTask:
  tsk: ('Args -> Task<'T>) ->
  args: 'Args ->
  ofSuccess: ('T -> 'Msg) ->
  ofError: (exn -> 'Msg) ->
    Cmd<'Msg>

/// Creates a command from a function returning a JS promise — the
/// web-first counterpart of <see cref="M:Mibo.Elmish.Cmd.ofTask"/>.
///
/// The work is started when the command executes (after the update that
/// returned it); when the promise resolves the result is mapped to a
/// message and dispatched, landing at the next frame's message drain.
/// If the promise rejects, the error handler is invoked instead.
///
/// ## Example
/// ```fsharp
/// Cmd.ofPromise (fetchJson url) () DataLoaded LoadError
/// ```
val ofPromise:
  work: ('Args -> JS.Promise<'T>) ->
  args: 'Args ->
  ofSuccess: ('T -> 'Msg) ->
  ofError: (exn -> 'Msg) ->
    Cmd<'Msg>
