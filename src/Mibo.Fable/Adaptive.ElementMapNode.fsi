namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Per-element adaptive map node (docs/2026-08-05-MAPA-DESIGN.md): web port of
// ElementMapNode.fs. AMap.mapA / chooseA / filterA share one node, the keyed
// sibling of ElementSetNode. Also holds the per-key join node
// (AMap.joinOn, docs/2026-08-10-JOIN-DESIGN.md).

/// <summary>
/// Maps every entry of a map to an adaptive value (or chooses/filters, when
/// the aval's value is <c>ValueNone</c> to drop the entry).
/// </summary>
type ElementMapNode<'K, 'V, 'U when 'K: equality> =
  new:
    source: IAdaptiveMap<'K, 'V> * mapping: ('K -> 'V -> aval<'U voption>) ->
      ElementMapNode<'K, 'V, 'U>

  interface ICommittedVersion
  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveMap<'K, 'U>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// Cache entry of <see cref="JoinMapNode&lt;'K1,'V1,'K2,'V2,'U&gt;"/>. The
/// swappable left input (<c>Cell</c>), the current join key, and the
/// version/last protocol of the element nodes.
/// </summary>
type internal JoinEntry<'K2, 'V1, 'U> = internal {
  mutable Aval: aval<'U voption>
  mutable Version: int64
  mutable Last: 'U voption
  mutable Cell: ChangeableValue<'V1>
  mutable JoinKey: 'K2
}

module internal JoinEntry =
  val create<'K2, 'V1, 'U> :
    aval: aval<'U voption> ->
    version: int64 ->
    last: 'U voption ->
    cell: ChangeableValue<'V1> ->
    joinKey: 'K2 ->
      JoinEntry<'K2, 'V1, 'U>

/// <summary>
/// Per-key equi-join over two adaptive maps (the node behind
/// <c>AMap.joinOn</c>). Every left entry maps to an output entry keyed by the
/// left key; the join key is computed from the left entry and looked up in
/// the right map. The per-key subgraph is built once and updated in place:
/// left updates re-apply the value cell (no rebuild), join-key changes re-run
/// the mapping against the new lookup.
/// </summary>
type JoinMapNode<'K1, 'V1, 'K2, 'V2, 'U when 'K1: equality and 'K2: equality> =
  new:
    left: IAdaptiveMap<'K1, 'V1> *
    right: IAdaptiveMap<'K2, 'V2> *
    keyOfLeft: ('K1 -> 'V1 -> 'K2) *
    mapping: ('K1 -> aval<'V1> -> aval<'V2 voption> -> aval<'U voption>) ->
      JoinMapNode<'K1, 'V1, 'K2, 'V2, 'U>

  interface ICommittedVersion
  interface IMapDeltaSink<'K1, 'V1>
  interface IAdaptiveMap<'K1, 'U>
  interface IDisposable
  interface IMapSinkRegistry
