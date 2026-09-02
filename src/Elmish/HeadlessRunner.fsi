namespace Mibo.Elmish

open System
open Mibo.Diagnostics

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
