module Mibo.Fable.Adaptive.AVal

open System
open System.Collections.Generic

/// <summary>
/// An adaptive value whose content is supplied by an external snapshot
/// function, re-read only when invalidated via the handle returned by
/// <see cref="AVal.ofExternal"/>. Not invalidated → reads are O(1): no
/// re-read, no comparison, no allocation. The invalidate handle is O(1)
/// to call; the re-read happens on the next read.
/// </summary>
type ExternalValueNode<'T when 'T: equality>(read: unit -> 'T) =
  let ctx = GraphContext.Current
  let ec = EqualityComparer<'T>.Default
  let mutable value = Unchecked.defaultof<'T>
  let mutable hasValue = false
  let mutable version = 0L
  let mutable dirty = true

  /// <summary>
  /// The invalidate handle implementation (returned by
  /// <see cref="AVal.ofExternal"/>). Call this when the external source
  /// changed; the re-read happens on the next read. Not for direct use.
  /// </summary>
  member this.Invalidate() : unit =
    dirty <- true
    ctx.BumpWriteGeneration()

  member private this.Poll() : unit =
    if dirty then
      dirty <- false
      let next = read()

      if not hasValue || not(ec.Equals(value, next)) then
        value <- next
        hasValue <- true
        version <- version + 1L

  interface IPostSource with
    member this.ApplyPosted() = this.Invalidate()

  interface IAdaptiveValue<'T> with
    member this.GetValue() =
      ctx.ClaimOwner()

      try
        this.Poll()
        ctx.AddDependency(this :> IAdaptiveObject, version)
        value
      finally
        ctx.ReleaseOwner()

    member this.Version =
      // Dirty indicator: version + 1 while invalidated but not yet
      // re-read, so version-checking consumers recompute exactly
      // once; the re-read at GetValue decides the real version.
      if dirty then version + 1L else version

/// <summary>
/// Creates an adaptive value from an external snapshot function and an
/// invalidate handle. The read function runs at most once per invalidate,
/// on the next read; when not invalidated, reads are O(1) and allocate
/// nothing. The handle is O(1) to call.
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
let inline ofExternal(read: unit -> 'T) : aval<'T> * (unit -> unit) =
  let node = ExternalValueNode<'T>(read)
  (node :> aval<'T>, fun () -> node.Invalidate())

/// <summary>
/// Creates a constant adaptive value that never changes.
/// </summary>
/// <remarks>
/// Constant values have zero overhead - they never recompute and don't
/// track dependencies.
/// </remarks>
/// <example>
/// <code>
/// let pi = AVal.constant 3.14159
/// let doubled = AVal.map (fun x -> x * 2.0) pi
/// </code>
/// </example>
let inline constant(value: 'T) : aval<'T> = ConstantValue value :> aval<_>

/// <summary>
/// Creates a constant adaptive value using the given create function. The
/// function runs at most once, on the first read; later reads return the
/// cached value.
/// </summary>
let inline delay(create: unit -> 'T) : aval<'T> =
  LazyConstantValue create :> aval<_>

/// <summary>
/// Creates a changeable value initially holding the given value (the
/// same as <c>CVal.create</c>).
/// </summary>
let inline init(value: 'T) : cval<'T> = ChangeableValue value

/// <summary>
/// Transforms an adaptive value using a mapping function.
/// </summary>
/// <remarks>
/// The function is called lazily - only when the result is read and the
/// source has changed. The result is cached until the source changes.
/// </remarks>
/// <example>
/// <code>
/// let celsius = CVal.create 20.0
/// let fahrenheit = AVal.map (fun c -> c * 9.0/5.0 + 32.0) (CVal.value celsius)
/// </code>
/// </example>
let inline map (f: 'T -> 'U) (value: aval<'T>) : aval<'U> =
  AdaptiveNode(fun () -> f(value.GetValue())) :> aval<_>

/// <summary>
/// Combines two adaptive values using a mapping function.
/// </summary>
/// <remarks>
/// Recomputes only when either input changes. Both inputs are read in a
/// single evaluation.
/// </remarks>
let inline map2
  (f: 'T -> 'U -> 'V)
  (left: aval<'T>)
  (right: aval<'U>)
  : aval<'V> =
  AdaptiveNode(fun () -> f (left.GetValue()) (right.GetValue())) :> aval<_>

/// <summary>
/// Combines three adaptive values using a mapping function.
/// Recomputes only when one of the three inputs changes.
/// </summary>
let inline map3
  (f: 'A -> 'B -> 'C -> 'T)
  (a: aval<'A>)
  (b: aval<'B>)
  (c: aval<'C>)
  : aval<'T> =
  AdaptiveNode(fun () -> f (a.GetValue()) (b.GetValue()) (c.GetValue()))
  :> aval<_>

/// <summary>
/// Combines four adaptive values using a mapping function.
/// Recomputes only when one of the four inputs changes.
/// </summary>
let inline map4
  (f: 'A -> 'B -> 'C -> 'D -> 'T)
  (a: aval<'A>)
  (b: aval<'B>)
  (c: aval<'C>)
  (d: aval<'D>)
  : aval<'T> =
  AdaptiveNode(fun () ->
    f (a.GetValue()) (b.GetValue()) (c.GetValue()) (d.GetValue()))
  :> aval<_>

/// <summary>
/// Combines N adaptive values of the same type using a function that
/// receives all values as an array. Optimized for wide fan-in patterns.
/// </summary>
/// <remarks>
/// The array passed to the compute function is reused by the node and is
/// valid only during the call. Do not retain it. If you only need a
/// reduction (sum, min, max, etc.), prefer <see cref="reduce"/> for
/// better performance.
/// </remarks>
let inline mapN (compute: 'T[] -> 'U) (deps: aval<'T>[]) : aval<'U> =
  MapNNode(deps, compute) :> aval<_>

/// <summary>
/// Reduces N adaptive values using a binary operation and initial value.
/// Optimized for wide fan-in aggregation patterns (sum, product, min,
/// max, etc.). Returns the <c>init</c> value when <c>deps</c> is empty.
/// </summary>
let inline reduce
  (init: 'T)
  (reduce: 'T -> 'T -> 'T)
  (deps: aval<'T>[])
  : aval<'T> =
  ReduceNode(deps, init, reduce) :> aval<_>

/// <summary>
/// Sums N adaptive integer values. Convenience function equivalent to
/// <c>reduce 0 (+)</c>. Returns 0 when <c>deps</c> is empty.
/// </summary>
let inline sum(deps: aval<int>[]) : aval<int> =
  ReduceNode(deps, 0, (+)) :> aval<_>

/// <summary>
/// Adaptively depends on <c>value</c>, applies the mapping, and
/// adaptively depends on the adaptive value the mapping returns. When an
/// input changes, the previously returned inner value is dropped and the
/// mapping selects a new one (dynamic dependencies: the next evaluation
/// re-reads whatever the mapping returns this time).
/// </summary>
let inline bind (f: 'T -> aval<'U>) (value: aval<'T>) : aval<'U> =
  AdaptiveNode(fun () ->
    let inner = f(value.GetValue())
    inner.GetValue())
  :> aval<_>

/// <summary>
/// Adaptively applies the mapping to the two values and adaptively
/// depends on the adaptive value the mapping returns.
/// </summary>
let inline bind2
  (f: 'T -> 'U -> aval<'V>)
  (a: aval<'T>)
  (b: aval<'U>)
  : aval<'V> =
  AdaptiveNode(fun () -> (f (a.GetValue()) (b.GetValue())).GetValue())
  :> aval<_>

/// <summary>
/// Adaptively applies the mapping to the three values and adaptively
/// depends on the adaptive value the mapping returns.
/// </summary>
let inline bind3
  (f: 'T -> 'U -> 'V -> aval<'W>)
  (a: aval<'T>)
  (b: aval<'U>)
  (c: aval<'V>)
  : aval<'W> =
  AdaptiveNode(fun () ->
    (f (a.GetValue()) (b.GetValue()) (c.GetValue())).GetValue())
  :> aval<_>

/// <summary>
/// Creates a custom adaptive value using the given computation.
/// Callers are responsible for removing inputs that are no longer needed.
/// </summary>
let inline custom(compute: unit -> 'T) : aval<'T> =
  AdaptiveNode compute :> aval<_>

/// <summary>Gets the current value of an adaptive value.</summary>
let inline getValue(value: aval<'T>) : 'T = value.GetValue()

/// <summary>Evaluates the given adaptive value (the same as <c>getValue</c>).</summary>
let inline force(value: aval<'T>) : 'T = value.GetValue()
