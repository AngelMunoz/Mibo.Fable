/// <summary>Operations on adaptive maps.</summary>
module Mibo.Fable.Adaptive.AMap

open System
open System.Collections.Generic

/// <summary>An empty adaptive map (FDA <c>AMap.empty</c> parity).</summary>
val empty<'K, 'V> : amap<'K, 'V> when 'K: equality

/// <summary>An adaptive map whose content is computed lazily, once, at first read.</summary>
val inline constant:
  create: (unit -> Dictionary<'K, 'V>) -> amap<'K, 'V> when 'K: equality

/// <summary>Alias of <see cref="constant"/>.</summary>
val inline delay:
  create: (unit -> Dictionary<'K, 'V>) -> amap<'K, 'V> when 'K: equality

/// <summary>An adaptive map over fixed, immutable entries.</summary>
val inline ofSeq: items: seq<'K * 'V> -> amap<'K, 'V> when 'K: equality
/// <summary>An adaptive map over a fixed array of entries.</summary>
val inline ofArray: items: ('K * 'V)[] -> amap<'K, 'V> when 'K: equality
/// <summary>An adaptive map over a fixed list of entries.</summary>
val inline ofList: items: ('K * 'V) list -> amap<'K, 'V> when 'K: equality
/// <summary>An adaptive map over a fixed F# <c>Map</c>.</summary>
val inline ofMap: items: Map<'K, 'V> -> amap<'K, 'V> when 'K: comparison

/// <summary>Maps every entry of the map.</summary>
val inline map:
  f: ('K -> 'V -> 'U) -> mapValue: amap<'K, 'V> -> amap<'K, 'U>
    when 'K: equality

/// <summary>Maps every entry, keeping only the ones the mapping returns a value for (option form).</summary>
val inline choose:
  f: ('K -> 'V -> 'U option) -> mapValue: amap<'K, 'V> -> amap<'K, 'U>
    when 'K: equality

/// <summary>Maps every entry, keeping only the ones the mapping returns a value for (voption form).</summary>
val inline chooseV:
  f: ('K -> 'V -> 'U voption) -> mapValue: amap<'K, 'V> -> amap<'K, 'U>
    when 'K: equality

/// <summary>Maps the values only.</summary>
val inline mapV:
  f: ('V -> 'U) -> mapValue: amap<'K, 'V> -> amap<'K, 'U> when 'K: equality

/// <summary>Unions both maps, resolving colliding keys with the given function.</summary>
val inline unionWith:
  resolve: ('K -> 'V -> 'V -> 'V) ->
  left: amap<'K, 'V> ->
  right: amap<'K, 'V> ->
    amap<'K, 'V>
    when 'K: equality

/// <summary>Unions both maps, preferring the RIGHT value on collision.</summary>
val inline union:
  left: amap<'K, 'V> -> right: amap<'K, 'V> -> amap<'K, 'V> when 'K: equality

/// <summary>The keys present in both maps, with the values paired (struct pair).</summary>
val inline intersect:
  left: amap<'K, 'V1> -> right: amap<'K, 'V2> -> amap<'K, struct ('V1 * 'V2)>
    when 'K: equality

/// <summary>Alias of <see cref="intersect"/>.</summary>
val inline intersectV:
  left: amap<'K, 'V1> -> right: amap<'K, 'V2> -> amap<'K, struct ('V1 * 'V2)>
    when 'K: equality

/// <summary>Intersects both maps, combining the paired values.</summary>
val inline intersectWith:
  combine: ('K -> 'V1 -> 'V2 -> 'V3) ->
  left: amap<'K, 'V1> ->
  right: amap<'K, 'V2> ->
    amap<'K, 'V3>
    when 'K: equality

/// <summary>The keys present in the left map but not in the right map.</summary>
val inline difference:
  left: amap<'K, 'V> -> right: amap<'K, 'V> -> amap<'K, 'V> when 'K: equality

/// <summary>Groups the entries of a map by a computed key (output entries are live adaptive maps).</summary>
val inline groupBy:
  keyOf: ('K -> 'V -> 'G) -> mapValue: amap<'K, 'V> -> amap<'G, amap<'K, 'V>>
    when 'K: equality and 'G: equality

/// <summary>Merges both maps with an option-based mapping (called only when at least one side has a value).</summary>
val inline choose2:
  mapping: ('K -> 'V1 option -> 'V2 option -> 'V3 option) ->
  left: amap<'K, 'V1> ->
  right: amap<'K, 'V2> ->
    amap<'K, 'V3>
    when 'K: equality

/// <summary>Merges both maps with a voption-based mapping.</summary>
val inline choose2V:
  mapping: ('K -> 'V1 voption -> 'V2 voption -> 'V3 voption) ->
  left: amap<'K, 'V1> ->
  right: amap<'K, 'V2> ->
    amap<'K, 'V3>
    when 'K: equality

/// <summary>A map from a set of entries, keeping ALL values of a key in a HashSet.</summary>
val inline ofASet:
  elements: aset<'K * 'V> -> amap<'K, HashSet<'V>>
    when 'K: equality and 'V: equality

/// <summary>A map from a set of entries; duplicate keys keep the LAST value.</summary>
val inline ofASetIgnoreDuplicates:
  elements: aset<'K * 'V> -> amap<'K, 'V> when 'K: equality and 'V: equality

/// <summary>A map from a set, deriving the key from every value (keep-all).</summary>
val inline ofASetMapped:
  getKey: ('V -> 'K) -> elements: aset<'V> -> amap<'K, HashSet<'V>>
    when 'K: equality and 'V: equality

/// <summary>A map from a set, deriving the key from every value; duplicate keys keep the LAST value.</summary>
val inline ofASetMappedIgnoreDuplicates:
  getKey: ('V -> 'K) -> elements: aset<'V> -> amap<'K, 'V>
    when 'K: equality and 'V: equality

/// <summary>Maps the keys of a set to entries.</summary>
val inline mapSet:
  mapping: ('K -> 'V) -> set: aset<'K> -> amap<'K, 'V> when 'K: equality

/// <summary>An adaptive set of the map's key/value pairs (struct pairs).</summary>
val inline toASet:
  mapValue: amap<'K, 'V> -> aset<struct ('K * 'V)>
    when 'K: equality and 'V: equality

/// <summary>An adaptive set of the map's keys.</summary>
val inline keys: mapValue: amap<'K, 'V> -> aset<'K> when 'K: equality
/// <summary>An adaptive list of the map's entries (map iteration order).</summary>
val inline toAList: mapValue: amap<'K, 'V> -> alist<'K * 'V> when 'K: equality
/// <summary>An adaptive map of a list of entries; duplicate keys: the last entry wins.</summary>
val inline ofAList: list: alist<'K * 'V> -> amap<'K, 'V> when 'K: equality

/// <summary>An adaptive set of the map's distinct values.</summary>
val inline toASetValues:
  mapValue: amap<'K, 'V> -> aset<'V> when 'K: equality and 'V: equality

/// <summary>An adaptive map over an adaptive value of a sequence of entries (the rebuild boundary).</summary>
val inline ofAVal<'K, 'V, 'S when 'K: equality and 'S :> seq<'K * 'V>> :
  value: aval<'S> -> amap<'K, 'V>

/// <summary>Adaptively maps over the given value and returns the resulting map (FDA <c>AMap.bind</c> parity).</summary>
val inline bind:
  mapping: ('T -> amap<'K, 'V>) -> value: aval<'T> -> amap<'K, 'V>
    when 'K: equality

/// <summary>Adaptively maps over the two values and returns the resulting map.</summary>
val inline bind2:
  mapping: ('A -> 'B -> amap<'K, 'V>) ->
  a: aval<'A> ->
  b: aval<'B> ->
    amap<'K, 'V>
    when 'K: equality

/// <summary>Adaptively maps over the three values and returns the resulting map.</summary>
val inline bind3:
  mapping: ('A -> 'B -> 'C -> amap<'K, 'V>) ->
  a: aval<'A> ->
  b: aval<'B> ->
  c: aval<'C> ->
    amap<'K, 'V>
    when 'K: equality

/// <summary>An adaptive map driven by a compute function (pull model).</summary>
val inline custom:
  compute: (Dictionary<'K, 'V> -> MapDeltaBuilder<'K, 'V> -> unit) ->
    amap<'K, 'V>
    when 'K: equality

/// <summary>Creates an adaptive map from an external snapshot function and an invalidate handle.</summary>
val inline ofExternal:
  snapshot: (unit -> IReadOnlyDictionary<'K, 'V>) ->
    amap<'K, 'V> * (unit -> unit)
    when 'K: equality

/// <summary>Maps every entry, disposing the mapped value when its key leaves.</summary>
val inline mapUse:
  mapping: ('K -> 'V -> 'W) ->
  mapValue: amap<'K, 'V> ->
    IDisposable * amap<'K, 'W>
    when 'K: equality and 'W: equality and 'W :> IDisposable

/// <summary>Keeps the entries that satisfy the predicate.</summary>
val inline filter:
  predicate: ('K -> 'V -> bool) -> mapValue: amap<'K, 'V> -> amap<'K, 'V>
    when 'K: equality

/// <summary>Keeps the entries whose value satisfies the predicate.</summary>
val inline filterV:
  predicate: ('V -> bool) -> mapValue: amap<'K, 'V> -> amap<'K, 'V>
    when 'K: equality

/// <summary>Adaptively maps every entry of the map to an adaptive value.</summary>
val inline mapA:
  mapping: ('K -> 'V -> aval<'U>) -> mapValue: amap<'K, 'V> -> amap<'K, 'U>
    when 'K: equality

/// <summary>Adaptively maps every entry to an adaptive value, keeping only the entries whose aval holds Some.</summary>
val inline chooseA:
  mapping: ('K -> 'V -> aval<'U option>) ->
  mapValue: amap<'K, 'V> ->
    amap<'K, 'U>
    when 'K: equality

/// <summary>The voption counterpart of chooseA (the no-allocation path).</summary>
val inline chooseAV:
  mapping: ('K -> 'V -> aval<'U voption>) ->
  mapValue: amap<'K, 'V> ->
    amap<'K, 'U>
    when 'K: equality

/// <summary>Adaptively keeps the entries whose predicate aval holds true.</summary>
val inline filterA:
  predicate: ('K -> 'V -> aval<bool>) -> mapValue: amap<'K, 'V> -> amap<'K, 'V>
    when 'K: equality

/// <summary>Equi-joins two maps on a computed key (per-key subgraph, built once, updated in place).</summary>
val inline joinOn:
  left: amap<'K1, 'V1> ->
  right: amap<'K2, 'V2> ->
  keyOfLeft: ('K1 -> 'V1 -> 'K2) ->
  mapping: ('K1 -> aval<'V1> -> aval<'V2 voption> -> aval<'U voption>) ->
    amap<'K1, 'U>
    when 'K1: equality and 'K2: equality

/// <summary>Adaptively reduces the map over the values.</summary>
val inline reduce:
  reduction: AdaptiveReduction<'a, 's, 'v> -> mapValue: amap<'k, 'a> -> aval<'v>
    when 'k: equality

/// <summary>Maps every entry, then reduces the mapped values.</summary>
val inline reduceBy:
  reduction: AdaptiveReduction<'b, 's, 'v> ->
  mapping: ('k -> 'a -> 'b) ->
  mapValue: amap<'k, 'a> ->
    aval<'v>
    when 'k: equality

/// <summary>Adaptively folds the map with <c>add</c>; every removal recomputes.</summary>
val inline fold:
  add: ('s -> 'k -> 'v -> 's) -> zero: 's -> mapValue: amap<'k, 'v> -> aval<'s>
    when 'k: equality

/// <summary>Adaptively folds the map with an invertible <c>subtract</c>.</summary>
val inline foldGroup:
  add: ('s -> 'k -> 'v -> 's) ->
  subtract: ('s -> 'k -> 'v -> 's) ->
  zero: 's ->
  mapValue: amap<'k, 'v> ->
    aval<'s>
    when 'k: equality

/// <summary>Adaptively folds the map with a partially invertible <c>trySubtract</c>.</summary>
val inline foldHalfGroup:
  add: ('s -> 'k -> 'v -> 's) ->
  trySubtract: ('s -> 'k -> 'v -> 's voption) ->
  zero: 's ->
  mapValue: amap<'k, 'v> ->
    aval<'s>
    when 'k: equality

/// <summary>Adaptively sums the mapped entries.</summary>
val inline sumBy:
  mapping: ('k -> 'v -> 'u) -> mapValue: amap<'k, 'v> -> aval<'u>
    when 'k: equality
    and 'u: (static member (+): 'u * 'u -> 'u)
    and 'u: (static member (-): 'u * 'u -> 'u)
    and 'u: (static member Zero: 'u)

/// <summary>Adaptively gets the number of entries (incremental).</summary>
val inline count: mapValue: amap<'K, 'V> -> aval<int> when 'K: equality

/// <summary>Adaptively averages the mapped entries (needs DivideByInt, e.g. float).</summary>
val inline averageBy:
  mapping: ('k -> 'v -> ^u) -> mapValue: amap<'k, 'v> -> aval< ^u >
    when 'k: equality
    and ^u: (static member DivideByInt: ^u * int -> ^u)
    and ^u: (static member (+): ^u * ^u -> ^u)
    and ^u: (static member Zero: ^u)
    and ^u: (static member (-): ^u * ^u -> ^u)

/// <summary>Adaptively tests if the map is empty (incremental).</summary>
val inline isEmpty: mapValue: amap<'K, 'V> -> aval<bool> when 'K: equality

/// <summary>Adaptively tests if any entry satisfies the predicate.</summary>
val inline exists:
  predicate: ('K -> 'V -> bool) -> mapValue: amap<'K, 'V> -> aval<bool>
    when 'K: equality

/// <summary>Adaptively tests if every entry satisfies the predicate.</summary>
val inline forall:
  predicate: ('K -> 'V -> bool) -> mapValue: amap<'K, 'V> -> aval<bool>
    when 'K: equality

/// <summary>Adaptively counts the entries that satisfy the predicate.</summary>
val inline countBy:
  predicate: ('K -> 'V -> bool) -> mapValue: amap<'K, 'V> -> aval<int>
    when 'K: equality

/// <summary>Adaptively reduces the map after mapping every entry to an adaptive value.</summary>
val inline reduceByA:
  reduction: AdaptiveReduction<'U, 's, 'v> ->
  mapping: ('K -> 'V -> aval<'U>) ->
  mapValue: amap<'K, 'V> ->
    aval<'v>
    when 'K: equality

/// <summary>Adaptively counts the entries whose predicate aval holds true.</summary>
val inline countByA:
  predicate: ('K -> 'V -> aval<bool>) -> mapValue: amap<'K, 'V> -> aval<int>
    when 'K: equality

/// <summary>Adaptively tests if any entry's predicate aval holds true.</summary>
val inline existsA:
  predicate: ('K -> 'V -> aval<bool>) -> mapValue: amap<'K, 'V> -> aval<bool>
    when 'K: equality

/// <summary>Adaptively tests if every entry's predicate aval holds true.</summary>
val inline forallA:
  predicate: ('K -> 'V -> aval<bool>) -> mapValue: amap<'K, 'V> -> aval<bool>
    when 'K: equality

/// <summary>Adaptively sums the avals mapped from the entries.</summary>
val inline sumByA:
  mapping: ('K -> 'V -> aval<'U>) -> mapValue: amap<'K, 'V> -> aval<'U>
    when 'K: equality
    and 'U: (static member (+): 'U * 'U -> 'U)
    and 'U: (static member (-): 'U * 'U -> 'U)
    and 'U: (static member Zero: 'U)

/// <summary>Adaptively averages the avals mapped from the entries (needs DivideByInt, e.g. float).</summary>
val inline averageByA:
  mapping: ('K -> 'V -> aval<'U>) -> mapValue: amap<'K, 'V> -> aval<'U>
    when 'K: equality
    and 'U: (static member DivideByInt: 'U * int -> 'U)
    and 'U: (static member (+): 'U * 'U -> 'U)
    and 'U: (static member (-): 'U * 'U -> 'U)
    and 'U: (static member Zero: 'U)

/// <summary>Adaptively gets the minimum of the avals mapped from the entries.</summary>
val inline tryMinA:
  mapping: ('K -> 'V -> aval<'U>) -> mapValue: amap<'K, 'V> -> aval<'U voption>
    when 'K: equality and 'U: comparison

/// <summary>Adaptively gets the maximum of the avals mapped from the entries.</summary>
val inline tryMaxA:
  mapping: ('K -> 'V -> aval<'U>) -> mapValue: amap<'K, 'V> -> aval<'U voption>
    when 'K: equality and 'U: comparison

/// <summary>Adaptively looks up the key (per-key precise, read-time gate).</summary>
val inline tryFind:
  key: 'K -> mapValue: amap<'K, 'V> -> aval<'V voption> when 'K: equality

/// <summary>Adaptively looks up the key (throws KeyNotFoundException when absent at read time).</summary>
val inline find: key: 'K -> mapValue: amap<'K, 'V> -> aval<'V> when 'K: equality

/// <summary>A constant map with a single entry.</summary>
val inline single: key: 'K -> value: 'V -> amap<'K, 'V> when 'K: equality

/// <summary>Materializes the map as an adaptive value (the retain boundary).</summary>
val inline toAVal:
  mapValue: amap<'K, 'V> -> aval<Dictionary<'K, 'V>> when 'K: equality

/// <summary>Returns a transient view of the current state (do not retain).</summary>
val inline getValue:
  mapValue: amap<'K, 'V> -> IReadOnlyDictionary<'K, 'V> when 'K: equality

/// <summary>Materializes the current state as an immutable copy (drains first).</summary>
val inline force: mapValue: amap<'K, 'V> -> Dictionary<'K, 'V> when 'K: equality

/// <summary>Materializes the F# <c>Map</c> counterpart (sorted, structural equality).</summary>
val inline toMap: mapValue: amap<'K, 'V> -> Map<'K, 'V> when 'K: comparison
