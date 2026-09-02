namespace Mibo.Fable.Adaptive

open System.Collections.Generic

/// <summary>
/// A set with per-element reference counts: two source elements can map onto one
/// output element, and the output element disappears only when the last source
/// reference disappears.
/// </summary>
type internal RefCountedSet<'T when 'T: equality> = internal {
  Data: HashSet<'T>
  Refcounts: Dictionary<'T, int>
} with

  /// Add one reference. Returns whether the element is newly present.
  member Add: item: 'T -> bool
  /// Remove one reference. Returns whether the element is fully removed.
  member Remove: item: 'T -> bool

module internal RefCountedSet =
  val inline create<'T> : unit -> RefCountedSet<'T> when 'T: equality
