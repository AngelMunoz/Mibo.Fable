/// <summary>Access to the frame profiler and a display helper.</summary>
module Mibo.Diagnostics.Diagnostics

open Mibo.Elmish

/// <summary>Returns the registered profiler, or <c>ValueNone</c>.</summary>
val inline tryGetProfiler: ctx: GameContext -> FrameProfiler voption

/// <summary>Returns the registered profiler.</summary>
/// <exception cref="T:System.Exception">Thrown when no profiler is registered.</exception>
val inline getProfiler: ctx: GameContext -> FrameProfiler

/// <summary>
/// Formats a snapshot as two short lines for a text overlay.
/// </summary>
/// <remarks>
/// The first line holds rates and costs. The second holds allocation,
/// collection, and graphics counts. Call this once per window, not once per
/// frame, because it allocates.
/// </remarks>
val format: stats: FrameStats -> string
