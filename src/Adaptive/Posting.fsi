/// <summary>
/// Applies changes posted to the graph.
/// </summary>
/// <remarks>
/// A "post" is applied at the start of the next graph operation (the drain
/// runs on the outermost claim), so several posts collapse into one
/// batched application — the same observable behavior as the original's
/// per-thread post rings, minus the cross-thread transport.
/// </remarks>
module Mibo.Fable.Adaptive.Posting

/// <summary>
/// Applies all pending posted changes now. Optional: pending posts are
/// applied automatically at the next graph operation. Use this to choose
/// an explicit batch boundary (for example, once per frame).
/// </summary>
val pump: unit -> unit
