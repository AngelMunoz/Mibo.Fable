namespace Mibo.Elmish

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Mibo.Diagnostics

/// <summary>
/// Internal message queue supporting optional frame-bounded dispatch.
/// </summary>
/// <remarks>
/// Extracted from the runtime so both the windowed and headless hosts share it.
/// Kept internal — hosts interact with it through <see cref="T:Mibo.Elmish.ElmishLoop`2"/>.
/// </remarks>
type internal DispatchQueue<'Msg> =
  internal new: mode: DispatchMode -> DispatchQueue<'Msg>
  member internal Mode: DispatchMode
  member internal Dispatch: msg: 'Msg -> unit
  member internal StartBatch: unit -> unit
  member internal EndBatch: unit -> unit
  member internal TryDequeue: unit -> 'Msg voption

/// <summary>
/// The six fields that define message-processing behavior, shared by
/// <see cref="T:Mibo.Elmish.Program`2"/> and <see cref="T:Mibo.Elmish.HeadlessProgram`2"/>.
/// </summary>
/// <remarks>
/// Each host projects its program type to a <c>LoopCore</c> via a trivial accessor,
/// so neither <c>Program</c> nor <c>HeadlessProgram</c> changes shape.
/// </remarks>
[<NoComparison>]
[<Struct>]
type LoopCore<'Model, 'Msg> = {
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  /// The update the pump invokes. Always context-taking: the program
  /// projections adapt a context-free <c>Program.Update</c> to this shape.
  Update: GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  Tick: (GameTime -> 'Msg) voption
  FixedStep: FixedStepConfig<'Msg> voption
  DispatchMode: DispatchMode
}

/// <summary>
/// The shared message-processing loop used by every Mibo host
/// (<c>RaylibGame</c>, <c>HeadlessRunner</c>, future backends).
/// </summary>
/// <remarks>
/// Owns all mutable loop state: the dispatch queue, current model, active
/// subscriptions, deferred-effect buffers, and the fixed-step accumulator.
/// Hosts call <see cref="M:Mibo.Elmish.ElmishLoop`2.Init"/> once after registering
/// backend services, then <see cref="M:Mibo.Elmish.ElmishLoop`2.TickFrame"/> each frame.
/// </remarks>
type ElmishLoop<'Model, 'Msg> =
  internal new: core: LoopCore<'Model, 'Msg> -> ElmishLoop<'Model, 'Msg>
  /// <summary>Whether the loop has received a <c>Cmd.Quit</c> signal.</summary>
  member ShouldQuit: bool
  /// <summary>The current model state.</summary>
  member Model: 'Model
  /// <summary>The <see cref="T:Mibo.Elmish.GameContext"/> passed to <see cref="M:Mibo.Elmish.ElmishLoop`2.Init"/>, if initialized.</summary>
  member Context: GameContext voption
  /// <summary>The registered active subscriptions (for host-side disposal).</summary>
  member internal ActiveSubs: Dictionary<SubId, IDisposable>
  /// <summary>Dispatch a message into the loop's queue.</summary>
  member Dispatch: msg: 'Msg -> unit
  /// <summary>
  /// Initialize the loop: store the context, call the program's <c>Init</c>,
  /// execute startup commands, and start initial subscriptions.
  /// </summary>
  /// <remarks>Call exactly once, after the host has registered backend services.</remarks>
  member Init: ctx: GameContext -> unit
  /// <summary>
  /// Advance the simulation by one frame: drain deferred effects, run fixed-step,
  /// dispatch the tick message, process all queued messages, and update
  /// subscriptions if any messages were processed this frame.
  /// </summary>
  /// <remarks>
  /// Subscription re-evaluation is keyed on "messages were processed" rather than
  /// "the model is structurally different", because Mibo permits in-place mutable
  /// models (e.g. a class whose fields are mutated by each system) whose
  /// <c>Update</c> returns the same reference every frame. Reference or structural
  /// equality would never detect a change for those models.
  /// </remarks>
  /// <param name="elapsed">Frame delta (e.g. <c>TimeSpan.FromMilliseconds(16)</c> for 60fps).</param>
  /// <param name="gameTime">The current game time, supplied by the host.</param>
  /// <returns><c>true</c> if any messages were processed this frame; <c>false</c> otherwise.</returns>
  member TickFrame: elapsed: TimeSpan * gameTime: GameTime -> bool
  /// <summary>
  /// Dispose all active subscriptions. Hosts should call this on shutdown.
  /// </summary>
  member DisposeSubs: unit -> unit
