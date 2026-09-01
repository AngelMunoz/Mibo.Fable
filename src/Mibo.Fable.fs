module Mibo.Fable.Exports

open System
open Fable.Core
open Mibo.Elmish

/// Creates a headless simulation runner.
///
/// <param name="init">Returns the starting model.</param>
/// <param name="update">Receives a message and the current model, returns the next model.</param>
/// <param name="width">Virtual viewport width in pixels (default 800 when null-ish from JS).</param>
/// <param name="height">Virtual viewport height in pixels (default 600 when null-ish from JS).</param>
let createRunner
  (init: unit -> 'Model)
  (update: 'Msg -> 'Model -> 'Model)
  width
  height
  : HeadlessRunner<'Model, 'Msg> =
  let program =
    HeadlessProgram.mkHeadless
      (fun _ -> struct (init(), Cmd.none))
      (fun msg model -> struct (update msg model, Cmd.none))

  new HeadlessRunner<'Model, 'Msg>(program, width, height)

/// Creates a headless runner that dispatches a tick message once per stepped
/// frame, so time-driven simulations advance. The mapper receives the frame
/// length in milliseconds and returns the message to dispatch.
let createRunnerWithTick
  (init: unit -> 'Model)
  (update: 'Msg -> 'Model -> 'Model)
  (tick: float -> 'Msg)
  width
  height
  : HeadlessRunner<'Model, 'Msg> =
  let program =
    HeadlessProgram.mkHeadless
      (fun _ -> struct (init(), Cmd.none))
      (fun msg model -> struct (update msg model, Cmd.none))
    |> HeadlessProgram.withTick(fun time ->
      time.ElapsedGameTime.TotalMilliseconds |> tick)

  new HeadlessRunner<'Model, 'Msg>(program, width, height)

/// Advances the simulation by one frame of the given length in milliseconds.
let stepFrame (ms: float) (runner: HeadlessRunner<'Model, 'Msg>) : unit =
  runner.Step(TimeSpan.FromMilliseconds ms)

/// Advances the simulation by <c>count</c> frames of <c>ms</c> milliseconds each.
let stepFrames
  (count: int)
  (ms: float)
  (runner: HeadlessRunner<'Model, 'Msg>)
  : unit =
  runner.StepN(count, TimeSpan.FromMilliseconds ms)

/// Advances the simulation until the predicate on the model returns true
/// (or 10000 steps were taken). Returns true when the predicate was met.
let stepUntil
  (predicate: 'Model -> bool)
  (ms: float)
  (runner: HeadlessRunner<'Model, 'Msg>)
  : bool =
  runner.StepUntil(predicate, TimeSpan.FromMilliseconds ms)

/// Sends a message into the simulation.
let dispatch (msg: 'Msg) (runner: HeadlessRunner<'Model, 'Msg>) : unit =
  runner.Dispatch msg

/// Sends several messages into the simulation, in order.
let dispatchMany
  (msgs: 'Msg seq)
  (runner: HeadlessRunner<'Model, 'Msg>)
  : unit =
  runner.DispatchMany msgs

/// The current model.
let model(runner: HeadlessRunner<'Model, 'Msg>) : 'Model = runner.Model

/// Whether the simulation received a quit signal.
let shouldQuit(runner: HeadlessRunner<'Model, 'Msg>) : bool = runner.ShouldQuit

/// Total time simulated so far, in milliseconds.
let elapsedMs(runner: HeadlessRunner<'Model, 'Msg>) : float =
  runner.GameTime.TotalTime.TotalMilliseconds

/// Releases the runner's subscriptions and observers.
let dispose(runner: HeadlessRunner<'Model, 'Msg>) : unit = runner.Dispose()

// ─────────────────────────────────────────────────────────────────────────────
// The adaptive (signals) execution model — State · Projection · Update ·
// Force, the counterpart of Mibo.Adaptive's AdaptiveHeadless. The program
// mutates signal roots instead of returning models; observers receive the
// readonly frame forced at the end of every step.
// ─────────────────────────────────────────────────────────────────────────────

open Mibo.Signals

/// Creates a signals runner. `init` builds the graph (roots and
/// projections) and returns the frame force; `update` is the per-frame
/// phase that reads projections and writes roots.
let createAdaptiveRunner
  (init: SignalsFrameContext -> SignalsInit<'Frame>)
  (update: SignalsContext -> GameTime -> unit)
  width
  height
  : SignalsHeadless<'Frame> =
  new SignalsHeadless<'Frame>(
    SignalsProgram.mkProgram init update,
    width,
    height
  )

/// Creates a signals runner with framework-managed fixed-step sub-stepping:
/// each frame's delta is converted into zero or more `stepSeconds` sub-steps
/// (capped at `maxStepsPerFrame`); the frame is forced once at the end.
let createAdaptiveRunnerWithFixedStep
  (init: SignalsFrameContext -> SignalsInit<'Frame>)
  (update: SignalsContext -> GameTime -> unit)
  (stepSeconds: float)
  (maxStepsPerFrame: int)
  width
  height
  : SignalsHeadless<'Frame> =
  let program =
    SignalsProgram.mkProgram init update
    |> SignalsProgram.withFixedStep {
      StepSeconds = float32 stepSeconds
      MaxStepsPerFrame = maxStepsPerFrame
      MaxFrameSeconds = ValueNone
    }

  new SignalsHeadless<'Frame>(program, width, height)

/// Advances the adaptive simulation by one frame of the given length in
/// milliseconds.
let stepAdaptiveFrame (ms: float) (runner: SignalsHeadless<'Frame>) : unit =
  runner.Step(TimeSpan.FromMilliseconds ms)

/// Advances the adaptive simulation by <c>count</c> frames.
let stepAdaptiveFrames
  (count: int)
  (ms: float)
  (runner: SignalsHeadless<'Frame>)
  : unit =
  runner.StepN(count, TimeSpan.FromMilliseconds ms)

/// Advances until the predicate on the forced frame returns true.
let stepAdaptiveUntil
  (predicate: 'Frame -> bool)
  (ms: float)
  (runner: SignalsHeadless<'Frame>)
  : bool =
  runner.StepUntil(predicate, TimeSpan.FromMilliseconds ms)

/// Posts boundary work onto the runner — the host's injection point
/// (input events, external writes). It runs at the start of the next
/// step, before Update.
let postAdaptiveIntent
  (work: unit -> unit)
  (runner: SignalsHeadless<'Frame>)
  : unit =
  runner.Intents.PostNextFrame work

/// Starts `work` immediately; when the promise settles, `done` runs at the
/// next post drain (after Update, before the frame is forced) — errors go
/// to `onError` (or rethrow) at the same drain. The counterpart of the
/// .NET postTask/postAsync.
let postAdaptiveTask
  (work: unit -> JS.Promise<'T>)
  (onDone: 'T -> unit)
  (runner: SignalsHeadless<'Frame>)
  : unit =
  runner.Intents.PostTask(work, onDone)

/// Registers a per-frame callback receiving the forced readonly frame.
let onAdaptiveFrame
  (onNext: 'Frame -> unit)
  (runner: SignalsHeadless<'Frame>)
  : unit =
  runner.Observe(fun (struct (_, frame, _)) -> onNext frame)

/// The last forced frame.
let adaptiveFrame(runner: SignalsHeadless<'Frame>) : 'Frame = runner.Frame

/// Whether the adaptive simulation received an exit request.
let adaptiveShouldQuit(runner: SignalsHeadless<'Frame>) : bool =
  runner.ShouldQuit

/// Total time simulated so far, in milliseconds.
let adaptiveElapsedMs(runner: SignalsHeadless<'Frame>) : float =
  runner.GameTime.TotalTime.TotalMilliseconds

/// Releases the runner's disposables.
let disposeAdaptive(runner: SignalsHeadless<'Frame>) : unit = runner.Dispose()

/// Groups signal writes so dependent work runs once after the batch.
let batchWrites(work: unit -> unit) : unit = Signals.batch work

/// Runs `work` now and again whenever any signal it read changes; returns
/// the disposer. The push side — useful for view binding in the host.
let onSignalChange(work: unit -> unit) : (unit -> unit) = Signals.effect work
