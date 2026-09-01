namespace Mibo.Signals

open System
open Mibo.Elmish

// ─────────────────────────────────────────────────────────────────────────────
// The signals headless runner — the web-side counterpart of Mibo.Adaptive's
// AdaptiveProgram + AdaptiveHeadless (the State · Projection · Update · Force
// model Defli and Kimo build on).
//
// The runner owns the frame boundary. Each Step:
//   (1) drains the boundary lane — externally posted work (Dispatch, host
//       events) lands before the step reads state,
//   (2) writes the current game time into the time root (once per fixed
//       sub-step when FixedStep is set),
//   (3) runs the program's Update phase — reads projections, writes roots,
//   (4) drains the intent queue until empty — posted work runs in post
//       order; work posted during the drain runs in the same drain,
//   (5) forces the frame builder — every projection read by the force
//       recomputes at most once if a dependency moved, not at all otherwise,
//   (6) notifies the observers with the forced frame. Draw code reads the
//       returned frame: plain data until the next step writes again.
//
// What the .NET version needed that the web does not: threads, semaphores,
// concurrent queues and cross-thread post pumping. The graph is confined to
// the one JS thread; "freeze a frame and publish it" is finishing your
// writes and handing out the snapshot.
// ─────────────────────────────────────────────────────────────────────────────

/// Deferred `unit -> unit` work — the counterpart of the .NET IntentQueue.
/// Two lanes: boundary work runs at the start of the next Step (external
/// events, next-frame work); after-update work runs right after each
/// Update, before the frame is forced. Work posted during a drain runs in
/// the same drain. `PostTask` covers the .NET postTask/postAsync: the
/// work starts immediately (promise concurrency), and its completion
/// re-enters through the after-update lane. True parallelism stays a
/// worker concern — a CPU-heavy task still runs on this thread.
type IntentQueue() =
  let boundary = ResizeArray<unit -> unit>()
  let afterUpdate = ResizeArray<unit -> unit>()

  /// Queues work for the next step's boundary, before Update.
  member _.PostNextFrame(work: unit -> unit) : unit = boundary.Add work

  /// Queues work to run after the next Update (per sub-step), before the
  /// frame is forced.
  member _.Post(work: unit -> unit) : unit = afterUpdate.Add work

  /// Starts `work` immediately; when the promise settles, the completion
  /// runs at the next post drain (after Update, before the frame is
  /// forced) — the counterpart of the .NET postTask/postAsync, whose
  /// completion returns via post. Rejections route through `onError`;
  /// without one, the error is rethrown at the drain so it is not
  /// swallowed.
  member _.PostTask
    (
      work: unit -> Fable.Core.JS.Promise<'T>,
      onDone: 'T -> unit,
      ?onError: exn -> unit
    ) : unit =
    work()
    |> Promise.either
      (fun value -> afterUpdate.Add(fun () -> onDone value))
      (fun err ->
        afterUpdate.Add(fun () ->
          match onError with
          | Some handler -> handler err
          | None -> raise err))
    |> ignore

  member internal q.DrainBoundary() =
    let mutable i = 0

    while i < boundary.Count do
      let work = boundary[i]
      boundary[i] <- Unchecked.defaultof<unit -> unit>
      i <- i + 1
      work()

    boundary.Clear()

  member internal q.DrainAfterUpdate() =
    let mutable i = 0

    while i < afterUpdate.Count do
      let work = afterUpdate[i]
      afterUpdate[i] <- Unchecked.defaultof<unit -> unit>
      i <- i + 1
      work()

    afterUpdate.Clear()

/// The Init-phase context: the framework-owned roots plus the intent queue.
/// Work posted here runs at the startup drain, right after Init returns and
/// before the first frame is forced — the counterpart of the MVU init Cmd.
type SignalsFrameContext
  internal
  (
    ctx: GameContext,
    time: CVal<GameTime>,
    exitRequested: CVal<bool>,
    intents: IntentQueue
  ) =

  /// The framework-owned time root. The runner writes it at the start of
  /// every step (once per fixed sub-step); projections may depend on it.
  member _.Time = time

  /// Set to true to make the runner stop — the counterpart of
  /// `CVal.set true ctx.ExitRequested`.
  member _.ExitRequested = exitRequested

  /// The GameContext the runner owns: window dimensions and services.
  member _.Context = ctx

  member _.WindowWidth = ctx.WindowWidth
  member _.WindowHeight = ctx.WindowHeight

  /// Deferred work, as documented on IntentQueue.
  member _.Intents = intents

/// The Update-phase context: everything Init received. The frame builder
/// must not post: it runs after the post drain.
type SignalsContext
  internal (frameCtx: SignalsFrameContext, intents: IntentQueue) =

  member _.Time = frameCtx.Time
  member _.ExitRequested = frameCtx.ExitRequested
  member _.Context = frameCtx.Context
  member _.WindowWidth = frameCtx.WindowWidth
  member _.WindowHeight = frameCtx.WindowHeight
  member _.Intents = intents

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

module SignalsInit =

  /// Creates an init from a frame builder — no disposables.
  let ofFrameBuilder(frameBuilder: unit -> 'Frame) : SignalsInit<'Frame> = {
    FrameBuilder = frameBuilder
    Disposables = []
  }

  /// Appends disposables released when the runner is disposed.
  let withDisposables
    (disposables: IDisposable list)
    (init: SignalsInit<'Frame>)
    : SignalsInit<'Frame> =
    {
      init with
          Disposables = init.Disposables @ disposables
    }

  /// Adds a single disposable.
  let withDisposable
    (disposable: IDisposable)
    (init: SignalsInit<'Frame>)
    : SignalsInit<'Frame> =
    {
      init with
          Disposables = disposable :: init.Disposables
    }

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
  let mkProgram
    (init: SignalsFrameContext -> SignalsInit<'Frame>)
    (update: SignalsContext -> GameTime -> unit)
    : SignalsProgram<'Frame> =
    {
      Init = init
      Update = update
      Observers = []
      FixedStep = ValueNone
    }

  /// Adds an observer notified with the forced frame after every step.
  let withObserver
    (factory: unit -> IObserver<struct (GameContext * 'Frame * GameTime)>)
    (program: SignalsProgram<'Frame>)
    : SignalsProgram<'Frame> =
    {
      program with
          Observers = factory :: program.Observers
    }

  /// Enables framework-managed fixed-step sub-stepping.
  let withFixedStep
    (cfg: SignalsFixedStepConfig)
    (program: SignalsProgram<'Frame>)
    : SignalsProgram<'Frame> =
    if cfg.StepSeconds <= 0.0f then
      invalidArg (nameof cfg.StepSeconds) "StepSeconds must be > 0"

    if cfg.MaxStepsPerFrame <= 0 then
      invalidArg (nameof cfg.MaxStepsPerFrame) "MaxStepsPerFrame must be > 0"

    {
      program with
          FixedStep = ValueSome cfg
    }

/// Runs a signals program with explicit frame stepping — the counterpart of
/// AdaptiveHeadless. Same host surface as the MVU HeadlessRunner: Step,
/// StepN, StepUntil, Dispose, ShouldQuit — but the program mutates signal
/// roots instead of returning models, and observers receive the forced
/// readonly frame.
type SignalsHeadless<'Frame>
  (program: SignalsProgram<'Frame>, ?width: int, ?height: int) =

  let w = defaultArg width 800
  let h = defaultArg height 600

  let observers =
    ResizeArray<IObserver<struct (GameContext * 'Frame * GameTime)>>()

  let intents = IntentQueue()

  let mutable initialized = false
  let mutable gameContext = Unchecked.defaultof<GameContext>
  let mutable frameCtx = Unchecked.defaultof<SignalsFrameContext>
  let mutable ctx = Unchecked.defaultof<SignalsContext>
  let mutable frameBuilder: unit -> 'Frame = Unchecked.defaultof<unit -> 'Frame>
  let mutable disposables: IDisposable list = []

  let mutable gameTime = {
    TotalTime = TimeSpan.Zero
    ElapsedGameTime = TimeSpan.Zero
  }

  let mutable fixedAccSeconds = 0.0f
  let mutable frame = Unchecked.defaultof<'Frame>

  /// Creates the graph and the first frame. Runs once, on the first step.
  let ensureInitialized() =
    if not initialized then
      initialized <- true
      gameContext <- GameContext.create(w, h)

      let timeRoot = CVal.create gameTime
      let exitRoot = CVal.create false
      frameCtx <- SignalsFrameContext(gameContext, timeRoot, exitRoot, intents)
      ctx <- SignalsContext(frameCtx, intents)

      let init = program.Init frameCtx
      frameBuilder <- init.FrameBuilder
      disposables <- init.Disposables

      // Observers registered by prepending (see withObserver), so
      // reverse to notify in registration order.
      for factory in List.rev program.Observers do
        observers.Add(factory())

      // Startup drain: Init's posted work runs before the first frame.
      intents.DrainBoundary()

      frame <- frameBuilder()

  let stepCore(dtSeconds: float32) =
    let dt = TimeSpan.FromSeconds(float dtSeconds)

    gameTime <- {
      TotalTime = gameTime.TotalTime + dt
      ElapsedGameTime = dt
    }

    Signals.batch(fun () ->
      CVal.set gameTime frameCtx.Time
      program.Update ctx gameTime
      intents.DrainAfterUpdate())

  /// Whether an exit was requested.
  member _.ShouldQuit =
    ensureInitialized()
    CVal.get frameCtx.ExitRequested

  /// The last forced frame.
  member _.Frame =
    ensureInitialized()
    frame

  /// Total and per-step elapsed time of the last step.
  member _.GameTime =
    ensureInitialized()
    gameTime

  /// The runner's GameContext (services, dimensions).
  member _.Context =
    ensureInitialized()
    gameContext

  /// The intent queue. Hosts and external events post boundary work here
  /// (it runs at the start of the next step, before Update).
  member _.Intents =
    ensureInitialized()
    intents

  /// Registers an observer notified with (context, forced frame, game
  /// time) after every step. Safe before or after the first step.
  member this.Observe
    (onNext: struct (GameContext * 'Frame * GameTime) -> unit)
    : unit =
    ensureInitialized()
    observers.Add(HeadlessProgram.observe onNext)

  /// Advances the simulation by one frame of the given elapsed time.
  member _.Step(elapsed: TimeSpan) : unit =
    ensureInitialized()

    if CVal.get frameCtx.ExitRequested then
      ()
    else
      // Clamp negatives; go through milliseconds so the clamp stays a
      // float operation (a TimeSpan comparison here compiles to an int
      // truncation under Fable, which would eat sub-millisecond parts).
      let ms = elapsed.TotalMilliseconds
      let ms = if ms < 0.0 then 0.0 else ms
      let elapsed = TimeSpan.FromMilliseconds ms

      // Boundary: externally posted work (input, host events) lands
      // before this step reads state.
      intents.DrainBoundary()

      match program.FixedStep with
      | ValueNone -> stepCore(float32 elapsed.TotalSeconds)
      | ValueSome cfg ->
        let maxFrame =
          match cfg.MaxFrameSeconds with
          | ValueSome max -> max
          | ValueNone -> 0.25f

        let clamped = float32 elapsed.TotalSeconds |> min maxFrame
        fixedAccSeconds <- fixedAccSeconds + clamped
        let mutable steps = 0

        Signals.batch(fun () ->
          while fixedAccSeconds >= cfg.StepSeconds
                && steps < cfg.MaxStepsPerFrame do
            fixedAccSeconds <- fixedAccSeconds - cfg.StepSeconds
            stepCore cfg.StepSeconds
            steps <- steps + 1)

        if steps = cfg.MaxStepsPerFrame then
          // Cap hit: drop the backlog instead of spiraling.
          fixedAccSeconds <- 0.0f

      frame <- frameBuilder()

      for i = 0 to observers.Count - 1 do
        observers[i].OnNext(struct (gameContext, frame, gameTime))

  /// Advances the simulation by count frames.
  member this.StepN(count: int, elapsed: TimeSpan) : unit =
    for _ = 1 to count do
      this.Step(elapsed)

  /// Advances until the predicate on the forced frame returns true (or
  /// maxFrames steps). Returns true when the predicate was met.
  member this.StepUntil
    (predicate: 'Frame -> bool, elapsed: TimeSpan, [<Struct>] ?maxFrames: int)
    : bool =
    let max = defaultValueArg maxFrames 10000
    let mutable steps = 0

    let mutable met = predicate this.Frame || this.ShouldQuit

    while steps < max && not met do
      this.Step(elapsed)
      steps <- steps + 1
      met <- predicate this.Frame || this.ShouldQuit

    met

  /// Releases disposables registered by the program's Init.
  member _.Dispose() : unit =
    if initialized then
      for d in disposables do
        d.Dispose()

      disposables <- []

  interface IDisposable with
    member this.Dispose() = this.Dispose()
