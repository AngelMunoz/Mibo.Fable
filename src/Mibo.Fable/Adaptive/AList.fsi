/// <summary>Operations on adaptive lists.</summary>
module Mibo.Fable.Adaptive.AList

open System
open System.Collections.Generic

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
val inline choosei: f: (int -> 'T -> 'U option) -> list: alist<'T> -> alist<'U>

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
val inline sortWith: comparer: ('T -> 'T -> int) -> list: alist<'T> -> alist<'T>

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
