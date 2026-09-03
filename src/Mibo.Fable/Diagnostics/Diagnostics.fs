module Mibo.Diagnostics.Diagnostics

open Mibo.Elmish

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
