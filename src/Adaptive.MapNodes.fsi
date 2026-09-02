namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Web port of Mibo.Adaptive's Core/Collections/MapNodes.fs: the adaptive map
// nodes. Same journal/drain model as the set nodes; registration happens
// through the map sink registry (WeakMap/prototype probes on JS), so derived
// maps compose freely. Constant maps hold a Dictionary (no FrozenDictionary
// on the port).

/// <summary>An adaptive map over a fixed, immutable value. The value is computed once, at first read.</summary>
type ConstantMap<'K, 'V when 'K: equality> =
  new: create: (unit -> seq<'K * 'V>) -> ConstantMap<'K, 'V>
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable

/// <summary>
/// Maps every entry of a map (or chooses, when the mapping returns
/// <c>ValueNone</c> to drop an entry).
/// </summary>
type MapMapNode<'K, 'V, 'U when 'K: equality> =
  new:
    source: IAdaptiveMap<'K, 'V> * mapping: ('K -> 'V -> 'U voption) ->
      MapMapNode<'K, 'V, 'U>

  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveMap<'K, 'U>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>Keeps the entries of a map that satisfy a predicate.</summary>
type FilterMapNode<'K, 'V when 'K: equality> =
  new:
    source: IAdaptiveMap<'K, 'V> * predicate: ('K -> 'V -> bool) ->
      FilterMapNode<'K, 'V>

  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// Merges two maps with a mapping over both side values (voptions). The
/// mapping decides the semantics: choose2, intersect(With), union(With) are
/// all this node with different mappings (FDA models them all on
/// Choose2VReader). The mapping is called only when at least one side has a
/// value.
/// </summary>
type Choose2MapNode<'K, 'V1, 'V2, 'V3 when 'K: equality> =
  new:
    left: IAdaptiveMap<'K, 'V1> *
    right: IAdaptiveMap<'K, 'V2> *
    mapping: ('K -> 'V1 voption -> 'V2 voption -> 'V3 voption) ->
      Choose2MapNode<'K, 'V1, 'V2, 'V3>

  interface Collections.ISideMapSinkTarget
  interface IAdaptiveMap<'K, 'V3>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>Internal. State of a set-to-map node (one value per key).</summary>
type internal SetToMapState<'K, 'V, 'T when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Data: Dictionary<'K, 'V>
  mutable Journal: SetDelta<'T>
  mutable Out: MapDelta<'K, 'V>
}

module internal SetToMapState =
  val create<'K, 'V, 'T> :
    depCount: int -> SetToMapState<'K, 'V, 'T> when 'K: equality

/// <summary>
/// A map from a set: every element maps to an entry. When multiple elements
/// map to one key, the last value wins (<c>ofASetIgnoreDuplicates</c>); a
/// removal of an entry whose value is not the current one is a no-op (gated).
/// <c>mapSet</c> uses an unconditional removal (a set key appears once).
/// </summary>
type SetToMapNode<'K, 'V, 'T when 'K: equality and 'T: equality> =
  new:
    source: IAdaptiveSet<'T> * toEntry: ('T -> 'K * 'V) * gated: bool ->
      SetToMapNode<'K, 'V, 'T>

  interface ISetDeltaSink<'T>
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>Internal. State of a keep-all set-to-map node (per-key value sets).</summary>
type internal SetToMapKeepAllState<'K, 'V, 'T when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Data: Dictionary<'K, HashSet<'V>>
  mutable Journal: SetDelta<'T>
  mutable Out: MapDelta<'K, HashSet<'V>>
}

module internal SetToMapKeepAllState =
  val create<'K, 'V, 'T> :
    depCount: int -> SetToMapKeepAllState<'K, 'V, 'T> when 'K: equality

/// <summary>
/// A map from a set of entries: every key keeps ALL its values in a HashSet
/// (<c>ofASet</c>/<c>ofASetMapped</c> FDA parity). A changed value set emits a
/// fresh HashSet in the delta.
/// </summary>
type SetToMapKeepAllNode<'K, 'V, 'T when 'K: equality and 'T: equality> =
  new:
    source: IAdaptiveSet<'T> * toEntry: ('T -> 'K * 'V) ->
      SetToMapKeepAllNode<'K, 'V, 'T>

  interface ISetDeltaSink<'T>
  interface IAdaptiveMap<'K, HashSet<'V>>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>Internal. State of a map-to-set node (keys or distinct values).</summary>
type internal MapToSetState<'K, 'V, 'T when 'K: equality and 'T: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Mirror: Dictionary<'K, 'T>
  mutable Out: RefCountedSet<'T>
  mutable Journal: MapDelta<'K, 'V>
  mutable OutDelta: SetDelta<'T>
}

module internal MapToSetState =
  val create<'K, 'V, 'T> :
    depCount: int -> MapToSetState<'K, 'V, 'T>
      when 'K: equality and 'T: equality

/// <summary>
/// A set from a map: every entry contributes the selected value (the key for
/// <c>toASet</c>, the value for <c>toASetValues</c>). Equal selections share
/// one reference count: an entry removal drops the output element only when
/// the last contributing entry disappears.
/// </summary>
type MapToSetNode<'K, 'V, 'T when 'K: equality and 'T: equality> =
  new:
    source: IAdaptiveMap<'K, 'V> * select: ('K -> 'V -> 'T) ->
      MapToSetNode<'K, 'V, 'T>

  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive map over an adaptive value of a sequence of entries. Every
/// change of the value replaces the whole state and emits the diff as the
/// delta (the rebuild boundary, like <see cref="OfAvalSetNode"/>).
/// </summary>
type OfAvalMapNode<'K, 'V, 'S when 'K: equality and 'S :> seq<'K * 'V>> =
  new: value: IAdaptiveValue<'S> -> OfAvalMapNode<'K, 'V, 'S>
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// An adaptive map whose content is driven by a compute function (FDA
/// <c>AMap.custom</c> parity, pull model). The compute receives the current
/// view and a delta builder and appends the operations that describe the
/// change since the previous call.
/// </summary>
type CustomMapNode<'K, 'V when 'K: equality> =
  new:
    compute: (Dictionary<'K, 'V> -> MapDeltaBuilder<'K, 'V> -> unit) ->
      CustomMapNode<'K, 'V>

  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// An adaptive map bound to a scalar value (<c>AMap.bind</c>): <c>mapping
/// value</c> selects the inner map; when the value changes, the whole inner
/// map is swapped and the old inner sink is unregistered eagerly.
/// Registration is lazy (first read); disposal unregisters everything.
/// </summary>
type BindMapNode<'K, 'V, 'T when 'K: equality> =
  new:
    value: IAdaptiveValue<'T> * mapping: ('T -> IAdaptiveMap<'K, 'V>) ->
      BindMapNode<'K, 'V, 'T>

  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// An adaptive map of a list of entries (FDA <c>AMap.ofAList</c> parity). The
/// list deltas are converted to map deltas; the mirror (key per input
/// position) is aligned with the source.
/// </summary>
type AListToMapNode<'K, 'V when 'K: equality> =
  new: source: IAdaptiveList<'K * 'V> -> AListToMapNode<'K, 'V>
  interface IListDeltaSink<'K * 'V>
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// An adaptive list of a map's entries (FDA <c>AMap.toAList</c> parity, poll
/// node). The order is the map's iteration order; every read rebuilds and
/// emits the positional diff.
/// </summary>
type MapToAListNode<'K, 'V when 'K: equality> =
  new: source: IAdaptiveMap<'K, 'V> -> MapToAListNode<'K, 'V>
  interface IAdaptiveList<'K * 'V>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// Maps every entry, disposing the mapped value when its key leaves (FDA
/// <c>AMap.mapUse</c> parity). The mapped values are stable (the mapping runs
/// once per key).
/// </summary>
type MapUseMapNode<'K, 'V, 'W
  when 'K: equality and 'W: equality and 'W :> IDisposable> =
  new:
    source: IAdaptiveMap<'K, 'V> * mapping: ('K -> 'V -> 'W) ->
      MapUseMapNode<'K, 'V, 'W>

  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveMap<'K, 'W>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// A per-key lookup over an adaptive map (the node behind <c>AMap.tryFind</c>
/// and <c>AMap.find</c>). Registers nothing; the version advances only when
/// the value at that key actually changed (read-time equality gate).
/// </summary>
type MapLookupNode<'K, 'V when 'K: equality> =
  new: source: IAdaptiveMap<'K, 'V> * key: 'K -> MapLookupNode<'K, 'V>
  interface IAdaptiveValue<'V voption>
  interface IDisposable

/// <summary>
/// A count over an adaptive map, projected through <c>view</c> (the node
/// behind <c>AMap.count</c> and <c>AMap.isEmpty</c>). Registers nothing; the
/// version advances only when the projected output changed.
/// </summary>
type MapCountNode<'K, 'V, 'Out when 'K: equality> =
  new:
    source: IAdaptiveMap<'K, 'V> * view: (int -> 'Out) ->
      MapCountNode<'K, 'V, 'Out>

  interface IAdaptiveValue<'Out>
  interface IDisposable

/// <summary>
/// A live per-group map (the value of a <c>AMap.groupBy</c> output entry).
/// The owning <see cref="GroupByMapNode&lt;'K,'V,'G&gt;"/> is its only
/// writer. The node is garbage collected with its group (weak sink
/// references, no manual disposal).
/// </summary>
type internal GroupMapChildNode<'K, 'V when 'K: equality> =
  new: unit -> GroupMapChildNode<'K, 'V>
  member Count: int
  /// Apply one set entry (equal-value elision) and deliver the delta.
  member internal ApplySetOne: k: 'K * v: 'V -> unit
  /// Apply one remove entry and deliver the delta.
  member internal ApplyRemOne: k: 'K -> unit
  interface IAdaptiveMap<'K, 'V>
  interface IMapSinkRegistry
  interface IDisposable

/// <summary>
/// Groups the entries of an adaptive map by a computed key (the node behind
/// <c>AMap.groupBy</c>). The output entries are live adaptive maps
/// (<see cref="GroupMapChildNode&lt;'K,'V&gt;"/>); group-content changes
/// reach the consumers through the children, so the output map's version
/// moves only for group add/remove. A group disappears when it becomes empty.
/// </summary>
type GroupByMapNode<'K, 'V, 'G when 'K: equality and 'G: equality> =
  new:
    source: IAdaptiveMap<'K, 'V> * keyOf: ('K -> 'V -> 'G) ->
      GroupByMapNode<'K, 'V, 'G>

  interface IMapDeltaSink<'K, 'V>
  interface IAdaptiveMap<'G, amap<'K, 'V>>
  interface IDisposable
  interface IMapSinkRegistry
