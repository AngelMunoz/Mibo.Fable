/// <summary>
/// The JavaScript-facing entry point of Mibo.Fable.
///
/// The core simulation lives in the <c>Mibo</c> namespaces (Elmish loop, input,
/// layout, draw commands). F# game code should use those directly — Fable
/// compiles it all together. This module wraps the small surface that a
/// JavaScript/TypeScript host needs to drive the headless simulation runner,
/// hiding F#-specific shapes (currying, TimeSpan-as-ticks) behind plain calls.
/// </summary>
module Mibo.Fable.Exports

open System
open Mibo.Elmish

/// Creates a headless simulation runner.
///
/// <param name="init">Returns the starting model.</param>
/// <param name="update">Receives a message and the current model, returns the next model.</param>
/// <param name="width">Virtual viewport width in pixels (default 800 when null-ish from JS).</param>
/// <param name="height">Virtual viewport height in pixels (default 600 when null-ish from JS).</param>
let createRunner (init: unit -> 'Model) (update: 'Msg -> 'Model -> 'Model) width height
    : HeadlessRunner<'Model, 'Msg> =
  let program =
    HeadlessProgram.mkHeadless
      (fun _ -> struct (init (), Cmd.none))
      (fun msg model -> struct (update msg model, Cmd.none))

  new HeadlessRunner<'Model, 'Msg>(program, width, height)

/// Creates a headless runner that dispatches a tick message once per stepped
/// frame, so time-driven simulations advance. The mapper receives the frame
/// length in milliseconds and returns the message to dispatch.
let createRunnerWithTick (init: unit -> 'Model) (update: 'Msg -> 'Model -> 'Model)
    (tick: float -> 'Msg) width height
    : HeadlessRunner<'Model, 'Msg> =
  let program =
    HeadlessProgram.mkHeadless
      (fun _ -> struct (init (), Cmd.none))
      (fun msg model -> struct (update msg model, Cmd.none))
    |> HeadlessProgram.withTick (fun time ->
      time.ElapsedGameTime.TotalMilliseconds |> tick)

  new HeadlessRunner<'Model, 'Msg>(program, width, height)

/// Advances the simulation by one frame of the given length in milliseconds.
let stepFrame (ms: float) (runner: HeadlessRunner<'Model, 'Msg>) : unit =
  runner.Step(TimeSpan.FromMilliseconds ms)

/// Advances the simulation by <c>count</c> frames of <c>ms</c> milliseconds each.
let stepFrames (count: int) (ms: float) (runner: HeadlessRunner<'Model, 'Msg>) : unit =
  runner.StepN(count, TimeSpan.FromMilliseconds ms)

/// Advances the simulation until the predicate on the model returns true
/// (or 10000 steps were taken). Returns true when the predicate was met.
let stepUntil (predicate: 'Model -> bool) (ms: float) (runner: HeadlessRunner<'Model, 'Msg>) : bool =
  runner.StepUntil(predicate, TimeSpan.FromMilliseconds ms)

/// Sends a message into the simulation.
let dispatch (msg: 'Msg) (runner: HeadlessRunner<'Model, 'Msg>) : unit =
  runner.Dispatch msg

/// Sends several messages into the simulation, in order.
let dispatchMany (msgs: 'Msg seq) (runner: HeadlessRunner<'Model, 'Msg>) : unit =
  runner.DispatchMany msgs

/// The current model.
let model (runner: HeadlessRunner<'Model, 'Msg>) : 'Model = runner.Model

/// Whether the simulation received a quit signal.
let shouldQuit (runner: HeadlessRunner<'Model, 'Msg>) : bool = runner.ShouldQuit

/// Total time simulated so far, in milliseconds.
let elapsedMs (runner: HeadlessRunner<'Model, 'Msg>) : float =
  runner.GameTime.TotalTime.TotalMilliseconds

/// Releases the runner's subscriptions and observers.
let dispose (runner: HeadlessRunner<'Model, 'Msg>) : unit = runner.Dispose()
