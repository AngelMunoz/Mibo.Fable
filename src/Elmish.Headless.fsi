namespace Mibo.Elmish

open System
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
