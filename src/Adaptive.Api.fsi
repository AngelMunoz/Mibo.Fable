namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Public API: web port of Mibo.Adaptive's Core/Collections/Api.fs.
// `force` is the materialization point: it drains and returns an immutable
// copy that the library never touches again (a fresh HashSet/Dictionary on
// JS, standing in for the .NET FrozenSet/FrozenDictionary). `getValue`
// returns a transient view for computations. `toSet`/`toMap` materialize the
// F# Set/Map counterparts.

/// <summary>Operations on adaptive sets.</summary>
module ASet =
  /// <summary>An empty adaptive set (FDA <c>ASet.empty</c> parity).</summary>
  val empty<'T> : aset<'T>
  /// <summary>An adaptive set over fixed, immutable items.</summary>
  val inline ofSeq: items: seq<'T> -> aset<'T>
  /// <summary>An adaptive set over a fixed array.</summary>
  val inline ofArray: items: 'T[] -> aset<'T>
  /// <summary>An adaptive set over a fixed list.</summary>
  val inline ofList: items: 'T list -> aset<'T>
  /// <summary>An adaptive set over a fixed HashSet.</summary>
  val inline ofHashSet: items: HashSet<'T> -> aset<'T>
  /// <summary>An adaptive set whose content is fixed but computed lazily, once, at first read.</summary>
  val inline constant: create: (unit -> HashSet<'T>) -> aset<'T>
  /// <summary>Alias of <see cref="constant"/> (FDA parity: delay is constant).</summary>
  val inline delay: create: (unit -> HashSet<'T>) -> aset<'T>

  /// <summary>Maps every element of the set.</summary>
  val inline map:
    f: ('T -> 'U) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Maps every element, keeping only the ones the mapping returns a value for.</summary>
  val inline choose:
    f: ('T -> 'U option) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Maps every element, keeping only the ones the mapping returns a value for (voption form).</summary>
  val inline chooseV:
    f: ('T -> 'U voption) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Keeps the elements that satisfy the predicate.</summary>
  val inline filter:
    predicate: ('T -> bool) -> set: aset<'T> -> aset<'T> when 'T: equality

  /// <summary>Adaptively maps every element of the set to an adaptive value (FDA <c>ASet.mapA</c> parity).</summary>
  val inline mapA:
    mapping: ('T -> aval<'U>) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Adaptively maps every element to an adaptive value, keeping only the elements whose aval holds Some.</summary>
  val inline chooseA:
    mapping: ('T -> aval<'U option>) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>The voption counterpart of chooseA (the no-allocation path).</summary>
  val inline chooseAV:
    mapping: ('T -> aval<'U voption>) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Adaptively keeps the elements whose predicate aval holds true (FDA <c>ASet.filterA</c> parity).</summary>
  val inline filterA:
    predicate: ('T -> aval<bool>) -> set: aset<'T> -> aset<'T> when 'T: equality

  /// <summary>The union of two sets.</summary>
  val inline union:
    left: aset<'T> -> right: aset<'T> -> aset<'T> when 'T: equality

  /// <summary>The union of all given sets (static sequence; the dynamic form is collect).</summary>
  val unionMany: sets: seq<aset<'T>> -> aset<'T> when 'T: equality

  /// <summary>Adaptively maps over the given set and unions all resulting sets (FDA <c>ASet.collect</c> parity).</summary>
  val inline collect:
    mapping: ('T -> aset<'U>) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Flattens the set by statically expanding each element to a sequence.</summary>
  val inline collect':
    mapping: ('T -> seq<'U>) -> set: aset<'T> -> aset<'U>
      when 'T: equality and 'U: equality

  /// <summary>Maps every element, disposing the mapped value when its last source occurrence leaves.</summary>
  val inline mapUse:
    mapping: ('A -> 'B) -> set: aset<'A> -> IDisposable * aset<'B>
      when 'A: equality and 'B: equality and 'B :> IDisposable

  /// <summary>The elements of the left set that are not in the right set.</summary>
  val inline difference:
    left: aset<'T> -> right: aset<'T> -> aset<'T> when 'T: equality

  /// <summary>The elements present in both sets.</summary>
  val inline intersect:
    left: aset<'T> -> right: aset<'T> -> aset<'T> when 'T: equality

  /// <summary>The symmetric difference: elements present in exactly one set.</summary>
  val inline xor:
    left: aset<'T> -> right: aset<'T> -> aset<'T> when 'T: equality

  /// <summary>An adaptive set over an adaptive value of a sequence (the rebuild boundary).</summary>
  val inline ofAVal<'T, 'S when 'T: equality and 'S :> seq<'T>> :
    value: aval<'S> -> aset<'T>

  /// <summary>Adaptively maps over the given value and returns the resulting set (FDA <c>ASet.bind</c> parity).</summary>
  val inline bind:
    mapping: ('T -> aset<'U>) -> value: aval<'T> -> aset<'U> when 'U: equality

  /// <summary>An adaptive numeric range; the bounds are inclusive.</summary>
  val inline range:
    min: aval< ^T > -> max: aval< ^T > -> aset< ^T >
      when ^T: comparison
      and ^T: equality
      and ^T: (static member (+): ^T * ^T -> ^T)
      and ^T: (static member One: ^T)

  /// <summary>Adaptively maps over the two values and returns the resulting set.</summary>
  val inline bind2:
    mapping: ('A -> 'B -> aset<'C>) -> a: aval<'A> -> b: aval<'B> -> aset<'C>
      when 'C: equality

  /// <summary>Adaptively maps over the three values and returns the resulting set.</summary>
  val inline bind3:
    mapping: ('A -> 'B -> 'C -> aset<'D>) ->
    a: aval<'A> ->
    b: aval<'B> ->
    c: aval<'C> ->
      aset<'D>
      when 'D: equality

  /// <summary>An adaptive set over an external reader function (pull-based poll).</summary>
  val inline ofReader:
    reader: (unit -> HashSet<'T>) -> aset<'T> when 'T: equality

  /// <summary>An adaptive set driven by a compute function (FDA <c>ASet.custom</c> parity, pull model).</summary>
  val inline custom:
    compute: (HashSet<'T> -> SetDeltaBuilder<'T> -> unit) -> aset<'T>
      when 'T: equality

  /// <summary>Creates an adaptive set from an external snapshot function and an invalidate handle.</summary>
  val inline ofExternal:
    snapshot: (unit -> IReadOnlySet<'T>) -> aset<'T> * (unit -> unit)
      when 'T: equality

  /// <summary>Adaptively reduces the set with the given <see cref="AdaptiveReduction"/>.</summary>
  val inline reduce:
    reduction: AdaptiveReduction<'a, 's, 'v> -> set: aset<'a> -> aval<'v>
      when 'a: equality

  /// <summary>Maps every element, then reduces the mapped values.</summary>
  val inline reduceBy:
    reduction: AdaptiveReduction<'b, 's, 'v> ->
    mapping: ('a -> 'b) ->
    set: aset<'a> ->
      aval<'v>
      when 'a: equality

  /// <summary>Adaptively folds the set with <c>add</c>; every removal recomputes.</summary>
  val inline fold:
    add: ('s -> 'a -> 's) -> zero: 's -> set: aset<'a> -> aval<'s>
      when 'a: equality

  /// <summary>Adaptively folds the set with an invertible <c>subtract</c>.</summary>
  val inline foldGroup:
    add: ('s -> 'a -> 's) ->
    subtract: ('s -> 'a -> 's) ->
    zero: 's ->
    set: aset<'a> ->
      aval<'s>
      when 'a: equality

  /// <summary>Adaptively folds the set with a partially invertible <c>trySubtract</c>.</summary>
  val inline foldHalfGroup:
    add: ('s -> 'a -> 's) ->
    trySubtract: ('s -> 'a -> 's voption) ->
    zero: 's ->
    set: aset<'a> ->
      aval<'s>
      when 'a: equality

  /// <summary>Adaptively gets the number of elements (incremental).</summary>
  val inline count: set: aset<'T> -> aval<int> when 'T: equality
  /// <summary>Adaptively tests if the set is empty (incremental).</summary>
  val inline isEmpty: set: aset<'T> -> aval<bool> when 'T: equality

  /// <summary>Adaptively tests if the set contains the given element (per-element precise).</summary>
  val inline contains:
    value: 'T -> set: aset<'T> -> aval<bool> when 'T: equality

  /// <summary>Adaptively tests if any element satisfies the predicate.</summary>
  val inline exists:
    predicate: ('T -> bool) -> set: aset<'T> -> aval<bool> when 'T: equality

  /// <summary>Adaptively tests if every element satisfies the predicate.</summary>
  val inline forall:
    predicate: ('T -> bool) -> set: aset<'T> -> aval<bool> when 'T: equality

  /// <summary>Adaptively counts the elements that satisfy the predicate.</summary>
  val inline countBy:
    predicate: ('T -> bool) -> set: aset<'T> -> aval<int> when 'T: equality

  /// <summary>Adaptively reduces the set after mapping every element to an adaptive value.</summary>
  val inline reduceByA:
    reduction: AdaptiveReduction<'U, 's, 'v> ->
    mapping: ('T -> aval<'U>) ->
    set: aset<'T> ->
      aval<'v>
      when 'T: equality and 'U: equality

  /// <summary>Adaptively counts the elements whose predicate aval holds true.</summary>
  val inline countByA:
    predicate: ('T -> aval<bool>) -> set: aset<'T> -> aval<int>
      when 'T: equality

  /// <summary>Adaptively tests if any element's predicate aval holds true.</summary>
  val inline existsA:
    predicate: ('T -> aval<bool>) -> set: aset<'T> -> aval<bool>
      when 'T: equality

  /// <summary>Adaptively tests if every element's predicate aval holds true.</summary>
  val inline forallA:
    predicate: ('T -> aval<bool>) -> set: aset<'T> -> aval<bool>
      when 'T: equality

  /// <summary>Adaptively sums the avals mapped from the elements.</summary>
  val inline sumByA:
    mapping: ('T -> aval<'U>) -> set: aset<'T> -> aval<'U>
      when 'T: equality
      and 'U: (static member (+): 'U * 'U -> 'U)
      and 'U: (static member (-): 'U * 'U -> 'U)
      and 'U: (static member Zero: 'U)
      and ^U: equality

  /// <summary>Adaptively averages the avals mapped from the elements (needs DivideByInt, e.g. float).</summary>
  val inline averageByA:
    mapping: ('T -> aval<'U>) -> set: aset<'T> -> aval<'U>
      when 'U: (static member DivideByInt: 'U * int -> 'U)
      and 'U: (static member (+): 'U * 'U -> 'U)
      and 'U: (static member Zero: 'U)
      and 'T: equality
      and ^U: (static member (-): ^U * ^U -> ^U)
      and ^U: equality

  /// <summary>Adaptively sums the elements.</summary>
  val inline sum:
    set: aset<'T> -> aval<'T>
      when 'T: (static member (+): 'T * 'T -> 'T)
      and 'T: (static member (-): 'T * 'T -> 'T)
      and 'T: (static member Zero: 'T)
      and ^T: equality

  /// <summary>Adaptively sums the mapped elements.</summary>
  val inline sumBy:
    mapping: ('T -> 'U) -> set: aset<'T> -> aval<'U>
      when 'U: (static member (+): 'U * 'U -> 'U)
      and 'U: (static member (-): 'U * 'U -> 'U)
      and 'U: (static member Zero: 'U)
      and 'T: equality

  /// <summary>Adaptively averages the elements (needs DivideByInt, e.g. float).</summary>
  val inline average< ^T
    when ^T: equality
    and ^T: (static member DivideByInt: ^T * int -> ^T)
    and ^T: (static member (+): ^T * ^T -> ^T)
    and ^T: (static member (-): ^T * ^T -> ^T)
    and ^T: (static member Zero: ^T)> : set: aset< ^T > -> aval< ^T >

  /// <summary>Adaptively averages the mapped elements (needs DivideByInt, e.g. float).</summary>
  val inline averageBy<'T, ^U
    when 'T: equality
    and ^U: equality
    and ^U: (static member DivideByInt: ^U * int -> ^U)
    and ^U: (static member (+): ^U * ^U -> ^U)
    and ^U: (static member (-): ^U * ^U -> ^U)
    and ^U: (static member Zero: ^U)> :
    mapping: ('T -> ^U) -> set: aset<'T> -> aval< ^U >

  /// <summary>Adaptively gets the minimum element, or ValueNone when empty.</summary>
  val inline tryMin: set: aset<'T> -> aval<'T voption> when 'T: comparison
  /// <summary>Adaptively gets the maximum element, or ValueNone when empty.</summary>
  val inline tryMax: set: aset<'T> -> aval<'T voption> when 'T: comparison

  /// <summary>Adaptively gets the minimum of the avals mapped from the elements.</summary>
  val inline tryMinA:
    mapping: ('T -> aval<'U>) -> set: aset<'T> -> aval<'U voption>
      when 'U: comparison and 'T: equality

  /// <summary>Adaptively gets the maximum of the avals mapped from the elements.</summary>
  val inline tryMaxA:
    mapping: ('T -> aval<'U>) -> set: aset<'T> -> aval<'U voption>
      when 'U: comparison and 'T: equality

  /// <summary>A constant set with a single element.</summary>
  val inline single: value: 'T -> aset<'T>
  /// <summary>Materializes the set as an adaptive value (the retain boundary).</summary>
  val inline toAVal: set: aset<'T> -> aval<HashSet<'T>>
  /// <summary>Returns a transient view of the current state (do not retain).</summary>
  val inline getValue: set: aset<'T> -> IReadOnlySet<'T>
  /// <summary>Materializes the current state as an immutable copy (drains first).</summary>
  val inline force: set: aset<'T> -> HashSet<'T>

  /// <summary>The sorted set as a list, using the given comparison (stable).</summary>
  val inline sortWith:
    comparer: ('T -> 'T -> int) -> set: aset<'T> -> alist<'T> when 'T: equality

  /// <summary>The sorted set as a list, ascending (stable).</summary>
  val inline sort: set: aset<'T> -> alist<'T> when 'T: comparison
  /// <summary>The sorted set as a list, descending (stable).</summary>
  val inline sortDescending: set: aset<'T> -> alist<'T> when 'T: comparison

  /// <summary>The set sorted by the keys given by the projection (stable).</summary>
  val inline sortBy:
    f: ('T -> 'K) -> set: aset<'T> -> alist<'T>
      when 'K: comparison and 'T: equality

  /// <summary>The set sorted by the keys given by the projection, descending (stable).</summary>
  val inline sortByDescending:
    f: ('T -> 'K) -> set: aset<'T> -> alist<'T>
      when 'K: comparison and 'T: equality

  /// <summary>Materializes the F# <c>Set</c> counterpart (sorted, structural equality).</summary>
  val inline toSet: set: aset<'T> -> Set<'T> when 'T: comparison

/// <summary>Operations on changeable sets.</summary>
module CSet =
  /// <summary>An empty changeable set.</summary>
  val inline empty<'T> : cset<'T>
  /// <summary>A changeable set with the given items.</summary>
  val inline ofSeq: items: seq<'T> -> cset<'T>
  /// <summary>Adds an element. No-op when already present.</summary>
  val inline add: item: 'T -> set: cset<'T> -> unit
  /// <summary>Removes an element. No-op when absent.</summary>
  val inline remove: item: 'T -> set: cset<'T> -> unit
  /// <summary>Posts an add (the cval.Post handoff pattern).</summary>
  val inline postAdd: item: 'T -> set: cset<'T> -> unit
  /// <summary>Posts a remove.</summary>
  val inline postRemove: item: 'T -> set: cset<'T> -> unit
  /// <summary>Posts a full replace (supersedes the other ops of the same pending batch).</summary>
  val inline postSet: value: Set<'T> -> set: cset<'T> -> unit
  /// <summary>Replaces the whole set.</summary>
  val inline set: value: Set<'T> -> set: cset<'T> -> unit
  /// <summary>Replaces the whole set and returns whether the content changed.</summary>
  val inline updateTo: target: seq<'T> -> set: cset<'T> -> bool
  /// <summary>Applies a batch of set operations atomically.</summary>
  val perform: delta: SetDeltaBuilder<'T> -> set: cset<'T> -> unit
  /// <summary>Adds all the given elements (one atomic batch).</summary>
  val inline unionWith: other: seq<'T> -> set: cset<'T> -> unit
  /// <summary>Removes all the given elements (one atomic batch).</summary>
  val inline exceptWith: other: seq<'T> -> set: cset<'T> -> unit
  /// <summary>Keeps only the elements also present in <c>other</c> (one atomic batch).</summary>
  val inline intersectWith: other: seq<'T> -> set: cset<'T> -> unit
  /// <summary>Views the changeable set as an adaptive set.</summary>
  val inline value: set: cset<'T> -> aset<'T>
  /// <summary>Materializes the current state as an immutable snapshot.</summary>
  val inline force: set: cset<'T> -> HashSet<'T>
  /// <summary>Materializes the F# <c>Set</c> counterpart.</summary>
  val inline toSet: set: cset<'T> -> Set<'T> when 'T: comparison

/// <summary>Operations on adaptive maps.</summary>
module AMap =
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
    predicate: ('K -> 'V -> aval<bool>) ->
    mapValue: amap<'K, 'V> ->
      amap<'K, 'V>
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
    reduction: AdaptiveReduction<'a, 's, 'v> ->
    mapValue: amap<'k, 'a> ->
      aval<'v>
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
    add: ('s -> 'k -> 'v -> 's) ->
    zero: 's ->
    mapValue: amap<'k, 'v> ->
      aval<'s>
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
    mapping: ('K -> 'V -> aval<'U>) ->
    mapValue: amap<'K, 'V> ->
      aval<'U voption>
      when 'K: equality and 'U: comparison

  /// <summary>Adaptively gets the maximum of the avals mapped from the entries.</summary>
  val inline tryMaxA:
    mapping: ('K -> 'V -> aval<'U>) ->
    mapValue: amap<'K, 'V> ->
      aval<'U voption>
      when 'K: equality and 'U: comparison

  /// <summary>Adaptively looks up the key (per-key precise, read-time gate).</summary>
  val inline tryFind:
    key: 'K -> mapValue: amap<'K, 'V> -> aval<'V voption> when 'K: equality

  /// <summary>Adaptively looks up the key (throws KeyNotFoundException when absent at read time).</summary>
  val inline find:
    key: 'K -> mapValue: amap<'K, 'V> -> aval<'V> when 'K: equality

  /// <summary>A constant map with a single entry.</summary>
  val inline single: key: 'K -> value: 'V -> amap<'K, 'V> when 'K: equality

  /// <summary>Materializes the map as an adaptive value (the retain boundary).</summary>
  val inline toAVal:
    mapValue: amap<'K, 'V> -> aval<Dictionary<'K, 'V>> when 'K: equality

  /// <summary>Returns a transient view of the current state (do not retain).</summary>
  val inline getValue:
    mapValue: amap<'K, 'V> -> IReadOnlyDictionary<'K, 'V> when 'K: equality

  /// <summary>Materializes the current state as an immutable copy (drains first).</summary>
  val inline force:
    mapValue: amap<'K, 'V> -> Dictionary<'K, 'V> when 'K: equality

  /// <summary>Materializes the F# <c>Map</c> counterpart (sorted, structural equality).</summary>
  val inline toMap: mapValue: amap<'K, 'V> -> Map<'K, 'V> when 'K: comparison

/// <summary>Operations on changeable maps.</summary>
module CMap =
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
  val inline force:
    mapValue: cmap<'K, 'V> -> Dictionary<'K, 'V> when 'K: equality

  /// <summary>Materializes the F# <c>Map</c> counterpart.</summary>
  val inline toMap: mapValue: cmap<'K, 'V> -> Map<'K, 'V> when 'K: comparison

/// <summary>Operations on adaptive lists.</summary>
module AList =
  /// <summary>An empty adaptive list (FDA <c>AList.empty</c> parity).</summary>
  val empty<'T> : alist<'T>
  /// <summary>An adaptive list over fixed, immutable items.</summary>
  val inline ofSeq: items: seq<'T> -> alist<'T>
  /// <summary>An adaptive list over a fixed array.</summary>
  val inline ofArray: items: 'T[] -> alist<'T>
  /// <summary>An adaptive list over a fixed list.</summary>
  val inline ofList: items: 'T list -> alist<'T>
  /// <summary>An adaptive list over a fixed ResizeArray.</summary>
  val inline ofResizeArray: items: ResizeArray<'T> -> alist<'T>
  /// <summary>A constant list with a single element.</summary>
  val inline single: value: 'T -> alist<'T>
  /// <summary>An adaptive list whose content is computed lazily, once, at first read.</summary>
  val inline constant: create: (unit -> ResizeArray<'T>) -> alist<'T>
  /// <summary>Alias of <see cref="constant"/>.</summary>
  val inline delay: create: (unit -> ResizeArray<'T>) -> alist<'T>
  /// <summary>Maps every element of the list.</summary>
  val inline map: f: ('T -> 'U) -> list: alist<'T> -> alist<'U>
  /// <summary>Maps every element, keeping only the ones the mapping returns a value for (option form).</summary>
  val inline choose: f: ('T -> 'U option) -> list: alist<'T> -> alist<'U>
  /// <summary>Maps every element, keeping only the ones the mapping returns a value for (voption form).</summary>
  val inline chooseV: f: ('T -> 'U voption) -> list: alist<'T> -> alist<'U>
  /// <summary>Keeps the elements that satisfy the predicate.</summary>
  val inline filter: predicate: ('T -> bool) -> list: alist<'T> -> alist<'T>
  /// <summary>Maps every element, passing the input position to the mapping.</summary>
  val inline mapi: f: (int -> 'T -> 'U) -> list: alist<'T> -> alist<'U>

  /// <summary>Keeps the entries whose index-aware mapping returns a value (option form).</summary>
  val inline choosei:
    f: (int -> 'T -> 'U option) -> list: alist<'T> -> alist<'U>

  /// <summary>Keeps the entries whose index-aware mapping returns a value (voption form).</summary>
  val inline chooseiV:
    f: (int -> 'T -> 'U voption) -> list: alist<'T> -> alist<'U>

  /// <summary>Keeps the elements whose index-aware predicate holds.</summary>
  val inline filteri:
    predicate: (int -> 'T -> bool) -> list: alist<'T> -> alist<'T>

  /// <summary>An adaptive list of the elements paired with their input positions (struct pairs).</summary>
  val inline indexed: list: alist<'T> -> alist<struct (int * 'T)>
  /// <summary>Adaptively maps every element of the list to an adaptive value.</summary>
  val inline mapA: mapping: ('T -> aval<'U>) -> list: alist<'T> -> alist<'U>

  /// <summary>Adaptively maps every element to an adaptive value, keeping only the elements whose aval holds Some.</summary>
  val inline chooseA:
    mapping: ('T -> aval<'U option>) -> list: alist<'T> -> alist<'U>

  /// <summary>The voption counterpart of chooseA (the no-allocation path).</summary>
  val inline chooseAV:
    mapping: ('T -> aval<'U voption>) -> list: alist<'T> -> alist<'U>

  /// <summary>Adaptively keeps the elements whose predicate aval holds true.</summary>
  val inline filterA:
    predicate: ('T -> aval<bool>) -> list: alist<'T> -> alist<'T>

  /// <summary>Adaptively maps every element to an adaptive value, passing the input position.</summary>
  val inline mapiA:
    mapping: (int -> 'T -> aval<'U>) -> list: alist<'T> -> alist<'U>

  /// <summary>Adaptively maps with the input position, keeping Some results (option form).</summary>
  val inline chooseiA:
    mapping: (int -> 'T -> aval<'U option>) -> list: alist<'T> -> alist<'U>

  /// <summary>Adaptively maps with the input position, keeping Some results (voption form).</summary>
  val inline chooseiAV:
    mapping: (int -> 'T -> aval<'U voption>) -> list: alist<'T> -> alist<'U>

  /// <summary>Adaptively keeps the elements whose predicate aval holds true, passing the input position.</summary>
  val inline filteriA:
    predicate: (int -> 'T -> aval<bool>) -> list: alist<'T> -> alist<'T>

  /// <summary>The concatenation of two lists.</summary>
  val inline append: left: alist<'T> -> right: alist<'T> -> alist<'T>
  /// <summary>Returns a transient view of the current state (do not retain).</summary>
  val inline getValue: list: alist<'T> -> IReadOnlyList<'T>
  /// <summary>Materializes the current state as a fresh array (drains first).</summary>
  val inline force: list: alist<'T> -> 'T[]
  /// <summary>Materializes the F# <c>list</c> counterpart.</summary>
  val inline toList: list: alist<'T> -> 'T list
  /// <summary>Materializes the array counterpart.</summary>
  val inline toArray: list: alist<'T> -> 'T[]
  /// <summary>Adaptively gets the number of elements (incremental).</summary>
  val inline count: list: alist<'T> -> aval<int>
  /// <summary>Adaptively tests if the list is empty (incremental).</summary>
  val inline isEmpty: list: alist<'T> -> aval<bool>

  /// <summary>Adaptively reduces the list (FDA <c>AList.reduce</c> parity).</summary>
  val inline reduce:
    reduction: AdaptiveReduction<'a, 's, 'v> -> list: alist<'a> -> aval<'v>

  /// <summary>Maps every element, then reduces the mapped values.</summary>
  val inline reduceBy:
    reduction: AdaptiveReduction<'b, 's, 'v> ->
    mapping: ('a -> 'b) ->
    list: alist<'a> ->
      aval<'v>

  /// <summary>Adaptively folds the list with <c>add</c>; every removal recomputes.</summary>
  val inline fold:
    add: ('s -> 'a -> 's) -> zero: 's -> list: alist<'a> -> aval<'s>

  /// <summary>Adaptively folds the list with an invertible <c>subtract</c>.</summary>
  val inline foldGroup:
    add: ('s -> 'a -> 's) ->
    subtract: ('s -> 'a -> 's) ->
    zero: 's ->
    list: alist<'a> ->
      aval<'s>

  /// <summary>Adaptively folds the list with a partially invertible <c>trySubtract</c>.</summary>
  val inline foldHalfGroup:
    add: ('s -> 'a -> 's) ->
    trySubtract: ('s -> 'a -> 's voption) ->
    zero: 's ->
    list: alist<'a> ->
      aval<'s>

  /// <summary>Adaptively tests if any element satisfies the predicate.</summary>
  val inline exists: predicate: ('T -> bool) -> list: alist<'T> -> aval<bool>
  /// <summary>Adaptively tests if every element satisfies the predicate.</summary>
  val inline forall: predicate: ('T -> bool) -> list: alist<'T> -> aval<bool>
  /// <summary>Adaptively counts the elements that satisfy the predicate.</summary>
  val inline countBy: predicate: ('T -> bool) -> list: alist<'T> -> aval<int>
  /// <summary>Adaptively gets the minimum element, or ValueNone when empty.</summary>
  val inline tryMin: list: alist<'T> -> aval<'T voption> when 'T: comparison
  /// <summary>Adaptively gets the maximum element, or ValueNone when empty.</summary>
  val inline tryMax: list: alist<'T> -> aval<'T voption> when 'T: comparison

  /// <summary>Adaptively sums the elements (needs an additive numeric type).</summary>
  val inline sum:
    list: alist<'T> -> aval<'T>
      when 'T: (static member (+): 'T * 'T -> 'T)
      and 'T: (static member (-): 'T * 'T -> 'T)
      and 'T: (static member Zero: 'T)

  /// <summary>Adaptively sums the mapped elements.</summary>
  val inline sumBy:
    mapping: ('T -> 'U) -> list: alist<'T> -> aval<'U>
      when 'U: (static member (+): 'U * 'U -> 'U)
      and 'U: (static member (-): 'U * 'U -> 'U)
      and 'U: (static member Zero: 'U)

  /// <summary>Adaptively averages the elements (needs DivideByInt, e.g. float).</summary>
  val inline average:
    list: alist< ^T > -> aval< ^T >
      when ^T: comparison
      and ^T: (static member DivideByInt: ^T * int -> ^T)
      and ^T: (static member (+): ^T * ^T -> ^T)
      and ^T: (static member (-): ^T * ^T -> ^T)
      and ^T: (static member Zero: ^T)

  /// <summary>Adaptively averages the mapped elements (needs DivideByInt, e.g. float).</summary>
  val inline averageBy:
    mapping: ('T -> ^U) -> list: alist<'T> -> aval< ^U >
      when ^U: comparison
      and ^U: (static member DivideByInt: ^U * int -> ^U)
      and ^U: (static member (+): ^U * ^U -> ^U)
      and ^U: (static member (-): ^U * ^U -> ^U)
      and ^U: (static member Zero: ^U)

  /// <summary>Adaptively reduces the list after mapping every element to an adaptive value.</summary>
  val inline reduceByA:
    reduction: AdaptiveReduction<'U, 's, 'v> ->
    mapping: ('T -> aval<'U>) ->
    list: alist<'T> ->
      aval<'v>

  /// <summary>Adaptively counts the elements whose predicate aval holds true.</summary>
  val inline countByA:
    predicate: ('T -> aval<bool>) -> list: alist<'T> -> aval<int>

  /// <summary>Adaptively tests if any element's predicate aval holds true.</summary>
  val inline existsA:
    predicate: ('T -> aval<bool>) -> list: alist<'T> -> aval<bool>

  /// <summary>Adaptively tests if every element's predicate aval holds true.</summary>
  val inline forallA:
    predicate: ('T -> aval<bool>) -> list: alist<'T> -> aval<bool>

  /// <summary>Adaptively sums the avals mapped from the elements.</summary>
  val inline sumByA:
    mapping: ('T -> aval<'U>) -> list: alist<'T> -> aval<'U>
      when 'U: (static member (+): 'U * 'U -> 'U)
      and 'U: (static member (-): 'U * 'U -> 'U)
      and 'U: (static member Zero: 'U)

  /// <summary>Adaptively averages the avals mapped from the elements (needs DivideByInt, e.g. float).</summary>
  val inline averageByA:
    mapping: ('T -> aval<'U>) -> list: alist<'T> -> aval<'U>
      when 'U: (static member DivideByInt: 'U * int -> 'U)
      and 'U: (static member (+): 'U * 'U -> 'U)
      and 'U: (static member (-): 'U * 'U -> 'U)
      and 'U: (static member Zero: 'U)

  /// <summary>Adaptively gets the minimum of the avals mapped from the elements.</summary>
  val inline tryMinA:
    mapping: ('T -> aval<'U>) -> list: alist<'T> -> aval<'U voption>
      when 'U: comparison

  /// <summary>Adaptively gets the maximum of the avals mapped from the elements.</summary>
  val inline tryMaxA:
    mapping: ('T -> aval<'U>) -> list: alist<'T> -> aval<'U voption>
      when 'U: comparison

  /// <summary>An adaptive list over an adaptive value of a sequence (the rebuild boundary).</summary>
  val inline ofAVal<'T, 'S when 'S :> seq<'T>> : value: aval<'S> -> alist<'T>
  /// <summary>An adaptive list generated from a count and a generator.</summary>
  val inline init: f: (int -> 'T) -> count: aval<int> -> alist<'T>

  /// <summary>An adaptive numeric range as a list; the bounds are inclusive.</summary>
  val inline range:
    min: aval< ^T > -> max: aval< ^T > -> alist< ^T >
      when ^T: comparison
      and ^T: (static member (+): ^T * ^T -> ^T)
      and ^T: (static member One: ^T)

  /// <summary>Adaptively looks up the element at the given position (per-position precise).</summary>
  val inline tryAt: index: int -> list: alist<'T> -> aval<'T voption>
  /// <summary>Alias of <see cref="tryAt"/>.</summary>
  val inline tryGet: index: int -> list: alist<'T> -> aval<'T voption>
  /// <summary>Adaptively gets the first element, or ValueNone when empty.</summary>
  val inline tryFirst: list: alist<'T> -> aval<'T voption>
  /// <summary>Adaptively gets the last element, or ValueNone when empty.</summary>
  val inline tryLast: list: alist<'T> -> aval<'T voption>
  /// <summary>Materializes the list as an adaptive value (the retain boundary).</summary>
  val inline toAVal: list: alist<'T> -> aval<'T[]>
  /// <summary>An adaptive set of the list's elements, deduplicated (last-occurrence removal).</summary>
  val inline toASet: list: alist<'T> -> aset<'T> when 'T: equality

  /// <summary>An adaptive set of the elements paired with their input positions.</summary>
  val inline toIndexedASet:
    list: alist<'T> -> aset<struct (int * 'T)> when 'T: equality

  /// <summary>An adaptive list of a set's elements (set iteration order).</summary>
  val inline ofASet: set: aset<'T> -> alist<'T> when 'T: equality
  /// <summary>Reverses the list (poll node).</summary>
  val inline rev: list: alist<'T> -> alist<'T>
  /// <summary>Adaptively maps over the given value and returns the resulting list (rebuild-on-change).</summary>
  val inline bind: mapping: ('T -> alist<'U>) -> value: aval<'T> -> alist<'U>

  /// <summary>Adaptively maps over the two values and returns the resulting list.</summary>
  val inline bind2:
    mapping: ('A -> 'B -> alist<'C>) -> a: aval<'A> -> b: aval<'B> -> alist<'C>

  /// <summary>Adaptively maps over the three values and returns the resulting list.</summary>
  val inline bind3:
    mapping: ('A -> 'B -> 'C -> alist<'D>) ->
    a: aval<'A> ->
    b: aval<'B> ->
    c: aval<'C> ->
      alist<'D>

  /// <summary>Concatenates a fixed sequence of lists (poll node).</summary>
  val inline concat: lists: (#seq<alist<'T>>) -> alist<'T>

  /// <summary>The window [offset, offset + count) of the list (adaptive bounds).</summary>
  val inline subA:
    offset: aval<int> -> count: aval<int> -> list: alist<'T> -> alist<'T>

  /// <summary>The window [offset, offset + count) of the list (static bounds).</summary>
  val inline sub: offset: int -> count: int -> list: alist<'T> -> alist<'T>
  /// <summary>The first <c>count</c> elements (adaptive bound).</summary>
  val inline takeA: count: aval<int> -> list: alist<'T> -> alist<'T>
  /// <summary>The first <c>count</c> elements (static bound).</summary>
  val inline take: count: int -> list: alist<'T> -> alist<'T>
  /// <summary>All elements after the first <c>count</c> (adaptive bound).</summary>
  val inline skipA: count: aval<int> -> list: alist<'T> -> alist<'T>
  /// <summary>All elements after the first <c>count</c> (static bound).</summary>
  val inline skip: count: int -> list: alist<'T> -> alist<'T>

  /// <summary>Sorts the list with the given comparison (stable, poll node).</summary>
  val inline sortWith:
    comparer: ('T -> 'T -> int) -> list: alist<'T> -> alist<'T>

  /// <summary>Sorts the list ascending (stable).</summary>
  val inline sort: list: alist<'T> -> alist<'T> when 'T: comparison
  /// <summary>Sorts the list descending (stable).</summary>
  val inline sortDescending: list: alist<'T> -> alist<'T> when 'T: comparison

  /// <summary>Sorts the list by the keys given by the projection (stable).</summary>
  val inline sortBy:
    f: ('T -> 'K) -> list: alist<'T> -> alist<'T> when 'K: comparison

  /// <summary>Sorts the list by the keys, passing the input position to the projection (stable).</summary>
  val inline sortByi:
    f: (int -> 'T -> 'K) -> list: alist<'T> -> alist<'T> when 'K: comparison

  /// <summary>Sorts the list by the keys, descending (stable).</summary>
  val inline sortByDescending:
    f: ('T -> 'K) -> list: alist<'T> -> alist<'T> when 'K: comparison

  /// <summary>Sorts the list by the keys, descending, index-aware (stable).</summary>
  val inline sortByDescendingi:
    f: (int -> 'T -> 'K) -> list: alist<'T> -> alist<'T> when 'K: comparison

  /// <summary>An adaptive list of adjacent pairs (struct pairs).</summary>
  val inline pairwise: list: alist<'T> -> alist<struct ('T * 'T)>
  /// <summary>An adaptive list of adjacent pairs, cyclically closed (struct pairs).</summary>
  val inline pairwiseCyclic: list: alist<'T> -> alist<struct ('T * 'T)>

  /// <summary>Maps every element, disposing the mapped value when the element leaves its position.</summary>
  val inline mapUse:
    mapping: ('T -> 'W) -> list: alist<'T> -> IDisposable * alist<'W>
      when 'W: equality and 'W :> IDisposable

  /// <summary>Maps every element with the input position, disposing when the element leaves its position.</summary>
  val inline mapUsei:
    mapping: (int -> 'T -> 'W) -> list: alist<'T> -> IDisposable * alist<'W>
      when 'W: equality and 'W :> IDisposable

  /// <summary>Creates an adaptive list from an external snapshot function and an invalidate handle.</summary>
  val inline ofExternal:
    snapshot: (unit -> IReadOnlyList<'T>) -> alist<'T> * (unit -> unit)
      when 'T: equality

  /// <summary>An adaptive list driven by a compute function (pull model, positional ops).</summary>
  val inline custom:
    compute: (IReadOnlyList<'T> -> ListDeltaBuilder<'T> -> unit) -> alist<'T>
      when 'T: equality

/// <summary>Operations on changeable lists.</summary>
module CList =
  /// <summary>An empty changeable list.</summary>
  val inline empty<'T> : clist<'T>
  /// <summary>A changeable list with the given items.</summary>
  val inline ofSeq: items: seq<'T> -> clist<'T>
  /// <summary>A changeable list with the given items.</summary>
  val inline ofArray: items: 'T[] -> clist<'T>
  /// <summary>A changeable list with the given items.</summary>
  val inline ofList: items: 'T list -> clist<'T>
  /// <summary>Appends an element at the end of the list.</summary>
  val inline append: value: 'T -> list: clist<'T> -> unit
  /// <summary>Inserts an element at the start of the list.</summary>
  val inline prepend: value: 'T -> list: clist<'T> -> unit
  /// <summary>Inserts an element before the element currently at the position.</summary>
  val inline insertAt: position: int -> value: 'T -> list: clist<'T> -> unit
  /// <summary>Removes the element currently at the position.</summary>
  val inline removeAt: position: int -> list: clist<'T> -> unit
  /// <summary>Replaces the element currently at the position.</summary>
  val inline updateAt: position: int -> value: 'T -> list: clist<'T> -> unit
  /// <summary>Removes the first occurrence of the value. No-op when absent.</summary>
  val inline remove: value: 'T -> list: clist<'T> -> unit
  /// <summary>Posts an append (the cval.Post handoff pattern).</summary>
  val inline postAppend: value: 'T -> list: clist<'T> -> unit
  /// <summary>Posts an insert at the start.</summary>
  val inline postPrepend: value: 'T -> list: clist<'T> -> unit
  /// <summary>Posts an insert before the element currently at the position.</summary>
  val inline postInsertAt: position: int -> value: 'T -> list: clist<'T> -> unit
  /// <summary>Posts a remove at the position.</summary>
  val inline postRemoveAt: position: int -> list: clist<'T> -> unit
  /// <summary>Posts a replace at the position.</summary>
  val inline postUpdateAt: position: int -> value: 'T -> list: clist<'T> -> unit
  /// <summary>Posts a remove of the first occurrence of the value.</summary>
  val inline postRemove: value: 'T -> list: clist<'T> -> unit
  /// <summary>Posts a clear.</summary>
  val inline postClear: list: clist<'T> -> unit
  /// <summary>Posts a full replace (supersedes the other ops of the same pending batch).</summary>
  val inline postSet: values: seq<'T> -> list: clist<'T> -> unit
  /// <summary>Removes all elements.</summary>
  val inline clear: list: clist<'T> -> unit
  /// <summary>Replaces the whole list (last-wins over the batch inside a transaction).</summary>
  val inline set: values: seq<'T> -> list: clist<'T> -> unit
  /// <summary>Replaces the whole list and returns whether the content changed.</summary>
  val inline updateTo: target: 'T[] -> list: clist<'T> -> bool
  /// <summary>Applies a batch of positional list operations atomically.</summary>
  val perform: delta: ListDeltaBuilder<'T> -> list: clist<'T> -> unit
  /// <summary>Appends all the given elements (one atomic batch).</summary>
  val inline addRange: items: seq<'T> -> list: clist<'T> -> unit
  /// <summary>Views the changeable list as an adaptive list.</summary>
  val inline value: list: clist<'T> -> alist<'T>
  /// <summary>Materializes the current state as an immutable array snapshot.</summary>
  val inline force: list: clist<'T> -> 'T[]
  /// <summary>Materializes the F# <c>list</c> counterpart.</summary>
  val inline toList: list: clist<'T> -> 'T list

/// <summary>
/// Slicing for adaptive lists: <c>list.[a..b]</c>. The bounds are clamped;
/// the slice is the window [a, b] inclusive.
/// </summary>
[<AutoOpen>]
module AListSliceExtensions =
  type IAdaptiveList<'T> with
    /// <summary>Slicing: <c>list.[a..b]</c>. The bounds are clamped.</summary>
    member GetSlice: start: int option * finish: int option -> alist<'T>
