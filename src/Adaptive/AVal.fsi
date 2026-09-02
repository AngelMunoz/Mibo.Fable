/// <summary>
/// Core operations for creating and transforming adaptive values.
/// Adaptive values automatically track dependencies and recompute only
/// when their inputs change.
/// </summary>
/// <example>
/// <code>
/// let x = CVal.create 1
/// let y = CVal.create 2
/// let sum = AVal.map2 (+) (CVal.value x) (CVal.value y)
/// AVal.getValue sum  // 3
/// CVal.set 10 x
/// AVal.getValue sum  // 12
/// </code>
/// </example>
module Mibo.Fable.Adaptive.AVal

/// <summary>
/// An adaptive value whose content is supplied by an external snapshot
/// function, re-read only when invalidated via the handle returned by
/// <see cref="ofExternal"/>. Not invalidated → reads are O(1): no
/// re-read, no comparison, no allocation. The invalidate handle is O(1)
/// to call; the re-read happens on the next read.
/// </summary>
type ExternalValueNode<'T when 'T: equality> =
  new: read: (unit -> 'T) -> ExternalValueNode<'T>
  /// <summary>
  /// The invalidate handle implementation (returned by
  /// <see cref="ofExternal"/>). Call this when the external source
  /// changed; the re-read happens on the next read. Not for direct use.
  /// </summary>
  member Invalidate: unit -> unit
  interface IPostSource
  interface IAdaptiveValue<'T>

/// <summary>
/// Creates an adaptive value from an external snapshot function and an
/// invalidate handle. The read function runs at most once per
/// invalidate, on the next read; when not invalidated, reads are O(1)
/// and allocate nothing. The handle is O(1) to call.
/// </summary>
/// <example>
/// <code>
/// let mutable current = 0
/// let value, invalidate = AVal.ofExternal (fun () -> current)
/// current &lt;- 42
/// invalidate ()
/// AVal.getValue value  // 42
/// </code>
/// </example>
val inline ofExternal:
  read: (unit -> 'T) -> aval<'T> * (unit -> unit) when 'T: equality

/// <summary>
/// Creates a constant adaptive value that never changes.
/// </summary>
/// <remarks>
/// Constant values have zero overhead - they never recompute and don't
/// track dependencies.
/// </remarks>
val inline constant: value: 'T -> aval<'T>

/// <summary>
/// Creates a constant adaptive value using the given create function.
/// The function runs at most once, on the first read; later reads return
/// the cached value.
/// </summary>
val inline delay: create: (unit -> 'T) -> aval<'T>

/// <summary>
/// Creates a changeable value initially holding the given value (the
/// same as <c>CVal.create</c>).
/// </summary>
val inline init: value: 'T -> cval<'T>

/// <summary>
/// Transforms an adaptive value using a mapping function.
/// </summary>
/// <remarks>
/// The function is called lazily - only when the result is read and the
/// source has changed. The result is cached until the source changes.
/// </remarks>
val inline map: f: ('T -> 'U) -> value: aval<'T> -> aval<'U>

/// <summary>
/// Combines two adaptive values using a mapping function.
/// </summary>
/// <remarks>
/// Recomputes only when either input changes. Both inputs are read in a
/// single evaluation.
/// </remarks>
val inline map2:
  f: ('T -> 'U -> 'V) -> left: aval<'T> -> right: aval<'U> -> aval<'V>

/// <summary>
/// Combines three adaptive values using a mapping function.
/// Recomputes only when one of the three inputs changes.
/// </summary>
val inline map3:
  f: ('A -> 'B -> 'C -> 'T) ->
  a: aval<'A> ->
  b: aval<'B> ->
  c: aval<'C> ->
    aval<'T>

/// <summary>
/// Combines four adaptive values using a mapping function.
/// Recomputes only when one of the four inputs changes.
/// </summary>
val inline map4:
  f: ('A -> 'B -> 'C -> 'D -> 'T) ->
  a: aval<'A> ->
  b: aval<'B> ->
  c: aval<'C> ->
  d: aval<'D> ->
    aval<'T>

/// <summary>
/// Combines N adaptive values of the same type using a function that
/// receives all values as an array. Optimized for wide fan-in patterns.
/// </summary>
/// <remarks>
/// The array passed to the compute function is reused by the node and is
/// valid only during the call. Do not retain it. If you only need a
/// reduction (sum, min, max, etc.), prefer <see cref="reduce"/>.
/// </remarks>
val inline mapN: compute: ('T[] -> 'U) -> deps: aval<'T>[] -> aval<'U>

/// <summary>
/// Reduces N adaptive values using a binary operation and initial value.
/// Optimized for wide fan-in aggregation patterns (sum, product, min,
/// max, etc.). Returns the <c>init</c> value when <c>deps</c> is empty.
/// </summary>
val inline reduce:
  init: 'T -> reduce: ('T -> 'T -> 'T) -> deps: aval<'T>[] -> aval<'T>

/// <summary>
/// Sums N adaptive integer values. Convenience function equivalent to
/// <c>reduce 0 (+)</c>. Returns 0 when <c>deps</c> is empty.
/// </summary>
val inline sum: deps: aval<int>[] -> aval<int>

/// <summary>
/// Adaptively depends on <c>value</c>, applies the mapping, and
/// adaptively depends on the adaptive value the mapping returns. When an
/// input changes, the previously returned inner value is dropped and the
/// mapping selects a new one (dynamic dependencies: the next evaluation
/// re-reads whatever the mapping returns this time).
/// </summary>
val inline bind: f: ('T -> aval<'U>) -> value: aval<'T> -> aval<'U>

/// <summary>
/// Adaptively applies the mapping to the two values and adaptively
/// depends on the adaptive value the mapping returns.
/// </summary>
val inline bind2:
  f: ('T -> 'U -> aval<'V>) -> a: aval<'T> -> b: aval<'U> -> aval<'V>

/// <summary>
/// Adaptively applies the mapping to the three values and adaptively
/// depends on the adaptive value the mapping returns.
/// </summary>
val inline bind3:
  f: ('T -> 'U -> 'V -> aval<'W>) ->
  a: aval<'T> ->
  b: aval<'U> ->
  c: aval<'V> ->
    aval<'W>

/// <summary>
/// Creates a custom adaptive value using the given computation.
/// Callers are responsible for removing inputs that are no longer needed.
/// </summary>
val inline custom: compute: (unit -> 'T) -> aval<'T>

/// <summary>Gets the current value of an adaptive value.</summary>
val inline getValue: value: aval<'T> -> 'T

/// <summary>Evaluates the given adaptive value (the same as <c>getValue</c>).</summary>
val inline force: value: aval<'T> -> 'T
