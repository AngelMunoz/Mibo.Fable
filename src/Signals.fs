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
  let inline batch(work: unit -> unit) : unit = jsNative

  /// Runs `work` now and again whenever any cell it read changes.
  /// Returns the disposer. Effects are the push side — the runner and the
  /// frame force never need them; view hosts and debugging aids do.
  [<Import("effect", "@preact/signals-core")>]
  let inline effect(work: unit -> unit) : (unit -> unit) = jsNative

  /// Creates a raw signal cell.
  [<Import("signal", "@preact/signals-core")>]
  let internal rawSignal(value: 'T) : Signal<'T> = jsNative

  /// Creates a raw memoized view.
  [<Import("computed", "@preact/signals-core")>]
  let internal rawComputed(f: unit -> 'T) : Signal<'T> = jsNative

  /// Reads a signal without creating a dependency.
  [<Import("untracked", "@preact/signals-core")>]
  let internal rawUntracked(f: unit -> 'T) : 'T = jsNative

[<RequireQualifiedAccess>]
module CVal =
  /// Creates a signal root holding the initial value.
  let inline create(value: 'T) : CVal<'T> = Signals.rawSignal value

  /// Reads the current value. Inside a `computed`/`effect` this
  /// subscribes to the cell; outside, it is a plain read.
  let inline get(cval: CVal<'T>) : 'T = cval.value

  /// Writes the cell. Equal-by-reference writes are no-ops.
  let inline set (value: 'T) (cval: CVal<'T>) : unit = cval.value <- value

[<RequireQualifiedAccess>]
module AVal =
  /// Reads the current value, recomputing this node (and any stale
  /// dependency) at most once since the last change. Use in the frame
  /// force; use `peek` when you must not subscribe (effect bodies).
  let inline get(aval: AVal<'T>) : 'T = aval.value

  /// Reads without creating a dependency.
  let inline peek(aval: AVal<'T>) : 'T =
    Signals.rawUntracked(fun () -> aval.value)

  /// Derived view: recomputes only when `aval` changes.
  let inline map (f: 'T -> 'U) (aval: AVal<'T>) : AVal<'U> =
    Signals.rawComputed(fun () -> f(aval.value))

  /// Derived view over two dependencies.
  let inline map2 (f: 'T -> 'U -> 'V) (a: AVal<'T>) (b: AVal<'U>) : AVal<'V> =
    Signals.rawComputed(fun () -> f (a.value) (b.value))

  /// Creates a memoized view from an arbitrary read (may touch many
  /// cells — all become dependencies).
  let inline computed(f: unit -> 'T) : AVal<'T> = Signals.rawComputed f
