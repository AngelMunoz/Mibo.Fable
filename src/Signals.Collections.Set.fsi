namespace Mibo.Signals

// ─────────────────────────────────────────────────────────────────────────────
// Signals.Collections.Set — the ASet family: a structure-only adaptive set
// over @preact/signals-core. No per-element value cells: every read derives
// from the structural version channel. CSet is the changeable write side.
// The public surface mirrors Mibo.Adaptive's ASet/CSet modules.
// ─────────────────────────────────────────────────────────────────────────────

/// One changeable set: writes land immediately (or at the next boundary for
/// the posted intents) and the <see cref="ASet"/> view tracks them.
type CSet<'T when 'T: comparison> =

  internal new: items: seq<'T> -> CSet<'T>

  /// The adaptive view of the set.
  member Value: ASet<'T>

  /// Applies the pending boundary intents now.
  member FlushPosts: unit -> unit

  /// Adds an element. No-op when already present.
  member Add: item: 'T -> unit

  /// Removes an element. No-op when absent.
  member Remove: item: 'T -> unit

  /// Replaces the whole content with the given items.
  member Set: items: seq<'T> -> unit

  /// Replaces the content only when it differs; true when it changed.
  member UpdateTo: items: seq<'T> -> bool

  member PostAdd: item: 'T -> unit
  member PostRemove: item: 'T -> unit
  member PostSet: items: seq<'T> -> unit

[<RequireQualifiedAccess>]
module CSet =

  /// An empty changeable set.
  val empty<'T when 'T: comparison> : CSet<'T>

  /// A changeable set with the given initial content.
  val ofSeq: items: seq<'T> -> CSet<'T>

  /// Adds an element. No-op when already present.
  val add: item: 'T -> set: CSet<'T> -> unit

  /// Removes an element. No-op when absent.
  val remove: item: 'T -> set: CSet<'T> -> unit

  /// Replaces the whole content with the given set.
  val set: value: Set<'T> -> set: CSet<'T> -> unit

  /// Replaces the content only when it differs; true when it changed.
  val updateTo: items: seq<'T> -> set: CSet<'T> -> bool

  /// Adds all the given elements in one atomic batch
  /// (Mibo.Adaptive <c>CSet.unionWith</c> parity).
  val unionWith: other: seq<'T> -> set: CSet<'T> -> unit

  /// Removes all the given elements in one atomic batch
  /// (Mibo.Adaptive <c>CSet.exceptWith</c> parity).
  val exceptWith: other: seq<'T> -> set: CSet<'T> -> unit

  /// Reduces the content to its intersection with the given elements in
  /// one atomic batch (Mibo.Adaptive <c>CSet.intersectWith</c> parity).
  val intersectWith: other: seq<'T> -> set: CSet<'T> -> unit

  /// <summary>
  /// Applies a batch of set operations atomically: one net change at the
  /// end of the batch (Mibo.Adaptive <c>CSet.perform</c> parity). Adding
  /// and removing the same element within the batch cancels.
  /// </summary>
  val perform: delta: SetDeltaBuilder<'T> -> set: CSet<'T> -> unit

  val postAdd: item: 'T -> set: CSet<'T> -> unit
  val postRemove: item: 'T -> set: CSet<'T> -> unit
  val postSet: items: seq<'T> -> set: CSet<'T> -> unit
  val value: set: CSet<'T> -> ASet<'T>

  /// Materializes the current content; raises inside a batch.
  val force: set: CSet<'T> -> Set<'T>

  /// Materializes into a plain set — alias of <c>force</c>.
  val toSet: set: CSet<'T> -> Set<'T>

[<RequireQualifiedAccess>]
module ASet =

  /// The empty set; sealed — no write path reaches its content.
  val empty<'T when 'T: comparison> : ASet<'T>

  /// A set built once from <c>create</c> on the first read, then cached.
  val constant: create: (unit -> Set<'T>) -> ASet<'T>

  /// <see cref="constant"/> under the delayed-build name.
  val delay: create: (unit -> Set<'T>) -> ASet<'T>

  /// A set over prebuilt content; version is pinned at zero.
  val ofCells: cells: Set<'T> -> ASet<'T>

  val ofSeq: items: seq<'T> -> ASet<'T>
  val ofArray: items: 'T array -> ASet<'T>
  val ofList: items: 'T list -> ASet<'T>
  val ofHashSet: items: System.Collections.Generic.HashSet<'T> -> ASet<'T>
  val single: item: 'T -> ASet<'T>

  /// Inclusive integer range <c>first..last</c>.
  val range: first: int -> last: int -> ASet<int>

  /// A set tracking an aval of snapshots.
  val ofAVal: value: AVal<seq<'T>> -> ASet<'T>

  /// A set over external world data: <c>snapshot</c> runs at most once per
  /// <c>invalidate</c> call, on the next read.
  val ofExternal: snapshot: (unit -> Set<'T>) -> ASet<'T> * (unit -> unit)

  /// A set whose content is a plain function of the read
  /// (Mibo.Adaptive <c>ASet.ofReader</c> parity, pull model).
  val ofReader: reader: (unit -> Set<'T>) -> ASet<'T>

  /// <summary>
  /// A pull-model compute (Mibo.Adaptive <c>ASet.custom</c> parity): the
  /// compute receives the current content and a delta builder; it appends
  /// the operations that describe the change since its previous call (for
  /// example, by consuming its own event queue) and the set applies them.
  /// The compute re-runs on every read.
  /// </summary>
  val custom: compute: (Set<'T> -> SetDeltaBuilder<'T> -> unit) -> ASet<'T>

  /// Materializes the current content; raises inside a batch.
  val force: set: ASet<'T> -> Set<'T>

  val getValue: set: ASet<'T> -> Set<'T>
  val toAVal: set: ASet<'T> -> AVal<Set<'T>>
  val toSet: set: ASet<'T> -> Set<'T>
  val isEmpty: set: ASet<'T> -> AVal<bool>
  val count: set: ASet<'T> -> AVal<int>
  val contains: item: 'T -> set: ASet<'T> -> AVal<bool>
  val exists: predicate: ('T -> bool) -> set: ASet<'T> -> AVal<bool>
  val existsA: predicate: ('T -> AVal<bool>) -> set: ASet<'T> -> AVal<bool>
  val forall: predicate: ('T -> bool) -> set: ASet<'T> -> AVal<bool>
  val forallA: predicate: ('T -> AVal<bool>) -> set: ASet<'T> -> AVal<bool>
  val countBy: predicate: ('T -> bool) -> set: ASet<'T> -> AVal<int>
  val countByA: predicate: ('T -> AVal<bool>) -> set: ASet<'T> -> AVal<int>

  /// Average of the elements. Empty sets average to 0.0.
  val average: set: ASet<float> -> AVal<float>
  val averageBy: by: ('T -> float) -> set: ASet<'T> -> AVal<float>
  val averageByA: by: ('T -> AVal<float>) -> set: ASet<'T> -> AVal<float>

  /// Sum of the elements. Empty sets sum to 0.0.
  val sum: set: ASet<float> -> AVal<float>
  val sumBy: by: ('T -> float) -> set: ASet<'T> -> AVal<float>
  val sumByA: by: ('T -> AVal<float>) -> set: ASet<'T> -> AVal<float>

  val tryMax: set: ASet<'T> -> AVal<'T voption> when 'T: comparison
  val tryMin: set: ASet<'T> -> AVal<'T voption> when 'T: comparison

  val tryMaxA:
    by: ('T -> AVal<'M>) -> set: ASet<'T> -> AVal<'M voption>
      when 'M: comparison

  val tryMinA:
    by: ('T -> AVal<'M>) -> set: ASet<'T> -> AVal<'M voption>
      when 'M: comparison

  /// Reduces the elements with <c>op</c>; none when empty.
  val reduce: op: ('T -> 'T -> 'T) -> set: ASet<'T> -> AVal<'T voption>

  val reduceBy:
    by: ('T -> 'M) -> op: ('M -> 'M -> 'M) -> set: ASet<'T> -> AVal<'M voption>

  val reduceByA:
    by: ('T -> AVal<'M>) ->
    op: ('M -> 'M -> 'M) ->
    set: ASet<'T> ->
      AVal<'M voption>

  val fold: add: ('s -> 'T -> 's) -> zero: 's -> set: ASet<'T> -> AVal<'s>

  val foldGroup:
    add: ('s -> 'T -> 's) ->
    subtract: ('s -> 'T -> 's) ->
    zero: 's ->
    set: ASet<'T> ->
      AVal<'s>

  val foldHalfGroup:
    add: ('s -> 'T -> 's) ->
    trySubtract: ('s -> 'T -> 's voption) ->
    zero: 's ->
    set: ASet<'T> ->
      AVal<'s>

  /// Derives a map keyed by the elements: each element maps to
  /// <c>mapping element</c>. The derived value cells recompute per element.
  val map: mapping: ('T -> 'V) -> set: ASet<'T> -> AMap<'T, 'V>

  /// Map where the mapping returns an already-tracked view per element.
  val mapA: mapping: ('T -> AVal<'V>) -> set: ASet<'T> -> AMap<'T, 'V>

  /// A map over the elements where every value is the given pinned view.
  val mapTo: value: AVal<'V> -> set: ASet<'T> -> AMap<'T, 'V>

  /// Flattens: every element maps to a whole set; the result is the union.
  val bind: binder: ('T -> ASet<'M>) -> set: ASet<'T> -> ASet<'M>

  /// Keywise flattening over two sets of one element type: where both hold
  /// the element the binder produces the inner set; the union is the result.
  val bind2:
    binder: ('T -> 'T -> ASet<'M>) ->
    set1: ASet<'T> ->
    set2: ASet<'T> ->
      ASet<'M>

  /// Keywise flattening over three sets — see <c>bind2</c>.
  val bind3:
    binder: ('T -> 'T -> 'T -> ASet<'M>) ->
    set1: ASet<'T> ->
    set2: ASet<'T> ->
    set3: ASet<'T> ->
      ASet<'M>

  /// Union of the sets produced per element (each mapping returns a set).
  val collect: binder: ('T -> ASet<'M>) -> set: ASet<'T> -> ASet<'M>

  /// Union of the sequences produced per element.
  val collect': binder: ('T -> seq<'M>) -> set: ASet<'T> -> ASet<'M>

  val filter: predicate: ('T -> bool) -> set: ASet<'T> -> ASet<'T>
  val filterA: predicate: ('T -> AVal<bool>) -> set: ASet<'T> -> ASet<'T>
  val choose: chooser: ('T -> 'M option) -> set: ASet<'T> -> ASet<'M>
  val chooseA: chooser: ('T -> AVal<'M option>) -> set: ASet<'T> -> ASet<'M>
  val chooseAV: chooser: ('T -> AVal<'M option>) -> set: ASet<'T> -> ASet<'M>
  val chooseV: chooser: ('T -> 'M option) -> set: ASet<'T> -> ASet<'M>

  /// Elements of <c>set</c> that do not occur in <c>other</c>.
  val difference: set: ASet<'T> -> other: ASet<'T> -> ASet<'T>

  /// <see cref="difference"/> under the HashSet name.
  val exceptWith: set: ASet<'T> -> other: ASet<'T> -> ASet<'T>

  /// Elements present in both sets.
  val intersect: set: ASet<'T> -> other: ASet<'T> -> ASet<'T>
  val intersectWith: set: ASet<'T> -> other: ASet<'T> -> ASet<'T>

  /// Union of two sets.
  val union: set: ASet<'T> -> other: ASet<'T> -> ASet<'T>

  /// Union with a combiner over shared elements. Sets hold no values, so
  /// the combiner picks which of the two equal keys is kept — the left one.
  val unionWith:
    combine: ('T -> 'T -> 'T) -> set: ASet<'T> -> other: ASet<'T> -> ASet<'T>

  /// Union of the sets carried by the given view. The inner sets cannot
  /// key a set themselves (a set view is not comparable), so the source
  /// view carries the sequence of sets.
  val unionMany: sets: AVal<seq<ASet<'T>>> -> ASet<'T>

  /// Symmetric difference: elements present in exactly one of the sets.
  val xor: set: ASet<'T> -> other: ASet<'T> -> ASet<'T>

  /// The sorted set as a list, ascending (Mibo.Adaptive <c>ASet.sort</c>
  /// parity, stable).
  val sort: set: ASet<'T> -> AList<'T> when 'T: comparison

  /// The sorted set as a list, descending.
  val sortDescending: set: ASet<'T> -> AList<'T> when 'T: comparison

  val sortBy:
    projection: ('T -> 'K) -> set: ASet<'T> -> AList<'T> when 'K: comparison

  val sortByDescending:
    projection: ('T -> 'K) -> set: ASet<'T> -> AList<'T> when 'K: comparison

  /// The sorted set as a list with the given comparison function.
  val sortWith: comparison: ('T -> 'T -> int) -> set: ASet<'T> -> AList<'T>
