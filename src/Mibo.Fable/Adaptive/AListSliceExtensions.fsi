/// <summary>
/// Slicing for adaptive lists: <c>list.[a..b]</c>. The bounds are clamped;
/// the slice is the window [a, b] inclusive.
/// </summary>
[<AutoOpen>]
module Mibo.Fable.Adaptive.AListSliceExtensions

type IAdaptiveList<'T> with
  /// <summary>Slicing: <c>list.[a..b]</c>. The bounds are clamped.</summary>
  member GetSlice: start: int option * finish: int option -> alist<'T>
