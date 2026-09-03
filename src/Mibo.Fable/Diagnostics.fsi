namespace Mibo.Diagnostics

open System
open System.Diagnostics
open Mibo.Elmish

/// <summary>
/// Frame measurements collected over a time window.
/// </summary>
/// <remarks>
/// The profiler refreshes these values once per window. All rates are windowed
/// counts. All costs are means over the window, in milliseconds.
/// </remarks>
[<Struct>]
type FrameStats = {
  /// <summary>Host frames per second. A host frame is one update step of the host loop.</summary>
  FramesPerSecond: float32

  /// <summary>Draws per second. This is the frame rate the player sees. <c>ValueNone</c> on headless runners.</summary>
  DrawsPerSecond: float32 voption

  /// <summary>Simulation steps per second. With a fixed step this counts every step, not every frame.</summary>
  SimStepsPerSecond: float32

  /// <summary>Mean host frame interval in milliseconds.</summary>
  FrameMs: float32

  /// <summary>Worst host frame interval in the window, in milliseconds.</summary>
  WorstFrameMs: float32

  /// <summary>Mean cost of the update phase, in milliseconds.</summary>
  UpdateMs: float32

  /// <summary>Mean cost of the draw phase, in milliseconds. <c>ValueNone</c> on headless runners.</summary>
  DrawMs: float32 voption

  /// <summary>Bytes allocated on the frame thread during the window.</summary>
  AllocatedBytes: int64

  /// <summary>Generation 0 collections during the window.</summary>
  Gen0Collections: int

  /// <summary>Generation 1 collections during the window.</summary>
  Gen1Collections: int

  /// <summary>Generation 2 collections during the window.</summary>
  Gen2Collections: int

  /// <summary>Frames that ran behind in the window. Fixed step drops count here. On MonoGame, the fixed step catch up flag counts here.</summary>
  SlowFrames: int

  /// <summary>Total host frames since the profiler was created.</summary>
  TotalFrames: int64

  /// <summary>Draw calls of the last frame. <c>ValueNone</c> where the backend reports no such count.</summary>
  GpuDrawCalls: int64 voption

  /// <summary>Primitives of the last frame. <c>ValueNone</c> where the backend reports no such count.</summary>
  GpuPrimitives: int64 voption

  /// <summary>Texture binds of the last frame. <c>ValueNone</c> where the backend reports no such count.</summary>
  GpuTextureBinds: int64 voption
}

/// <summary>
/// Collects frame measurements for the running game and serves screenshot requests.
/// </summary>
/// <remarks>
/// Build one, pass it to the program with <c>withProfiler</c>, and the host
/// registers it in the <see cref="T:Mibo.Elmish.GameContext"/> and measures
/// every frame. When no profiler is supplied, nothing runs and nothing is
/// registered. Read it with
/// <see cref="M:Mibo.Diagnostics.Diagnostics.tryGetProfiler"/> from a renderer,
/// a subscription, or a context taking update.
/// <para>
/// All members are for the frame thread only. The stamp methods allocate
/// nothing and box nothing, so a host can call them every frame.
/// </para>
/// </remarks>
type FrameProfiler =
  new: window: TimeSpan * canScreenshot: bool -> FrameProfiler
  /// <summary>Creates a profiler that cannot take screenshots. Headless hosts use this form.</summary>
  static member create: window: TimeSpan -> FrameProfiler
  /// <summary>Whether measurement runs. On by default.</summary>
  /// <remarks>
  /// Turn it off and on at any time. While it is off every stamp and every
  /// request does nothing. Turning it back on starts a fresh window, so the
  /// time spent off never shows up as a frame spike.
  /// </remarks>
  member Enabled: bool with get, set
  /// <summary>The default measurement window of half a second.</summary>
  static member DefaultWindow: TimeSpan
  /// <summary>
  /// Starts a host frame. Hosts call this first in the frame, before input
  /// polling and the update phase.
  /// </summary>
  /// <remarks>
  /// When the window has elapsed, this call first freezes the finished window
  /// into <see cref="P:Mibo.Diagnostics.FrameProfiler.Snapshot"/> and then
  /// starts the next window.
  /// </remarks>
  member BeginFrame: unit -> unit
  /// <summary>
  /// Ends the update phase. Hosts call this after the update work of the frame
  /// is done, before any drawing.
  /// </summary>
  member EndUpdate: unit -> unit
  /// <summary>
  /// Starts the draw phase. Hosts call this right before they draw. Headless
  /// runners never call it, so draw fields stay <c>ValueNone</c> there.
  /// </summary>
  member BeginDraw: unit -> unit
  /// <summary>
  /// Ends the draw phase. Hosts call this after the last draw call of the
  /// frame, before they present.
  /// </summary>
  member EndDraw: unit -> unit
  /// <summary>
  /// Counts simulation steps that ran in this frame. The shared loop calls
  /// this once per frame. <paramref name="dropped"/> marks that the fixed step
  /// hit its step cap and threw time away.
  /// </summary>
  member AddSimSteps: steps: int * dropped: bool -> unit
  /// <summary>
  /// Counts one frame that ran behind. MonoGame hosts call this when the fixed
  /// step catch up flag is set.
  /// </summary>
  member NoteSlowFrame: unit -> unit

  /// <summary>
  /// Publishes the graphics counters of the frame that just drew. MonoGame
  /// hosts call this at the end of draw. Other backends never call it, so the
  /// fields stay <c>ValueNone</c>.
  /// </summary>
  member PublishGpuMetrics:
    drawCalls: int64 * primitives: int64 * textureBinds: int64 -> unit

  /// <summary>The measurements of the last completed window.</summary>
  /// <remarks>Zeroed out until the first window completes.</remarks>
  member Snapshot: FrameStats
  /// <summary>Whether this runtime can capture the screen.</summary>
  member CanScreenshot: bool
  /// <summary>
  /// Asks the host to save a screenshot at the given path at the end of the
  /// current frame.
  /// </summary>
  /// <remarks>
  /// The request does nothing when <see cref="P:Mibo.Diagnostics.FrameProfiler.CanScreenshot"/>
  /// is false, which is the case on headless runners. Check that property
  /// beforehand when the difference matters.
  /// <para>
  /// The file is written when the frame finishes drawing. The capture reads
  /// the whole screen and encodes a PNG, so expect one slow frame per request.
  /// </para>
  /// </remarks>
  member RequestScreenshot: path: string -> unit
  /// <summary>
  /// Takes the pending screenshot request, if any. Hosts call this at the end
  /// of draw, then write the file themselves.
  /// </summary>
  member DrainScreenshot: unit -> string voption
