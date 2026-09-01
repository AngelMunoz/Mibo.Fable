namespace Mibo.Diagnostics

open System
open System.Diagnostics
open Mibo.Elmish

[<Struct>]
type FrameStats = {
  FramesPerSecond: float32

  DrawsPerSecond: float32 voption

  SimStepsPerSecond: float32

  FrameMs: float32

  WorstFrameMs: float32

  UpdateMs: float32

  DrawMs: float32 voption

  AllocatedBytes: int64

  Gen0Collections: int

  Gen1Collections: int

  Gen2Collections: int

  SlowFrames: int

  TotalFrames: int64

  GpuDrawCalls: int64 voption

  GpuPrimitives: int64 voption

  GpuTextureBinds: int64 voption
}

type FrameProfiler(window: TimeSpan, canScreenshot: bool) as this =

  // Stopwatch ticks for one window. Computed once so the per frame check is a
  // subtraction and a compare.
  let windowSwTicks =
    let ticks = window.Ticks * 10000000L / 10_000_000L
    if ticks < 1L then 1L else ticks

  let mutable windowStart = 0L
  let mutable lastFrameStamp = 0L
  let mutable updateStart = 0L
  let mutable drawStart = 0L

  let mutable windowFrames = 0
  let mutable windowSimSteps = 0
  let mutable windowDraws = 0
  let mutable windowWorstMs = 0f
  let mutable windowUpdateMsSum = 0f
  let mutable windowDrawMsSum = 0f
  let mutable windowSlowFrames = 0
  let mutable windowAllocStart = 0L
  let mutable windowGen0Start = 0
  let mutable windowGen1Start = 0
  let mutable windowGen2Start = 0

  let mutable totalFrames = 0L
  let mutable gpuDrawCalls = 0L
  let mutable gpuPrimitives = 0L
  let mutable gpuTextureBinds = 0L
  let mutable gpuPublished = false
  let mutable pendingScreenshot: string voption = ValueNone
  let mutable snapshot = Unchecked.defaultof<FrameStats>
  let mutable enabled = true

  static member create(window: TimeSpan) = FrameProfiler(window, false)

  member _.Enabled
    with get () = enabled
    and set value =
      if value && not enabled then
        windowStart <- 0L
        lastFrameStamp <- 0L

      enabled <- value

  static member DefaultWindow = TimeSpan.FromSeconds 0.5

  /// <summary>
  /// Freezes the window that just ended and starts a new one.
  /// </summary>
  /// <param name="now">The current stopwatch stamp.</param>
  member private _.Publish(now: int64) =
    if windowFrames > 0 then
      let windowSec = float32(float(now - windowStart) / 10000000.0)

      let drawMs =
        if windowDraws > 0 then
          ValueSome(windowDrawMsSum / float32 windowDraws)
        else
          ValueNone

      let drawsPerSecond =
        if windowDraws > 0 then
          ValueSome(float32 windowDraws / windowSec)
        else
          ValueNone

      snapshot <- {
        FramesPerSecond = float32 windowFrames / windowSec
        DrawsPerSecond = drawsPerSecond
        SimStepsPerSecond = float32 windowSimSteps / windowSec
        FrameMs = windowSec * 1000f / float32 windowFrames
        WorstFrameMs = windowWorstMs
        UpdateMs = windowUpdateMsSum / float32 windowFrames
        DrawMs = drawMs
        AllocatedBytes = 0L - windowAllocStart
        Gen0Collections = 0 - windowGen0Start
        Gen1Collections = 0 - windowGen1Start
        Gen2Collections = 0 - windowGen2Start
        SlowFrames = windowSlowFrames
        TotalFrames = totalFrames
        GpuDrawCalls =
          if gpuPublished then ValueSome gpuDrawCalls else ValueNone
        GpuPrimitives =
          if gpuPublished then ValueSome gpuPrimitives else ValueNone
        GpuTextureBinds =
          if gpuPublished then
            ValueSome gpuTextureBinds
          else
            ValueNone
      }

    windowStart <- now
    windowFrames <- 0
    windowSimSteps <- 0
    windowDraws <- 0
    windowWorstMs <- 0f
    windowUpdateMsSum <- 0f
    windowDrawMsSum <- 0f
    windowSlowFrames <- 0
    windowAllocStart <- 0L
    windowGen0Start <- 0
    windowGen1Start <- 0
    windowGen2Start <- 0

  member _.BeginFrame() =
    if this.Enabled then
      let now = System.DateTime.UtcNow.Ticks

      if windowStart = 0L then
        this.SeedWindow(now)
      elif now - windowStart >= windowSwTicks then
        this.Publish(now)

      if lastFrameStamp <> 0L then
        let ms = float32(float(now - lastFrameStamp) / 10000.0)

        if ms > windowWorstMs then
          windowWorstMs <- ms

      lastFrameStamp <- now
      updateStart <- now
      windowFrames <- windowFrames + 1
      totalFrames <- totalFrames + 1L

  /// <summary>
  /// Records the first frame stamp and the window counters.
  /// </summary>
  member private _.SeedWindow(now: int64) =
    windowStart <- now
    windowAllocStart <- 0L
    windowGen0Start <- 0
    windowGen1Start <- 0
    windowGen2Start <- 0

  member _.EndUpdate() =
    if this.Enabled then
      let ms =
        float32(float(System.DateTime.UtcNow.Ticks - updateStart) / 10000.0)

      windowUpdateMsSum <- windowUpdateMsSum + ms

  member _.BeginDraw() =
    if this.Enabled then
      drawStart <- System.DateTime.UtcNow.Ticks

  member _.EndDraw() =
    if this.Enabled then
      let ms =
        float32(float(System.DateTime.UtcNow.Ticks - drawStart) / 10000.0)

      windowDrawMsSum <- windowDrawMsSum + ms
      windowDraws <- windowDraws + 1

  member _.AddSimSteps(steps: int, dropped: bool) =
    if this.Enabled then
      windowSimSteps <- windowSimSteps + steps

      if dropped then
        windowSlowFrames <- windowSlowFrames + 1

  member _.NoteSlowFrame() =
    if this.Enabled then
      windowSlowFrames <- windowSlowFrames + 1

  member _.PublishGpuMetrics
    (drawCalls: int64, primitives: int64, textureBinds: int64)
    =
    if this.Enabled then
      gpuDrawCalls <- drawCalls
      gpuPrimitives <- primitives
      gpuTextureBinds <- textureBinds
      gpuPublished <- true

  member _.Snapshot = snapshot

  member _.CanScreenshot = canScreenshot

  member _.RequestScreenshot(path: string) =
    if this.Enabled && canScreenshot then
      pendingScreenshot <- ValueSome path

  member _.DrainScreenshot() =
    let pending = pendingScreenshot
    pendingScreenshot <- ValueNone
    pending

/// <summary>Access to the frame profiler and a display helper.</summary>
module Diagnostics =

  /// <summary>Returns the registered profiler, or <c>ValueNone</c>.</summary>
  let inline tryGetProfiler(ctx: GameContext) : FrameProfiler voption =
    GameContext.tryGetService<FrameProfiler> ctx

  /// <summary>Returns the registered profiler.</summary>
  /// <exception cref="T:System.Exception">Thrown when no profiler is registered.</exception>
  let inline getProfiler(ctx: GameContext) : FrameProfiler =
    GameContext.getService<FrameProfiler> ctx

  /// <summary>
  /// Formats a snapshot as two short lines for a text overlay.
  /// </summary>
  /// <remarks>
  /// The first line holds rates and costs. The second holds allocation,
  /// collection, and graphics counts. Call this once per window, not once per
  /// frame, because it allocates.
  /// </remarks>
  let format(stats: FrameStats) : string =
    let drawPart =
      match stats.DrawMs, stats.DrawsPerSecond with
      | ValueSome ms, ValueSome hz -> $" | draw {ms:F2} ms at {hz:F0}/s"
      | _ -> ""

    let gpuPart =
      match stats.GpuDrawCalls with
      | ValueSome calls ->
        let prims = stats.GpuPrimitives |> ValueOption.defaultValue 0L
        let binds = stats.GpuTextureBinds |> ValueOption.defaultValue 0L
        $" | gpu {calls} draws {prims} prims {binds} textures"
      | ValueNone -> ""

    let slowPart =
      if stats.SlowFrames > 0 then
        $" | slow {stats.SlowFrames}"
      else
        ""

    let line1 =
      $"sim {stats.SimStepsPerSecond:F0}/s | frame {stats.FrameMs:F1} ms | worst {stats.WorstFrameMs:F1} ms | update {stats.UpdateMs:F2} ms{drawPart}"

    let line2 =
      $"alloc {stats.AllocatedBytes / 1024L} KB | gen0 {stats.Gen0Collections} | gen1 {stats.Gen1Collections} | gen2 {stats.Gen2Collections}{gpuPart}{slowPart}"

    $"{line1}\n{line2}"
