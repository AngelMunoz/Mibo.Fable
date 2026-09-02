/// <summary>Operations on adaptive sets.</summary>
module Mibo.Fable.Adaptive.ASet

open System
open System.Collections.Generic

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
  f: ('T -> 'U) -> set: aset<'T> -> aset<'U> when 'T: equality and 'U: equality

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
val inline xor: left: aset<'T> -> right: aset<'T> -> aset<'T> when 'T: equality

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
val inline ofReader: reader: (unit -> HashSet<'T>) -> aset<'T> when 'T: equality

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
val inline contains: value: 'T -> set: aset<'T> -> aval<bool> when 'T: equality

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
  predicate: ('T -> aval<bool>) -> set: aset<'T> -> aval<int> when 'T: equality

/// <summary>Adaptively tests if any element's predicate aval holds true.</summary>
val inline existsA:
  predicate: ('T -> aval<bool>) -> set: aset<'T> -> aval<bool> when 'T: equality

/// <summary>Adaptively tests if every element's predicate aval holds true.</summary>
val inline forallA:
  predicate: ('T -> aval<bool>) -> set: aset<'T> -> aval<bool> when 'T: equality

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
