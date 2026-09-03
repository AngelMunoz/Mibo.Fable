/// <summary>Combinators for building <see cref="AdaptiveReduction"/> values.</summary>
module Mibo.Fable.Adaptive.AdaptiveReduction

/// <summary>Maps the observed value of a reduction.</summary>
val inline mapOut:
  mapping: ('v -> 'w) ->
  reduction: AdaptiveReduction<'a, 's, 'v> ->
    AdaptiveReduction<'a, 's, 'w>

/// <summary>
/// Composes two reductions over the same element in parallel (FDA
/// <c>AdaptiveReduction.par</c> parity; tuple state). The subtract falls
/// back to a full recompute when either side cannot invert.
/// </summary>
val inline par:
  left: AdaptiveReduction<'a, 's, 'v> ->
  right: AdaptiveReduction<'a, 't, 'w> ->
    AdaptiveReduction<'a, ('s * 't), ('v * 'w)>

/// <summary>
/// Composes two reductions over the same element in parallel (FDA
/// <c>AdaptiveReduction.structpar</c> parity; struct state).
/// </summary>
val inline structpar:
  left: AdaptiveReduction<'a, 's, 'v> ->
  right: AdaptiveReduction<'a, 't, 'w> ->
    AdaptiveReduction<'a, struct ('s * 't), struct ('v * 'w)>

/// <summary>
/// Maps the element side of a reduction (FDA <c>AdaptiveReduction.mapIn</c>
/// parity).
/// </summary>
val inline mapIn:
  mapping: ('a -> 'b) ->
  reduction: AdaptiveReduction<'b, 's, 'v> ->
    AdaptiveReduction<'a, 's, 'v>

/// <summary>Counts the elements (FDA <c>AdaptiveReduction.count</c> parity).</summary>
val count<'a> : AdaptiveReduction<'a, int, int>

/// <summary>A reduction with an invertible subtract operation.</summary>
val inline group:
  zero: 's ->
  add: ('s -> 'a -> 's) ->
  subtract: ('s -> 'a -> 's) ->
    AdaptiveReduction<'a, 's, 's>

/// <summary>A reduction whose subtract may fall back to a full recompute.</summary>
val inline halfGroup:
  zero: 's ->
  add: ('s -> 'a -> 's) ->
  trySubtract: ('s -> 'a -> 's voption) ->
    AdaptiveReduction<'a, 's, 's>

/// <summary>A reduction that recomputes the whole state on every removal.</summary>
val inline fold:
  zero: 's -> add: ('s -> 'a -> 's) -> AdaptiveReduction<'a, 's, 's>

/// <summary>Counts the elements for which the mapped value is true.</summary>
val countPositive: AdaptiveReduction<bool, int, int>

/// <summary>Counts the elements for which the mapped value is false.</summary>
val countNegative: AdaptiveReduction<bool, int, int>

/// <summary>Sums the mapped values. Needs an additive numeric type.</summary>
val inline sum:
  unit -> AdaptiveReduction<'a, 'a, 'a>
    when 'a: (static member Zero: 'a)
    and 'a: (static member (+): 'a * 'a -> 'a)
    and 'a: (static member (-): 'a * 'a -> 'a)

/// <summary>The minimum of the mapped values, or ValueNone when empty. A removal recomputes.</summary>
val inline tryMin:
  unit -> AdaptiveReduction<'a, 'a voption, 'a voption> when 'a: comparison

/// <summary>The maximum of the mapped values, or ValueNone when empty. A removal recomputes.</summary>
val inline tryMax:
  unit -> AdaptiveReduction<'a, 'a voption, 'a voption> when 'a: comparison
