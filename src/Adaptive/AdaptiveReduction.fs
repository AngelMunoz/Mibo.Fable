module Mibo.Fable.Adaptive.AdaptiveReduction

/// <summary>Maps the observed value of a reduction.</summary>
let inline mapOut
  (mapping: 'v -> 'w)
  (reduction: AdaptiveReduction<'a, 's, 'v>)
  : AdaptiveReduction<'a, 's, 'w> =
  {
    seed = reduction.seed
    add = reduction.add
    sub = reduction.sub
    view = fun s -> mapping(reduction.view s)
  }

/// <summary>
/// Composes two reductions over the same element in parallel (FDA
/// <c>AdaptiveReduction.par</c> parity; tuple state). The subtract falls
/// back to a full recompute when either side cannot invert.
/// </summary>
let inline par
  (left: AdaptiveReduction<'a, 's, 'v>)
  (right: AdaptiveReduction<'a, 't, 'w>)
  : AdaptiveReduction<'a, 's * 't, 'v * 'w> =
  {
    seed = (left.seed, right.seed)
    add = fun (s, t) a -> (left.add s a, right.add t a)
    sub =
      fun (s, t) a ->
        match left.sub s a with
        | ValueSome s' ->
          match right.sub t a with
          | ValueSome t' -> ValueSome(s', t')
          | ValueNone -> ValueNone
        | ValueNone -> ValueNone
    view = fun (s, t) -> (left.view s, right.view t)
  }

/// <summary>
/// Composes two reductions over the same element in parallel (FDA
/// <c>AdaptiveReduction.structpar</c> parity; struct state, no tuple
/// allocation).
/// </summary>
let inline structpar
  (left: AdaptiveReduction<'a, 's, 'v>)
  (right: AdaptiveReduction<'a, 't, 'w>)
  : AdaptiveReduction<'a, struct ('s * 't), struct ('v * 'w)> =
  {
    seed = struct (left.seed, right.seed)
    add = fun struct (s, t) a -> struct (left.add s a, right.add t a)
    sub =
      fun struct (s, t) a ->
        match left.sub s a with
        | ValueSome s' ->
          match right.sub t a with
          | ValueSome t' -> ValueSome(struct (s', t'))
          | ValueNone -> ValueNone
        | ValueNone -> ValueNone
    view = fun struct (s, t) -> struct (left.view s, right.view t)
  }

/// <summary>
/// Maps the element side of a reduction (FDA <c>AdaptiveReduction.mapIn</c>
/// parity).
/// </summary>
let inline mapIn
  (mapping: 'a -> 'b)
  (reduction: AdaptiveReduction<'b, 's, 'v>)
  : AdaptiveReduction<'a, 's, 'v> =
  {
    seed = reduction.seed
    add = fun s a -> reduction.add s (mapping a)
    sub = fun s a -> reduction.sub s (mapping a)
    view = reduction.view
  }

/// <summary>Counts the elements (FDA <c>AdaptiveReduction.count</c> parity).</summary>
let count<'a> : AdaptiveReduction<'a, int, int> = {
  seed = 0
  add = fun s _ -> s + 1
  sub = fun s _ -> ValueSome(s - 1)
  view = id
}

/// <summary>A reduction with an invertible subtract operation.</summary>
let inline group
  (zero: 's)
  (add: 's -> 'a -> 's)
  (subtract: 's -> 'a -> 's)
  : AdaptiveReduction<'a, 's, 's> =
  {
    seed = zero
    add = add
    sub = fun s a -> ValueSome(subtract s a)
    view = id
  }

/// <summary>A reduction whose subtract may fall back to a full recompute.</summary>
let inline halfGroup
  (zero: 's)
  (add: 's -> 'a -> 's)
  (trySubtract: 's -> 'a -> 's voption)
  : AdaptiveReduction<'a, 's, 's> =
  {
    seed = zero
    add = add
    sub = trySubtract
    view = id
  }

/// <summary>A reduction that recomputes the whole state on every removal.</summary>
let inline fold
  (zero: 's)
  (add: 's -> 'a -> 's)
  : AdaptiveReduction<'a, 's, 's> =
  {
    seed = zero
    add = add
    sub = fun _ _ -> ValueNone
    view = id
  }

/// <summary>Counts the elements for which the mapped value is true.</summary>
let countPositive: AdaptiveReduction<bool, int, int> = {
  seed = 0
  add = fun s b -> if b then s + 1 else s
  sub = fun s b -> if b then ValueSome(s - 1) else ValueSome s
  view = id
}

/// <summary>Counts the elements for which the mapped value is false.</summary>
let countNegative: AdaptiveReduction<bool, int, int> = {
  seed = 0
  add = fun s b -> if b then s else s + 1
  sub = fun s b -> if b then ValueSome s else ValueSome(s - 1)
  view = id
}

/// <summary>Sums the mapped values. Needs an additive numeric type.</summary>
let inline sum() : AdaptiveReduction<'a, 'a, 'a> = {
  seed = LanguagePrimitives.GenericZero<'a>
  add = fun s v -> s + v
  sub = fun s v -> ValueSome(s - v)
  view = id
}

/// <summary>The minimum of the mapped values, or ValueNone when empty. A removal recomputes.</summary>
let inline tryMin() : AdaptiveReduction<'a, 'a voption, 'a voption> = {
  seed = ValueNone
  add =
    fun s v ->
      match s with
      | ValueSome m -> ValueSome(min m v)
      | ValueNone -> ValueSome v
  sub = fun _ _ -> ValueNone
  view = id
}

/// <summary>The maximum of the mapped values, or ValueNone when empty. A removal recomputes.</summary>
let inline tryMax() : AdaptiveReduction<'a, 'a voption, 'a voption> = {
  seed = ValueNone
  add =
    fun s v ->
      match s with
      | ValueSome m -> ValueSome(max m v)
      | ValueNone -> ValueSome v
  sub = fun _ _ -> ValueNone
  view = id
}
