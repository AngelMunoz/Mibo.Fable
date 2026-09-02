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
open Fable.Core
open Mibo.Elmish

/// Creates a headless simulation runner.
///
/// <param name="init">Returns the starting model.</param>
/// <param name="update">Receives a message and the current model, returns the next model.</param>
/// <param name="width">Virtual viewport width in pixels (default 800 when null-ish from JS).</param>
/// <param name="height">Virtual viewport height in pixels (default 600 when null-ish from JS).</param>
val createRunner:
  init: (unit -> 'Model) ->
  update: ('Msg -> 'Model -> 'Model) ->
  width: int ->
  height: int ->
    HeadlessRunner<'Model, 'Msg>

/// Creates a headless runner that dispatches a tick message once per stepped
/// frame, so time-driven simulations advance. The mapper receives the frame
/// length in milliseconds and returns the message to dispatch.
val createRunnerWithTick:
  init: (unit -> 'Model) ->
  update: ('Msg -> 'Model -> 'Model) ->
  tick: (float -> 'Msg) ->
  width: int ->
  height: int ->
    HeadlessRunner<'Model, 'Msg>

/// Advances the simulation by one frame of the given length in milliseconds.
val stepFrame: ms: float -> runner: HeadlessRunner<'Model, 'Msg> -> unit

/// Advances the simulation by <c>count</c> frames of <c>ms</c> milliseconds each.
val stepFrames:
  count: int -> ms: float -> runner: HeadlessRunner<'Model, 'Msg> -> unit

/// Advances the simulation until the predicate on the model returns true
/// (or 10000 steps were taken). Returns true when the predicate was met.
val stepUntil:
  predicate: ('Model -> bool) ->
  ms: float ->
  runner: HeadlessRunner<'Model, 'Msg> ->
    bool

/// Sends a message into the simulation.
val dispatch: msg: 'Msg -> runner: HeadlessRunner<'Model, 'Msg> -> unit

/// Sends several messages into the simulation, in order.
val dispatchMany:
  msgs: ('Msg seq) -> runner: HeadlessRunner<'Model, 'Msg> -> unit

/// The current model.
val model: runner: HeadlessRunner<'Model, 'Msg> -> 'Model

/// Whether the simulation received a quit signal.
val shouldQuit: runner: HeadlessRunner<'Model, 'Msg> -> bool

/// Total time simulated so far, in milliseconds.
val elapsedMs: runner: HeadlessRunner<'Model, 'Msg> -> float

/// Releases the runner's subscriptions and observers.
val dispose: runner: HeadlessRunner<'Model, 'Msg> -> unit

// ─────────────────────────────────────────────────────────────────────────────
// The adaptive (signals) execution model — State · Projection · Update ·
// Force, the counterpart of Mibo.Adaptive's AdaptiveHeadless. The program
// mutates signal roots instead of returning models; observers receive the
// readonly frame forced at the end of every step.
// ─────────────────────────────────────────────────────────────────────────────

open Mibo.Fable.Adaptive

/// Creates a signals runner. `init` builds the graph (roots and
/// projections) and returns the frame force; `update` is the per-frame
/// phase that reads projections and writes roots.
val createAdaptiveRunner:
  init: (AdaptiveFrameContext -> AdaptiveInit<'Frame>) ->
  update: (AdaptiveContext -> GameTime -> unit) ->
  width: int ->
  height: int ->
    AdaptiveHeadless<'Frame>

/// Creates a signals runner with framework-managed fixed-step sub-stepping:
/// each frame's delta is converted into zero or more `stepSeconds` sub-steps
/// (capped at `maxStepsPerFrame`); the frame is forced once at the end.
val createAdaptiveRunnerWithFixedStep:
  init: (AdaptiveFrameContext -> AdaptiveInit<'Frame>) ->
  update: (AdaptiveContext -> GameTime -> unit) ->
  stepSeconds: float ->
  maxStepsPerFrame: int ->
  width: int ->
  height: int ->
    AdaptiveHeadless<'Frame>

/// Advances the adaptive simulation by one frame of the given length in
/// milliseconds.
val stepAdaptiveFrame: ms: float -> runner: AdaptiveHeadless<'Frame> -> unit

/// Advances the adaptive simulation by <c>count</c> frames.
val stepAdaptiveFrames:
  count: int -> ms: float -> runner: AdaptiveHeadless<'Frame> -> unit

/// Advances until the predicate on the forced frame returns true.
val stepAdaptiveUntil:
  predicate: ('Frame -> bool) ->
  ms: float ->
  runner: AdaptiveHeadless<'Frame> ->
    bool

/// Posts boundary work onto the runner — the host's injection point
/// (input events, external writes). It runs at the start of the next
/// step, before Update.
val postAdaptiveIntent:
  work: (unit -> unit) -> runner: AdaptiveHeadless<'Frame> -> unit

/// Starts `work` immediately; when the promise settles, `done` runs at the
/// next post drain (after Update, before the frame is forced) — errors go
/// to `onError` (or rethrow) at the same drain. The counterpart of the
/// .NET postTask/postAsync.
val postAdaptiveTask:
  work: (unit -> JS.Promise<'T>) ->
  onDone: ('T -> unit) ->
  runner: AdaptiveHeadless<'Frame> ->
    unit

/// Registers a per-frame callback receiving the forced readonly frame.
val onAdaptiveFrame:
  onNext: ('Frame -> unit) -> runner: AdaptiveHeadless<'Frame> -> unit

/// The last forced frame.
val adaptiveFrame: runner: AdaptiveHeadless<'Frame> -> 'Frame

/// Whether the adaptive simulation received an exit request.
val adaptiveShouldQuit: runner: AdaptiveHeadless<'Frame> -> bool

/// Total time simulated so far, in milliseconds.
val adaptiveElapsedMs: runner: AdaptiveHeadless<'Frame> -> float

/// Releases the runner's disposables.
val disposeAdaptive: runner: AdaptiveHeadless<'Frame> -> unit
