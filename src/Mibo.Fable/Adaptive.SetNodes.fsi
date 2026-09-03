namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Web port of Mibo.Adaptive's Core/Collections/SetNodes.fs: the derived set
// nodes. A derived node registers with its dependencies on first read; a
// dependency push appends to the journal and advances the version and the
// write generation; reads cascade over changed dependencies, drain the
// journal, and return a transient view. Sink registration resolves through
// the WeakMap registry markers (the original's :? ISetSinkRegistry tests are
// always false on JS).

/// <summary>An adaptive set over a fixed, immutable value. The value is computed once, at first read.</summary>
type ConstantSet<'T> =
  new: create: (unit -> seq<'T>) -> ConstantSet<'T>
  interface IAdaptiveSet<'T>
  interface IDisposable

/// <summary>
/// Maps every element of a set (or chooses, when the mapping returns
/// <c>ValueNone</c> to drop an element). Duplicate outputs share one reference
/// count.
/// </summary>
type MapSetNode<'T, 'U when 'T: equality and 'U: equality> =
  new:
    source: IAdaptiveSet<'T> * mapping: ('T -> 'U voption) -> MapSetNode<'T, 'U>

  interface ISetDeltaSink<'T>
  interface IAdaptiveSet<'U>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>Keeps the elements of a set that satisfy a predicate.</summary>
type FilterSetNode<'T when 'T: equality> =
  new: source: IAdaptiveSet<'T> * predicate: ('T -> bool) -> FilterSetNode<'T>
  interface ISetDeltaSink<'T>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>The union of two sets. One reference count per element across both sides.</summary>
type UnionSetNode<'T when 'T: equality> =
  new: left: IAdaptiveSet<'T> * right: IAdaptiveSet<'T> -> UnionSetNode<'T>
  interface ISetDeltaSink<'T>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// A binary set operation over two sources: difference (left minus right),
/// intersect, or xor. Per-side reference counts drive the output membership.
/// </summary>
type TwoSourceSetNode<'T when 'T: equality> =
  new:
    op: TwoSetOp * left: IAdaptiveSet<'T> * right: IAdaptiveSet<'T> ->
      TwoSourceSetNode<'T>

  interface Collections.ITwoSetSinkTarget<'T>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive set over an adaptive value of a sequence. Every change of the
/// value replaces the whole state and emits the diff as the delta (the
/// rebuild boundary, like FDA <c>ASet.ofAVal</c>).
/// </summary>
type OfAvalSetNode<'T, 'S when 'T: equality and 'S :> seq<'T>> =
  new: value: IAdaptiveValue<'S> -> OfAvalSetNode<'T, 'S>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive set over an external reader function. The reader is called on
/// every read (poll); the node diffs the result against its state and emits
/// the diff as the delta. Pull-based: nothing marks this node.
/// </summary>
type ReaderSetNode<'T when 'T: equality> =
  new: reader: (unit -> HashSet<'T>) -> ReaderSetNode<'T>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive set whose content is driven by a compute function. The compute
/// receives the current view and a delta builder; it appends the operations
/// that describe the change since the previous call. Called on every read
/// (poll). FDA <c>ASet.custom</c> parity, pull model.
/// </summary>
type CustomSetNode<'T when 'T: equality> =
  new:
    compute: (HashSet<'T> -> SetDeltaBuilder<'T> -> unit) -> CustomSetNode<'T>

  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive set that unions one inner adaptive set per source element
/// (<c>ASet.collect</c>). The output is the refcounted union of all
/// contributions. A removed source element unregisters its inner sink
/// eagerly (Pitfall 1). Registration is lazy (first read); disposal
/// unregisters everything.
/// </summary>
type CollectSetNode<'T, 'U when 'T: equality and 'U: equality> =
  new:
    source: IAdaptiveSet<'T> * mapping: ('T -> IAdaptiveSet<'U>) ->
      CollectSetNode<'T, 'U>

  interface Collections.ICollectTarget<'T, 'U>
  interface ISetDeltaSink<'T>
  interface IAdaptiveSet<'U>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive set bound to a scalar value (<c>ASet.bind</c>): <c>mapping
/// value</c> selects the inner set; when the value changes, the whole inner
/// set is swapped and the old inner sink is unregistered eagerly.
/// Registration is lazy (first read); disposal unregisters everything.
/// </summary>
type BindSetNode<'T, 'U when 'U: equality> =
  new:
    value: IAdaptiveValue<'T> * mapping: ('T -> IAdaptiveSet<'U>) ->
      BindSetNode<'T, 'U>

  interface ISetDeltaSink<'U>
  interface IAdaptiveSet<'U>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// Maps every element, disposing the mapped value when its last source
/// occurrence leaves (FDA <c>ASet.mapUse</c> parity). The mapped values are
/// stable; two source elements mapping to one value are refcounted and the
/// value is disposed only when the last occurrence leaves.
/// </summary>
type MapUseSetNode<'A, 'B
  when 'A: equality and 'B: equality and 'B :> IDisposable> =
  new: source: IAdaptiveSet<'A> * mapping: ('A -> 'B) -> MapUseSetNode<'A, 'B>
  interface ISetDeltaSink<'A>
  interface IAdaptiveSet<'B>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// A per-element membership test over an adaptive set (the node behind
/// <c>ASet.contains</c>). Registers nothing; the membership is re-read at
/// the next read after a write, and the version advances only when the
/// watched element's membership actually changed.
/// </summary>
type SetContainsNode<'T when 'T: equality> =
  new: source: IAdaptiveSet<'T> * element: 'T -> SetContainsNode<'T>
  interface IAdaptiveValue<bool>
  interface IDisposable

/// <summary>
/// A count over an adaptive set, projected through <c>view</c> (the node
/// behind <c>ASet.count</c> and <c>ASet.isEmpty</c>). Registers nothing; the
/// version advances only when the projected output changed.
/// </summary>
type SetCountNode<'T, 'Out when 'T: equality> =
  new: source: IAdaptiveSet<'T> * view: (int -> 'Out) -> SetCountNode<'T, 'Out>
  interface IAdaptiveValue<'Out>
  interface IDisposable
