namespace Mibo.Signals

// ─────────────────────────────────────────────────────────────────────────────
// Signals — the web-side counterpart of Mibo.Adaptive's cells.
//
// Backed by @preact/signals-core: a tiny reactive graph with memoized
// computations. Where Mibo.Adaptive has cval (changeable roots) and aval
// (derived views), this layer exposes the same vocabulary:
//
//   CVal<'T> — a writable signal root. `update` phases write these.
//   AVal<'T> — a derived, memoized view (computed). The frame force
//              reads these once per step; each recomputes only if one
//              of its dependencies changed since the last read.
//
// Both are the same JS type (a preact signal); the two names carry the
// intent — write roots through CVal helpers, read views through AVal
// helpers — exactly like the cval/aval split in Defli/Kimo.
//
// Semantics notes for .NET-siders:
//  * No transactions — wrap a group of writes in `Signals.batch` to
//    postpone dependent work until the batch ends. Inside a frame step
//    nothing can observe mid-batch anyway (single thread, the force runs
//    after the update drain).
//  * Invalidation is reference-based (Object.is): writing a new record
//    instance always propagates, writing back the same instance is a
//    no-op. F# records compile to fresh objects, so "changed" is yours
//    to mean.
//  * `AVal.computed` is lazy: it recomputes on the next read after a
//    dependency moved. The frame force reads each projection exactly
//    once per step — recomputing each at most once, never speculatively.
// ─────────────────────────────────────────────────────────────────────────────

open Fable.Core

/// A preact signal: a cell holding 'T with memoized dependency tracking.
[<AllowNullLiteral>]
type Signal<'T> =
  abstract value: 'T with get, set

/// A writable signal root — the counterpart of Mibo.Adaptive's `cval`.
type CVal<'T> = Signal<'T>
/// A derived, memoized signal view — the counterpart of `aval`.
type AVal<'T> = Signal<'T>

/// The push side of the graph: batching and effects over the raw
/// @preact/signals-core bindings.
module Signals =
  /// Runs `work` and postpones dependent effect re-runs until it returns.
  /// Frame steps wrap their writes in a batch; nothing can observe the
  /// graph mid-batch anyway (single thread — the force runs after).
  [<Import("batch", "@preact/signals-core")>]
  val inline batch: work: (unit -> unit) -> unit

  /// Runs `work` now and again whenever any cell it read changes.
  /// Returns the disposer. Effects are the push side — the runner and the
  /// frame force never need them; view hosts and debugging aids do.
  [<Import("effect", "@preact/signals-core")>]
  val inline effect: work: (unit -> unit) -> (unit -> unit)

[<RequireQualifiedAccess>]
module CVal =
  /// Creates a signal root holding the initial value.
  [<Import("signal", "@preact/signals-core")>]
  val inline create: value: 'T -> CVal<'T>

  /// Reads the current value. Inside a `computed`/`effect` this
  /// subscribes to the cell; outside, it is a plain read.
  val inline get: cval: CVal<'T> -> 'T
  /// Writes the cell. Equal-by-reference writes are no-ops.
  val inline set: value: 'T -> cval: CVal<'T> -> unit

[<RequireQualifiedAccess>]
module AVal =
  /// Runs `f` with dependency tracking suppressed - the underlying
  // `untracked` import. Prefer `peek` for single-cell reads.
  [<Import("untracked", "@preact/signals-core")>]
  val inline untracked: f: (unit -> 'T) -> 'T

  /// Creates a memoized view from an arbitrary read (may touch many
  /// cells - all become dependencies).
  [<Import("computed", "@preact/signals-core")>]
  val inline computed: f: (unit -> 'T) -> AVal<'T>

  /// Reads the current value, recomputing this node (and any stale
  /// dependency) at most once since the last change. Use in the frame
  /// force; use `peek` when you must not subscribe (effect bodies).
  val inline get: aval: AVal<'T> -> 'T

  /// Reads without creating a dependency.
  val inline peek: aval: AVal<'T> -> 'T

  /// Derived view: recomputes only when `aval` changes.
  val inline map: f: ('T -> 'U) -> aval: AVal<'T> -> AVal<'U>

  /// Derived view over two dependencies.
  val inline map2: f: ('T -> 'U -> 'V) -> a: AVal<'T> -> b: AVal<'U> -> AVal<'V>
