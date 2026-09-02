module Mibo.Fable.Adaptive.Posting

/// <summary>
/// Applies all pending posted changes now. Optional: pending posts are applied
/// automatically at the next graph operation. Use this to choose an explicit
/// batch boundary (for example, once per frame).
/// </summary>
let pump() : unit = GraphContext.Current.Pump()
