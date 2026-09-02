namespace Mibo.Fable.Adaptive

open System
open Mibo.Elmish

// ─────────────────────────────────────────────────────────────────────────────
// The adaptive headless runner — the web-side counterpart of Mibo.Core's
// AdaptiveProgram + AdaptiveHeadless (the State · Projection · Update · Force
// model).
//
// The runner owns the frame boundary. Each Step:
//   (1) applies pending posts and drains the boundary lane — externally
//       posted work (host events) lands before the step reads state,
//   (2) writes the current game time into the time root (once per fixed
//       sub-step when FixedStep is set),
//   (3) runs the program's Update phase — reads projections, writes roots,
//   (4) drains the intent queue until empty — posted work runs in post
//       order; work posted during the drain runs in the same drain,
//   (5) forces the frame builder — every projection read by the force
//       recomputes at most once if a dependency moved, not at all otherwise,
//   (6) notifies the observers with the forced frame.
//
// What the .NET version needed that the web does not: threads, semaphores,
// concurrent queues and the dedicated game thread of RunAsync. The graph is
// confined to the one JS thread; posts ride the graph post queue and drain
// automatically at the next graph operation.
// ─────────────────────────────────────────────────────────────────────────────

type IntentQueue() =
  let boundary = ResizeArray<unit -> unit>()
  let afterUpdate = ResizeArray<unit -> unit>()

  member _.PostNextFrame(work: unit -> unit) : unit = boundary.Add work

  member _.Post(work: unit -> unit) : unit = afterUpdate.Add work

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

type AdaptiveFrameContext
  internal
  (
    ctx: GameContext,
    time: cval<GameTime>,
    exitRequested: cval<bool>,
    intents: IntentQueue
  ) =

  member _.Time = time

  member _.ExitRequested = exitRequested

  member _.Context = ctx

  member _.WindowWidth = ctx.WindowWidth
  member _.WindowHeight = ctx.WindowHeight

  member _.Intents = intents

type AdaptiveContext
  internal (frameCtx: AdaptiveFrameContext, intents: IntentQueue) =
  member _.Time = frameCtx.Time
  member _.ExitRequested = frameCtx.ExitRequested
  member _.Context = frameCtx.Context
  member _.WindowWidth = frameCtx.WindowWidth
  member _.WindowHeight = frameCtx.WindowHeight
  member _.Intents = intents

type AdaptiveInit<'Frame> = {
  FrameBuilder: unit -> 'Frame

  Disposables: IDisposable list
}

type AdaptiveFixedStepConfig = {
  StepSeconds: float32

  MaxStepsPerFrame: int

  MaxFrameSeconds: float32 voption
}

type AdaptiveProgram<'Frame> = {
  Init: AdaptiveFrameContext -> AdaptiveInit<'Frame>

  Update: AdaptiveContext -> GameTime -> unit

  Observers: (unit -> IObserver<struct (GameContext * 'Frame * GameTime)>) list

  FixedStep: AdaptiveFixedStepConfig voption
}

type AdaptiveHeadless<'Frame>
  (program: AdaptiveProgram<'Frame>, ?width: int, ?height: int) =

  let w = defaultArg width 800
  let h = defaultArg height 600

  let observers =
    ResizeArray<IObserver<struct (GameContext * 'Frame * GameTime)>>()

  let intents = IntentQueue()

  let mutable initialized = false
  let mutable gameContext = Unchecked.defaultof<GameContext>
  let mutable frameCtx = Unchecked.defaultof<AdaptiveFrameContext>
  let mutable ctx = Unchecked.defaultof<AdaptiveContext>
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
      frameCtx <- AdaptiveFrameContext(gameContext, timeRoot, exitRoot, intents)
      ctx <- AdaptiveContext(frameCtx, intents)

      let init = program.Init frameCtx
      frameBuilder <- init.FrameBuilder
      disposables <- init.Disposables

      // Observers registered by prepending (see withObserver), so
      // reverse to notify in registration order.
      for factory in List.rev program.Observers do
        observers.Add(factory())

      // Startup drain: Init's posted work runs before the first frame.
      intents.DrainBoundary()

      // Force the first frame so Frame is never default after initialization.
      frame <- frameBuilder()

  let stepCore(dtSeconds: float32) =
    let dt = TimeSpan.FromSeconds(float dtSeconds)

    gameTime <- {
      TotalTime = gameTime.TotalTime + dt
      ElapsedGameTime = dt
    }

    // The time root is the framework's write into the graph; the update
    // phase reads projections and writes roots directly (the pull-lazy
    // graph recomputes on read — no batching machinery).
    CVal.set gameTime ctx.Time
    program.Update ctx gameTime
    intents.DrainAfterUpdate()

  member _.ShouldQuit =
    ensureInitialized()
    AVal.getValue frameCtx.ExitRequested

  member _.Frame =
    ensureInitialized()
    frame

  member _.GameTime =
    ensureInitialized()
    gameTime

  member _.Context =
    ensureInitialized()
    gameContext

  member _.Intents =
    ensureInitialized()
    intents

  member this.Observe
    (onNext: struct (GameContext * 'Frame * GameTime) -> unit)
    : unit =
    ensureInitialized()
    observers.Add(HeadlessProgram.observe onNext)

  member _.Step(elapsed: TimeSpan) : unit =
    ensureInitialized()

    if AVal.getValue frameCtx.ExitRequested then
      ()
    else
      // Clamp negatives; go through milliseconds so the clamp stays a
      // float operation (a TimeSpan comparison here compiles to an int
      // truncation under Fable, which would eat sub-millisecond parts).
      let ms = elapsed.TotalMilliseconds
      let ms = if ms < 0.0 then 0.0 else ms
      let elapsed = TimeSpan.FromMilliseconds ms

      // Frame boundary: pending posted values apply first (the graph
      // drains them automatically at the next operation; the explicit
      // pump mirrors the .NET runner), then externally posted work
      // (input, host events) lands before this step reads state.
      Posting.pump()
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

        while fixedAccSeconds >= cfg.StepSeconds && steps < cfg.MaxStepsPerFrame do
          fixedAccSeconds <- fixedAccSeconds - cfg.StepSeconds
          stepCore cfg.StepSeconds
          steps <- steps + 1

        if steps = cfg.MaxStepsPerFrame then
          // Cap hit: drop the backlog instead of spiraling.
          fixedAccSeconds <- 0.0f

      // Force phase: recompute the frame's projections exactly once (not
      // at all if none of their dependencies moved).
      frame <- frameBuilder()

      for i = 0 to observers.Count - 1 do
        observers[i].OnNext(struct (gameContext, frame, gameTime))

  member this.StepN(count: int, elapsed: TimeSpan) : unit =
    for _ = 1 to count do
      this.Step(elapsed)

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

  member _.Dispose() : unit =
    if initialized then
      for d in disposables do
        d.Dispose()

      disposables <- []

  interface IDisposable with
    member this.Dispose() = this.Dispose()
