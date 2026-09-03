module Mibo.Elmish.HeadlessProgram

open System
open Mibo.Diagnostics

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
