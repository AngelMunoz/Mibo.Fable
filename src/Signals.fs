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

[<AllowNullLiteral>]
type Signal<'T> =
  abstract value: 'T with get, set

type CVal<'T> = Signal<'T>

type AVal<'T> = Signal<'T>

module Signals =

  [<Import("batch", "@preact/signals-core")>]
  let inline batch(work: unit -> unit) : unit = jsNative

  [<Import("effect", "@preact/signals-core")>]
  let inline effect(work: unit -> unit) : (unit -> unit) = jsNative

[<RequireQualifiedAccess>]
module CVal =
  [<Import("signal", "@preact/signals-core")>]
  let inline create(value: 'T) : CVal<'T> = jsNative

  let inline get(cval: CVal<'T>) : 'T = cval.value

  let inline set (value: 'T) (cval: CVal<'T>) : unit = cval.value <- value

[<RequireQualifiedAccess>]
module AVal =
  // `untracked` import. Prefer `peek` for single-cell reads.
  [<Import("untracked", "@preact/signals-core")>]
  let inline untracked(f: unit -> 'T) : 'T = jsNative

  [<Import("computed", "@preact/signals-core")>]
  let inline computed(f: unit -> 'T) : AVal<'T> = jsNative

  /// Reads the current value, recomputing this node (and any stale
  /// dependency) at most once since the last change. Use in the frame
  /// force; use `peek` when you must not subscribe (effect bodies).
  let inline get(aval: AVal<'T>) : 'T = aval.value

  /// Reads without creating a dependency.
  let inline peek(aval: AVal<'T>) : 'T = untracked(fun () -> aval.value)

  /// Derived view: recomputes only when `aval` changes.
  let inline map (f: 'T -> 'U) (aval: AVal<'T>) : AVal<'U> =
    computed(fun () -> f(aval.value))

  /// Derived view over two dependencies.
  let inline map2 (f: 'T -> 'U -> 'V) (a: AVal<'T>) (b: AVal<'U>) : AVal<'V> =
    computed(fun () -> f (a.value) (b.value))
