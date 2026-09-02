/// <summary>Operations on changeable maps.</summary>
module Mibo.Fable.Adaptive.CMap

open System
open System.Collections.Generic

/// <summary>An empty changeable map.</summary>
val inline empty<'K, 'V> : cmap<'K, 'V> when 'K: equality
/// <summary>A changeable map with the given entries.</summary>
val inline ofSeq: items: seq<'K * 'V> -> cmap<'K, 'V> when 'K: equality

/// <summary>Adds or updates an entry. No-op when the value is unchanged.</summary>
val inline addOrUpdate:
  key: 'K -> value: 'V -> mapValue: cmap<'K, 'V> -> unit when 'K: equality

/// <summary>Removes an entry. No-op when absent.</summary>
val inline remove: key: 'K -> mapValue: cmap<'K, 'V> -> unit when 'K: equality

/// <summary>Posts an add or update (the cval.Post handoff pattern).</summary>
val inline postAddOrUpdate:
  key: 'K -> value: 'V -> mapValue: cmap<'K, 'V> -> unit when 'K: equality

/// <summary>Posts a remove.</summary>
val inline postRemove:
  key: 'K -> mapValue: cmap<'K, 'V> -> unit when 'K: equality

/// <summary>Posts a full replace (supersedes the other ops of the same pending batch).</summary>
val inline postSet:
  value: seq<'K * 'V> -> mapValue: cmap<'K, 'V> -> unit when 'K: equality

/// <summary>Posts a clear (a full replace with the empty map).</summary>
val inline postClear: mapValue: cmap<'K, 'V> -> unit when 'K: equality

/// <summary>Replaces the whole map.</summary>
val inline set:
  value: Map<'K, 'V> -> mapValue: cmap<'K, 'V> -> unit when 'K: comparison

/// <summary>Tests whether the key is present.</summary>
val inline containsKey:
  key: 'K -> mapValue: cmap<'K, 'V> -> bool when 'K: equality

/// <summary>Gets the value for the key, or ValueNone when absent.</summary>
val inline tryGetValue:
  key: 'K -> mapValue: cmap<'K, 'V> -> 'V voption when 'K: equality

/// <summary>Gets the value for the key (KeyNotFoundException when absent).</summary>
val inline item: key: 'K -> mapValue: cmap<'K, 'V> -> 'V when 'K: equality

/// <summary>Replaces the whole map and returns whether the content changed.</summary>
val inline updateTo:
  target: seq<'K * 'V> -> mapValue: cmap<'K, 'V> -> bool when 'K: equality

/// <summary>Applies a batch of map operations atomically.</summary>
val perform:
  delta: MapDeltaBuilder<'K, 'V> -> mapValue: cmap<'K, 'V> -> unit
    when 'K: equality

/// <summary>Removes all entries (one atomic batch).</summary>
val inline clear: mapValue: cmap<'K, 'V> -> unit when 'K: equality
/// <summary>Views the changeable map as an adaptive map.</summary>
val inline value: mapValue: cmap<'K, 'V> -> amap<'K, 'V> when 'K: equality

/// <summary>Materializes the current state as an immutable snapshot.</summary>
val inline force: mapValue: cmap<'K, 'V> -> Dictionary<'K, 'V> when 'K: equality

/// <summary>Materializes the F# <c>Map</c> counterpart.</summary>
val inline toMap: mapValue: cmap<'K, 'V> -> Map<'K, 'V> when 'K: comparison
