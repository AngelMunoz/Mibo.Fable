namespace Mibo.Elmish

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Mibo.Diagnostics

type HeadlessProgram<'Model, 'Msg> = {
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  Update: 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  UpdateCtx:
    (GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) voption
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  Tick: (GameTime -> 'Msg) voption
  FixedStep: FixedStepConfig<'Msg> voption
  DispatchMode: DispatchMode
  Observers: (unit -> IObserver<struct (GameContext * 'Model * GameTime)>) list
  Profiler: FrameProfiler voption
}

module HeadlessProgram =

  let inline observe(onNext: 'T -> unit) : IObserver<'T> =
    { new IObserver<'T> with
        member _.OnNext value = onNext value
        member _.OnError _ = ()
        member _.OnCompleted() = ()
    }

  /// <summary>Projects a <see cref="T:Mibo.Elmish.HeadlessProgram`2"/> to a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
  let toLoopCore
    (program: HeadlessProgram<'Model, 'Msg>)
    : LoopCore<'Model, 'Msg> =
    {
      Init = program.Init
      Update =
        match program.UpdateCtx with
        | ValueSome update -> update
        | ValueNone -> fun _ctx msg model -> program.Update msg model
      Subscribe = program.Subscribe
      Tick = program.Tick
      FixedStep = program.FixedStep
      DispatchMode = program.DispatchMode
    }

  let mkHeadless
    (init: GameContext -> struct ('Model * Cmd<'Msg>))
    (update: 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>))
    : HeadlessProgram<'Model, 'Msg> =
    {
      Init = init
      Update = update
      UpdateCtx = ValueNone
      Subscribe = (fun _ctx _model -> Sub.none)
      Tick = ValueNone
      FixedStep = ValueNone
      DispatchMode = DispatchMode.Immediate
      Observers = []
      Profiler = ValueNone
    }

  /// <summary>
  /// Creates a new headless program whose update function also receives the
  /// GameContext. Mirrors <see cref="M:Mibo.Elmish.Program.mkProgramCtx"/>.
  /// </summary>
  let mkHeadlessCtx
    (init: GameContext -> struct ('Model * Cmd<'Msg>))
    (update: GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>))
    : HeadlessProgram<'Model, 'Msg> =
    {
      Init = init
      // Never called: the runner prefers UpdateCtx. Raises instead of
      // silently dropping messages if an internal path regresses.
      Update =
        (fun _ _ ->
          invalidOp
            "HeadlessProgram built with mkHeadlessCtx: the runner invokes UpdateCtx.")
      UpdateCtx = ValueSome update
      Subscribe = (fun _ctx _model -> Sub.none)
      Tick = ValueNone
      FixedStep = ValueNone
      DispatchMode = DispatchMode.Immediate
      Observers = []
      Profiler = ValueNone
    }

  /// <summary>Adds a subscription function to the program.</summary>
  let withSubscribe subscribe program : HeadlessProgram<'Model, 'Msg> = {
    program with
        Subscribe = subscribe
  }

  let withTick map program : HeadlessProgram<'Model, 'Msg> = {
    program with
        Tick = ValueSome map
  }

  /// <summary>Enables a framework-managed fixed timestep that dispatches a message at a constant rate, independent of variable frame timing.</summary>
  /// <param name="cfg">Fixed step configuration (step size, max steps per frame, max frame budget, message mapper).</param>
  /// <exception cref="T:System.ArgumentException">Thrown when <c>StepSeconds</c> ≤ 0 or <c>MaxStepsPerFrame</c> ≤ 0.</exception>
  let withFixedStep cfg program : HeadlessProgram<'Model, 'Msg> =
    if cfg.StepSeconds <= 0.0f then
      invalidArg (nameof cfg.StepSeconds) "StepSeconds must be > 0"

    if cfg.MaxStepsPerFrame <= 0 then
      invalidArg (nameof cfg.MaxStepsPerFrame) "MaxStepsPerFrame must be > 0"

    {
      program with
          FixedStep = ValueSome cfg
    }

  /// <summary>Sets the dispatch mode controlling when messages become eligible for processing.</summary>
  /// <param name="mode"><c>Immediate</c> processes in-frame; <c>FrameBounded</c> defers to the next step.</param>
  let withDispatchMode mode program : HeadlessProgram<'Model, 'Msg> = {
    program with
        DispatchMode = mode
  }

  let withObserver
    (factory: unit -> IObserver<struct (GameContext * 'Model * GameTime)>)
    program
    : HeadlessProgram<'Model, 'Msg> =
    {
      program with
          Observers = factory :: program.Observers
    }

  /// <summary>
  /// Supplies the frame profiler the runner registers and measures with.
  /// </summary>
  /// <remarks>Without it the runner measures nothing.</remarks>
  let withProfiler profiler program : HeadlessProgram<'Model, 'Msg> = {
    program with
        Profiler = ValueSome profiler
  }

type HeadlessRunner<'Model, 'Msg>
  (program: HeadlessProgram<'Model, 'Msg>, ?width: int, ?height: int) =

  let loop = ElmishLoop.create(HeadlessProgram.toLoopCore program)

  // Set in the constructor do block. Resolved once so Step does no lookup.
  let mutable profilerOpt: FrameProfiler voption = ValueNone

  let observers =
    ResizeArray<IObserver<struct (GameContext * 'Model * GameTime)>>()

  let mutable gameTime = {
    TotalTime = TimeSpan.Zero
    ElapsedGameTime = TimeSpan.Zero
  }

  let w = defaultArg width 800
  let h = defaultArg height 600

  do
    let ctx = GameContext.create(w, h)

    // Registered before Init so user init code sees it. Only a profiler
    // supplied through withProfiler runs; without one nothing is measured.
    program.Profiler
    |> ValueOption.iter(fun profiler ->
      GameContext.register<FrameProfiler> profiler ctx)

    profilerOpt <- program.Profiler

    loop.Init(ctx)

    // Observers are stored by prepending (see withObserver), so reverse to
    // initialize and notify in registration order — matching Renderers,
    // Config, and ServiceRegistrations.
    for factory in List.rev program.Observers do
      observers.Add(factory())

  member _.ShouldQuit = loop.ShouldQuit

  member _.Model = loop.Model

  member _.GameTime = gameTime

  member _.Dispatch(msg: 'Msg) = loop.Dispatch(msg)

  member _.DispatchMany(msgs: 'Msg seq) =
    for msg in msgs do
      loop.Dispatch(msg)

  member _.Step(elapsed: TimeSpan) =
    if loop.ShouldQuit then
      ()
    else

      let elapsed = if elapsed < TimeSpan.Zero then TimeSpan.Zero else elapsed

      gameTime <- {
        TotalTime = gameTime.TotalTime + elapsed
        ElapsedGameTime = elapsed
      }

      match profilerOpt with
      | ValueSome profiler ->
        profiler.BeginFrame()
        loop.TickFrame(elapsed, gameTime) |> ignore
        profiler.EndUpdate()
      | ValueNone -> loop.TickFrame(elapsed, gameTime) |> ignore

      match loop.Context with
      | ValueSome ctx ->
        for i = 0 to observers.Count - 1 do
          observers[i].OnNext(ctx, loop.Model, gameTime)
      | ValueNone -> ()

  member this.StepN(count: int, elapsed: TimeSpan) =
    for _ = 1 to count do
      this.Step elapsed


  member this.StepUntil
    (predicate: 'Model -> bool, elapsed: TimeSpan, [<Struct>] ?maxFrames: int)
    =
    let max = defaultValueArg maxFrames 10000
    let mutable steps = 0
    let mutable met = predicate this.Model || this.ShouldQuit

    while steps < max && not met do
      this.Step elapsed
      steps <- steps + 1
      met <- predicate this.Model || this.ShouldQuit

    met

  member _.Dispose() =
    loop.DisposeSubs()

    for i = 0 to observers.Count - 1 do
      match observers[i] with
      | :? IDisposable as d -> d.Dispose()
      | _ -> ()

    observers.Clear()

  interface IDisposable with
    member this.Dispose() = this.Dispose()
