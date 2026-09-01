namespace Mibo.Signals

open System
open Mibo.Elmish

/// Deferred `unit -> unit` work — the counterpart of the .NET IntentQueue.
/// Two lanes: boundary work runs at the start of the next Step (external
/// events, next-frame work); after-update work runs right after each
/// Update, before the frame is forced. Work posted during a drain runs in
/// the same drain. `PostTask` covers the .NET postTask/postAsync: the
/// work starts immediately (promise concurrency), and its completion
/// re-enters through the after-update lane. True parallelism stays a
/// worker concern — a CPU-heavy task still runs on this thread.
type IntentQueue =
  new: unit -> IntentQueue
  /// Queues work for the next step's boundary, before Update.
  member PostNextFrame: work: (unit -> unit) -> unit
  /// Queues work to run after the next Update (per sub-step), before the
  /// frame is forced.
  member Post: work: (unit -> unit) -> unit

  /// Starts `work` immediately; when the promise settles, the completion
  /// runs at the next post drain (after Update, before the frame is
  /// forced) — the counterpart of the .NET postTask/postAsync, whose
  /// completion returns via post. Rejections route through `onError`;
  /// without one, the error is rethrown at the drain so it is not
  /// swallowed.
  member PostTask:
    work: (unit -> Fable.Core.JS.Promise<'T>) *
    onDone: ('T -> unit) *
    ?onError: (exn -> unit) ->
      unit

  member internal DrainBoundary: unit -> unit
  member internal DrainAfterUpdate: unit -> unit

/// The Init-phase context: the framework-owned roots plus the intent queue.
/// Work posted here runs at the startup drain, right after Init returns and
/// before the first frame is forced — the counterpart of the MVU init Cmd.
type SignalsFrameContext =
  internal new:
    ctx: GameContext *
    time: CVal<GameTime> *
    exitRequested: CVal<bool> *
    intents: IntentQueue ->
      SignalsFrameContext

  /// The framework-owned time root. The runner writes it at the start of
  /// every step (once per fixed sub-step); projections may depend on it.
  member Time: CVal<GameTime>
  /// Set to true to make the runner stop — the counterpart of
  /// `CVal.set true ctx.ExitRequested`.
  member ExitRequested: CVal<bool>
  /// The GameContext the runner owns: window dimensions and services.
  member Context: GameContext
  member WindowWidth: int
  member WindowHeight: int
  /// Deferred work, as documented on IntentQueue.
  member Intents: IntentQueue

/// The Update-phase context: everything Init received. The frame builder
/// must not post: it runs after the post drain.
type SignalsContext =
  internal new:
    frameCtx: SignalsFrameContext * intents: IntentQueue -> SignalsContext

  member Time: CVal<GameTime>
  member ExitRequested: CVal<bool>
  member Context: GameContext
  member WindowWidth: int
  member WindowHeight: int
  member Intents: IntentQueue

/// An adaptive-style init: the frame force plus disposables. Build the graph
/// (roots and projections) in the program's Init and return the force that
/// packs the readonly frame at the end of every step.
type SignalsInit<'Frame> = {
  /// Forces the frame's output projections (each recomputes at most once
  /// if a dependency moved this step) and packs them into 'Frame.
  FrameBuilder: unit -> 'Frame

  /// Disposables released when the runner is disposed.
  Disposables: IDisposable list
}

/// <summary>Helpers for building a <see cref="T:Mibo.Signals.SignalsInit`1"/>.</summary>
module SignalsInit =

  /// Creates an init from a frame builder - no disposables.
  val ofFrameBuilder: frameBuilder: (unit -> 'Frame) -> SignalsInit<'Frame>

  /// Appends disposables released when the runner is disposed.
  val withDisposables:
    disposables: IDisposable list ->
    init: SignalsInit<'Frame> ->
      SignalsInit<'Frame>

  /// Adds a single disposable.
  val withDisposable:
    disposable: IDisposable -> init: SignalsInit<'Frame> -> SignalsInit<'Frame>

/// Fixed-step configuration: converts a variable frame delta into zero or
/// more fixed-size steps per Step call. The frame is forced once at the end,
/// so intermediate sub-steps are integrated but not observed.
type SignalsFixedStepConfig = {
  /// Fixed simulation step size in seconds (e.g. 1/60 = 0.0166667).
  StepSeconds: float32

  /// Maximum fixed steps per frame — prevents the spiral of death after
  /// long stalls; remaining accumulated time is dropped.
  MaxStepsPerFrame: int

  /// Clamps the per-frame delta used for accumulation.
  MaxFrameSeconds: float32 voption
}

/// A signals program: the complete description of a State · Projection ·
/// Update · Force game. There is no 'Msg and no Cmd — handlers write roots
/// directly and defer work through the context's Intents.
type SignalsProgram<'Frame> = {
  /// Builds the graph (roots, projections) and returns the frame force.
  Init: SignalsFrameContext -> SignalsInit<'Frame>

  /// Per-frame phase: runs after the time root is written and before the
  /// frame is forced — reads projections, writes roots, posts intents.
  /// Under fixed-step it runs once per sub-step, each followed by the
  /// post drain.
  Update: SignalsContext -> GameTime -> unit

  /// Observer factories receiving the forced frame each step.
  Observers: (unit -> IObserver<struct (GameContext * 'Frame * GameTime)>) list

  /// Optional framework-managed fixed-step configuration.
  FixedStep: SignalsFixedStepConfig voption
}

[<RequireQualifiedAccess>]
module SignalsProgram =
  /// Creates a signals program from an Init and an Update phase.
  val mkProgram:
    init: (SignalsFrameContext -> SignalsInit<'Frame>) ->
    update: (SignalsContext -> GameTime -> unit) ->
      SignalsProgram<'Frame>

  /// Adds an observer notified with the forced frame after every step.
  val withObserver:
    factory: (unit -> IObserver<struct (GameContext * 'Frame * GameTime)>) ->
    program: SignalsProgram<'Frame> ->
      SignalsProgram<'Frame>

  /// Enables framework-managed fixed-step sub-stepping.
  val withFixedStep:
    cfg: SignalsFixedStepConfig ->
    program: SignalsProgram<'Frame> ->
      SignalsProgram<'Frame>

/// Runs a signals program with explicit frame stepping — the counterpart of
/// AdaptiveHeadless. Same host surface as the MVU HeadlessRunner: Step,
/// StepN, StepUntil, Dispose, ShouldQuit — but the program mutates signal
/// roots instead of returning models, and observers receive the forced
/// readonly frame.
type SignalsHeadless<'Frame> =
  new:
    program: SignalsProgram<'Frame> * ?width: int * ?height: int ->
      SignalsHeadless<'Frame>

  /// Whether an exit was requested.
  member ShouldQuit: bool
  /// The last forced frame.
  member Frame: 'Frame
  /// Total and per-step elapsed time of the last step.
  member GameTime: GameTime
  /// The runner's GameContext (services, dimensions).
  member Context: GameContext
  /// The intent queue. Hosts and external events post boundary work here
  /// (it runs at the start of the next step, before Update).
  member Intents: IntentQueue

  /// Registers an observer notified with (context, forced frame, game
  /// time) after every step. Safe before or after the first step.
  member Observe:
    onNext: (struct (GameContext * 'Frame * GameTime) -> unit) -> unit

  /// Advances the simulation by one frame of the given elapsed time.
  member Step: elapsed: TimeSpan -> unit
  /// Advances the simulation by count frames.
  member StepN: count: int * elapsed: TimeSpan -> unit

  /// Advances until the predicate on the forced frame returns true (or
  /// maxFrames steps). Returns true when the predicate was met.
  member StepUntil:
    predicate: ('Frame -> bool) *
    elapsed: TimeSpan *
    [<OptionalArgument; Struct>] maxFrames: int voption ->
      bool

  /// Releases disposables registered by the program's Init.
  member Dispose: unit -> unit
  interface IDisposable
