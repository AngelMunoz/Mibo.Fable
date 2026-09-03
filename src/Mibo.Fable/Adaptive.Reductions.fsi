namespace Mibo.Fable.Adaptive

open System

// Web port of Mibo.Adaptive's Core/Collections/Reductions.fs: the
// incremental reductions. A record of seed/add/sub/view; the nodes keep the
// reduction state and apply journal deltas at read time (pull-lazy), with a
// full recompute fallback when `sub` cannot invert a removal.

/// <summary>
/// An incremental reduction protocol over elements of type 'a: the state 's is
/// updated with <c>add</c> for added elements and <c>sub</c> for removed ones.
/// <c>sub</c> returns <c>ValueNone</c> when it cannot invert the removal; the
/// library then recomputes the state from the current collection. <c>view</c>
/// projects the state to the observed value.
/// </summary>
/// <remarks>
/// Parity: FDA <c>AdaptiveReduction</c> (AdaptiveValue/AdaptiveReduction.fs).
/// Order of element application is undefined.
/// </remarks>
[<Struct>]
type AdaptiveReduction<'a, 's, 'v> = {
  /// <summary>The initial state.</summary>
  seed: 's
  /// <summary>Applies one added element to the state.</summary>
  add: 's -> 'a -> 's
  /// <summary>
  /// Inverts one removed element. Returns <c>ValueNone</c> when the removal
  /// cannot be inverted; the library recomputes from the current collection.
  /// </summary>
  sub: 's -> 'a -> 's voption
  /// <summary>Projects the state to the observed value.</summary>
  view: 's -> 'v
}

/// <summary>
/// A delta-driven reduction over a set. Registers as a delta sink on the
/// source; the journal is applied to the reduction state on read (drain),
/// with a full recompute fallback when <c>sub</c> cannot invert a removal.
/// </summary>
type SetReduceNode<'a, 'b, 's, 'v when 'a: equality> =
  new:
    source: IAdaptiveSet<'a> *
    mapping: ('a -> 'b) *
    reduction: AdaptiveReduction<'b, 's, 'v> ->
      SetReduceNode<'a, 'b, 's, 'v>

  interface ISetDeltaSink<'a>
  interface IAdaptiveValue<'v>
  interface IDisposable

/// <summary>
/// A reduction over an adaptive list (FDA <c>AList.reduce</c> parity). The
/// reduction state is maintained per delta: an insert adds the mapped value,
/// a remove subtracts it, an update subtracts the old and adds the new; a
/// mid-list insert rebuilds (the incremental add assumes an append or a
/// commutative reduction).
/// </summary>
type ListReduceNode<'a, 'b, 's, 'v> =
  new:
    source: IAdaptiveList<'a> *
    mapping: ('a -> 'b) *
    reduction: AdaptiveReduction<'b, 's, 'v> ->
      ListReduceNode<'a, 'b, 's, 'v>

  interface IListDeltaSink<'a>
  interface IAdaptiveValue<'v>
  interface IDisposable

/// <summary>
/// A delta-driven reduction over a map. Keeps a mirror of the source values
/// so removals and updates can invert (<c>sub</c> receives the old mapped
/// value). The mapping is applied per journal element at drain time.
/// </summary>
type MapReduceNode<'k, 'a, 'b, 's, 'v when 'k: equality> =
  new:
    source: IAdaptiveMap<'k, 'a> *
    mapping: ('k -> 'a -> 'b) *
    reduction: AdaptiveReduction<'b, 's, 'v> ->
      MapReduceNode<'k, 'a, 'b, 's, 'v>

  interface IMapDeltaSink<'k, 'a>
  interface IAdaptiveValue<'v>
  interface IDisposable
