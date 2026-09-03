/// <summary>Builders for <see cref="T:Mibo.Elmish.HeadlessProgram`2"/>.</summary>
module Mibo.Elmish.HeadlessProgram

open System
open Mibo.Diagnostics

/// <summary>
/// Creates a <c>System.IObserver</c> from an <c>onNext</c> callback, hiding
/// the <c>OnError</c> and <c>OnCompleted</c> boilerplate.
/// </summary>
val inline observe: onNext: ('T -> unit) -> IObserver<'T>

/// <summary>Projects a <see cref="T:Mibo.Elmish.HeadlessProgram`2"/> to a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
val toLoopCore: program: HeadlessProgram<'Model, 'Msg> -> LoopCore<'Model, 'Msg>

/// <summary>
/// Creates a new headless program with the given init and update functions.
/// </summary>
val mkHeadless:
  init: (GameContext -> struct ('Model * Cmd<'Msg>)) ->
  update: ('Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>
/// Creates a new headless program whose update function also receives the
/// GameContext. Mirrors <see cref="M:Mibo.Elmish.Program.mkProgramCtx"/>.
/// </summary>
val mkHeadlessCtx:
  init: (GameContext -> struct ('Model * Cmd<'Msg>)) ->
  update: (GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>Adds a subscription function to the program.</summary>
val withSubscribe:
  subscribe: (GameContext -> 'Model -> Sub<'Msg>) ->
  program: HeadlessProgram<'Model, 'Msg> ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>Adds a per-frame tick message generated from the current <see cref="T:Mibo.Elmish.GameTime"/>.</summary>
/// <param name="map">Function that converts the current game time into a message dispatched each frame.</param>
val withTick:
  map: (GameTime -> 'Msg) ->
  program: HeadlessProgram<'Model, 'Msg> ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>Enables a framework-managed fixed timestep that dispatches a message at a constant rate, independent of variable frame timing.</summary>
/// <param name="cfg">Fixed step configuration (step size, max steps per frame, max frame budget, message mapper).</param>
/// <exception cref="T:System.ArgumentException">Thrown when <c>StepSeconds</c> ≤ 0 or <c>MaxStepsPerFrame</c> ≤ 0.</exception>
val withFixedStep:
  cfg: FixedStepConfig<'Msg> ->
  program: HeadlessProgram<'Model, 'Msg> ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>Sets the dispatch mode controlling when messages become eligible for processing.</summary>
/// <param name="mode"><c>Immediate</c> processes in-frame; <c>FrameBounded</c> defers to the next step.</param>
val withDispatchMode:
  mode: DispatchMode ->
  program: HeadlessProgram<'Model, 'Msg> ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>Appends an observer factory that receives a model snapshot each frame.</summary>
val withObserver:
  factory: (unit -> IObserver<struct (GameContext * 'Model * GameTime)>) ->
  program: HeadlessProgram<'Model, 'Msg> ->
    HeadlessProgram<'Model, 'Msg>

/// <summary>
/// Supplies the frame profiler the runner registers and measures with.
/// </summary>
/// <remarks>Without it the runner measures nothing.</remarks>
val withProfiler:
  profiler: FrameProfiler ->
  program: HeadlessProgram<'Model, 'Msg> ->
    HeadlessProgram<'Model, 'Msg>
