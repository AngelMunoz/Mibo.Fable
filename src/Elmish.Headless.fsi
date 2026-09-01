namespace Mibo.Elmish

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Mibo.Diagnostics

/// <summary>
/// A program configuration for running the Elmish update loop without graphics.
/// </summary>
/// <remarks>
/// HeadlessProgram shares the core Elmish architecture (Init, Update, Subscribe, Tick, FixedStep)
/// with the full Program type, but excludes renderers and window configuration.
/// Use <see cref="M:Mibo.Elmish.HeadlessProgram.mkHeadless"/> to create one.
/// </remarks>
type HeadlessProgram<'Model, 'Msg> = {
  /// <summary>Creates initial model and commands when the headless runner starts.</summary>
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  /// <summary>Handles messages and returns updated model and commands.</summary>
  Update: 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  /// <summary>
  /// Optional context-aware update. When set, the runner calls this instead of
  /// <see cref="F:Mibo.Elmish.HeadlessProgram`2.Update"/>, passing the
  /// <see cref="T:Mibo.Elmish.GameContext"/> the runner owns.
  /// </summary>
  /// <remarks>Set via <see cref="M:Mibo.Elmish.HeadlessProgram.mkHeadlessCtx"/>.</remarks>
  UpdateCtx:
    (GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) voption
  /// <summary>Returns subscriptions based on current model state.</summary>
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  /// <summary>Optional function to generate a message each frame.</summary>
  Tick: (GameTime -> 'Msg) voption
  /// <summary>Optional framework-managed fixed timestep configuration.</summary>
  FixedStep: FixedStepConfig<'Msg> voption
  /// <summary>Controls when dispatched messages become eligible for processing.</summary>
  DispatchMode: DispatchMode
  /// <summary>Observer factories for receiving model snapshots each frame.</summary>
  Observers: (unit -> IObserver<struct (GameContext * 'Model * GameTime)>) list
  /// <summary>Optional frame profiler. Set via <see cref="M:Mibo.Elmish.HeadlessProgram.withProfiler"/>.</summary>
  /// <remarks>When unset, the runner measures nothing.</remarks>
  Profiler: FrameProfiler voption
}

/// <summary>Extension functions for projecting a <see cref="T:Mibo.Elmish.HeadlessProgram`2"/> onto a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
module HeadlessProgram =
  /// <summary>
  /// Creates a <c>System.IObserver</c> from an <c>onNext</c> callback, hiding
  /// the <c>OnError</c> and <c>OnCompleted</c> boilerplate.
  /// </summary>
  val inline observe: onNext: ('T -> unit) -> IObserver<'T>

  /// <summary>Projects a <see cref="T:Mibo.Elmish.HeadlessProgram`2"/> to a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
  val toLoopCore:
    program: HeadlessProgram<'Model, 'Msg> -> LoopCore<'Model, 'Msg>

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

/// <summary>
/// Controls execution of a headless Elmish program with explicit frame stepping.
/// </summary>
/// <remarks>
/// The runner delegates message processing to a shared <see cref="T:Mibo.Elmish.ElmishLoop`2"/>
/// and adds observer notification + virtual time management on top. Call <see cref="Step"/>
/// or <see cref="StepN"/> to advance the simulation.
/// </remarks>
type HeadlessRunner<'Model, 'Msg> =
  new:
    program: HeadlessProgram<'Model, 'Msg> * ?width: int * ?height: int ->
      HeadlessRunner<'Model, 'Msg>

  /// <summary>Whether the runner has received a Quit signal.</summary>
  member ShouldQuit: bool
  /// <summary>The current model state.</summary>
  member Model: 'Model
  /// <summary>Total elapsed virtual time.</summary>
  member GameTime: GameTime
  /// <summary>Dispatch a message to the runner.</summary>
  member Dispatch: msg: 'Msg -> unit
  /// <summary>Dispatch multiple messages at once.</summary>
  member DispatchMany: msgs: 'Msg seq -> unit
  /// <summary>Advance the simulation by one frame with the given delta time.</summary>
  /// <param name="elapsed">Frame delta (e.g. TimeSpan.FromMilliseconds(16) for 60fps). Negative values are clamped to zero.</param>
  /// <remarks>
  /// This mutates the runner's internal state (model, game time, subscriptions, deferred commands).
  /// Do not mix <c>Step</c>/<c>StepN</c>/<c>StepUntil</c> with <c>Run</c>/<c>RunAsync</c> on the same runner
  /// — they all advance the simulation and using them together will produce simulation corruption.
  /// </remarks>
  member Step: elapsed: TimeSpan -> unit
  /// <summary>Advance the simulation by N frames.</summary>
  /// <param name="count">Number of frames to run.</param>
  /// <param name="elapsed">Frame delta per step.</param>
  /// <remarks>
  /// This mutates the runner's internal state. Do not mix with <c>Run</c>/<c>RunAsync</c>
  /// on the same runner — they all advance the simulation and using them together
  /// will produce simulation corruption.
  /// </remarks>
  member StepN: count: int * elapsed: TimeSpan -> unit

  /// <summary>Advance until a predicate on the model returns true.</summary>
  /// <param name="predicate">Condition to check after each frame.</param>
  /// <param name="elapsed">Frame delta per step.</param>
  /// <param name="maxFrames">Safety limit to prevent infinite loops.</param>
  /// <returns>True if predicate was met, false if maxFrames was reached.</returns>
  /// <remarks>
  /// This mutates the runner's internal state. Do not mix with <c>Run</c>/<c>RunAsync</c>
  /// on the same runner — they all advance the simulation and using them together
  /// will produce simulation corruption.
  /// </remarks>
  member StepUntil:
    predicate: ('Model -> bool) *
    elapsed: TimeSpan *
    [<OptionalArgument; Struct>] maxFrames: int voption ->
      bool

  /// <summary>Dispose active subscriptions, observers, and clean up resources.</summary>
  member Dispose: unit -> unit
  interface IDisposable
