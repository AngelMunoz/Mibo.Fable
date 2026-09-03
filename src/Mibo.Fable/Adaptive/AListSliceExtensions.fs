/// <summary>
/// Slicing for adaptive lists: <c>list.[a..b]</c>. The bounds are clamped;
/// the slice is the window <c>[a, b]</c> inclusive.
/// </summary>
[<AutoOpen>]
module Mibo.Fable.Adaptive.AListSliceExtensions

type IAdaptiveList<'T> with
  /// <summary>
  /// Slicing: <c>list.[a..b]</c>. The bounds are clamped; the slice is the
  /// window <c>[a, b]</c> inclusive.
  /// </summary>
  member this.GetSlice(start: int option, finish: int option) : alist<'T> =
    let s = defaultArg start 0
    let f = defaultArg finish System.Int32.MaxValue
    AList.sub s (max 0 (f - s + 1)) this
