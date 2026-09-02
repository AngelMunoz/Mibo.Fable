namespace Mibo.Signals

// ─────────────────────────────────────────────────────────────────────────────
// Signals.Collections.List — the AList family: an adaptive list with stable
// internal ids over @preact/signals-core. Element cells are keyed by id, not
// position: moves and inserts reorder the order channel without recomputing
// element values. CList is the changeable write side.
// ─────────────────────────────────────────────────────────────────────────────

/// One changeable list: writes land immediately (or at the next boundary
/// for the posted intents) and the <see cref="AList"/> view tracks them.
/// Insertions and moves keep element identities stable.
type CList<'T when 'T: equality> =

  internal new: items: seq<'T> -> CList<'T>

  /// The adaptive view of the list.
  member Value: AList<'T>

  /// Applies the pending boundary intents now.
  member FlushPosts: unit -> unit

  /// Appends one element.
  member Append: item: 'T -> unit

  /// Appends a run of elements.
  member AddRange: items: seq<'T> -> unit

  /// Inserts one element at the front.
  member Prepend: item: 'T -> unit

  /// Inserts one element before the given position.
  member InsertAt: position: int * item: 'T -> unit

  /// Removes the first element equal to <c>item</c>; false when absent.
  member Remove: item: 'T -> unit

  /// Removes the element at the given position; raises when out of range.
  member RemoveAt: position: int -> unit

  /// Writes the element at the given position; raises when out of range.
  member UpdateAt: position: int * item: 'T -> unit

  /// Replaces the whole content with the given values.
  member Set: values: seq<'T> -> unit

  /// Removes every element.
  member Clear: unit -> unit

  /// Replaces the whole content; true when it changed.
  member UpdateTo: items: seq<'T> -> bool

  member PostAppend: items: seq<'T> -> unit
  member PostPrepend: item: 'T -> unit
  member PostInsertAt: position: int * item: 'T -> unit
  member PostRemove: item: 'T -> unit
  member PostRemoveAt: position: int -> unit
  member PostSet: values: seq<'T> -> unit
  member PostUpdateAt: position: int * item: 'T -> unit
  member PostClear: unit -> unit

  /// Materializes the current content (pack contract).
  member Force: unit -> 'T array

[<RequireQualifiedAccess>]
module CList =

  /// An empty changeable list.
  val empty<'T when 'T: equality> : CList<'T>

  /// A changeable list with the given initial content.
  val ofSeq: items: seq<'T> -> CList<'T> when 'T: equality
  val ofArray: items: 'T array -> CList<'T> when 'T: equality
  val ofList: items: 'T list -> CList<'T> when 'T: equality

  /// A list of <c>count</c> elements produced by <c>initializer</c>.
  val init:
    count: int -> initializer: (int -> 'T) -> CList<'T> when 'T: equality

  /// Inclusive integer range <c>first..last</c>.
  val range: first: int -> last: int -> CList<int> when 'T: equality

  val addRange: items: seq<'T> -> list: CList<'T> -> unit when 'T: equality
  val append: item: 'T -> list: CList<'T> -> unit when 'T: equality
  val prepend: item: 'T -> list: CList<'T> -> unit when 'T: equality

  val insertAt:
    position: int -> item: 'T -> list: CList<'T> -> unit when 'T: equality

  val remove: item: 'T -> list: CList<'T> -> unit when 'T: equality
  val removeAt: position: int -> list: CList<'T> -> unit when 'T: equality

  val updateAt:
    position: int -> item: 'T -> list: CList<'T> -> unit when 'T: equality

  /// Replaces the whole content with the given values.
  val set: values: seq<'T> -> list: CList<'T> -> unit when 'T: equality
  val clear: list: CList<'T> -> unit when 'T: equality
  val updateTo: items: seq<'T> -> list: CList<'T> -> bool when 'T: equality

  /// <summary>
  /// Applies a batch of list operations atomically: one net change at the
  /// end of the batch (Mibo.Adaptive <c>CList.perform</c> parity).
  /// Positions refer to the state as of the previous operation.
  /// </summary>
  val perform:
    delta: ListDeltaBuilder<'T> -> list: CList<'T> -> unit when 'T: equality

  val postAppend: items: seq<'T> -> list: CList<'T> -> unit when 'T: equality
  val postPrepend: item: 'T -> list: CList<'T> -> unit when 'T: equality

  val postInsertAt:
    position: int -> item: 'T -> list: CList<'T> -> unit when 'T: equality

  val postRemove: item: 'T -> list: CList<'T> -> unit when 'T: equality
  val postRemoveAt: position: int -> list: CList<'T> -> unit when 'T: equality

  /// Queues a whole-content replace as a boundary intent.
  val postSet: values: seq<'T> -> list: CList<'T> -> unit when 'T: equality

  val postUpdateAt:
    position: int -> item: 'T -> list: CList<'T> -> unit when 'T: equality

  val postClear: list: CList<'T> -> unit when 'T: equality

  val value: list: CList<'T> -> AList<'T> when 'T: equality

  /// Materializes the current content; raises inside a batch.
  val force: list: CList<'T> -> 'T array when 'T: equality

[<RequireQualifiedAccess>]
module AList =

  /// The empty list; sealed — no write path reaches its content.
  val empty<'T> : AList<'T>

  /// A list built once from <c>create</c> on the first read, then cached.
  val constant: create: (unit -> seq<'T>) -> AList<'T>

  /// <see cref="constant"/> under the delayed-build name.
  val delay: create: (unit -> seq<'T>) -> AList<'T>

  /// A list over prebuilt stable-id cells; version is pinned at zero.
  val ofCells: cells: ListCells<'T> -> AList<'T>

  val ofSeq: items: seq<'T> -> AList<'T>
  val ofArray: items: 'T array -> AList<'T>
  val ofList: items: 'T list -> AList<'T>
  val ofResizeArray: items: 'T ResizeArray -> AList<'T>
  val single: item: 'T -> AList<'T>

  /// A list of <c>count</c> elements produced by <c>initializer</c>.
  val init: count: int -> initializer: (int -> 'T) -> AList<'T>

  /// Inclusive integer range <c>first..last</c>.
  val range: first: int -> last: int -> AList<int>

  /// A list tracking an aval of snapshots; every change rebuilds the
  /// stable ids (the snapshot carries no identity across changes).
  val ofAVal: value: AVal<seq<'T>> -> AList<'T>

  /// A list of the set's elements in the set's comparison order.
  val ofASet: set: ASet<'T> -> AList<'T> when 'T: comparison

  /// A list over external world data: <c>snapshot</c> runs at most once
  /// per <c>invalidate</c> call, on the next read.
  val ofExternal: snapshot: (unit -> seq<'T>) -> AList<'T> * (unit -> unit)

  /// <summary>
  /// A pull-model compute (Mibo.Adaptive <c>AList.custom</c> parity): the
  /// compute receives the current content and a delta builder; it appends
  /// the operations that describe the change since its previous call (for
  /// example, by consuming its own event queue) and the list applies them
  /// to the stable-id cells. Positions refer to the state as of the
  /// previous operation. The compute re-runs on every read.
  /// </summary>
  val custom: compute: ('T array -> ListDeltaBuilder<'T> -> unit) -> AList<'T>

  /// The concatenation of two lists (Mibo.Adaptive <c>AList.append</c> parity).
  val append: left: AList<'T> -> right: AList<'T> -> AList<'T>

  /// Materializes the current content; raises inside a batch.
  val force: list: AList<'T> -> 'T array
  val getValue: list: AList<'T> -> 'T array
  val toArray: list: AList<'T> -> 'T array
  val toList: list: AList<'T> -> 'T list
  val toAVal: list: AList<'T> -> AVal<'T array>
  val toASet: list: AList<'T> -> ASet<'T> when 'T: comparison

  /// A map from position to element. Positions are list indices, so any
  /// structural change (insert, move, remove) re-derives the map.
  val toIndexedASet: list: AList<'T> -> AMap<int, 'T>

  val isEmpty: list: AList<'T> -> AVal<bool>
  val count: list: AList<'T> -> AVal<int>
  val countBy: predicate: ('T -> bool) -> list: AList<'T> -> AVal<int>
  val countByA: predicate: ('T -> AVal<bool>) -> list: AList<'T> -> AVal<int>
  val exists: predicate: ('T -> bool) -> list: AList<'T> -> AVal<bool>
  val existsA: predicate: ('T -> AVal<bool>) -> list: AList<'T> -> AVal<bool>
  val forall: predicate: ('T -> bool) -> list: AList<'T> -> AVal<bool>
  val forallA: predicate: ('T -> AVal<bool>) -> list: AList<'T> -> AVal<bool>

  /// The element at <c>index</c>, or none when out of range.
  val tryAt: index: int -> list: AList<'T> -> AVal<'T voption>

  /// <see cref="tryAt"/> — reads the current cell at the given position.
  val tryGet: index: int -> list: AList<'T> -> AVal<'T voption>
  val tryFirst: list: AList<'T> -> AVal<'T voption>
  val tryLast: list: AList<'T> -> AVal<'T voption>
  val tryMax: list: AList<'T> -> AVal<'T voption> when 'T: comparison
  val tryMin: list: AList<'T> -> AVal<'T voption> when 'T: comparison

  val tryMaxA:
    by: ('T -> AVal<'M>) -> list: AList<'T> -> AVal<'M voption>
      when 'M: comparison

  val tryMinA:
    by: ('T -> AVal<'M>) -> list: AList<'T> -> AVal<'M voption>
      when 'M: comparison

  /// Reduces the elements in order with <c>op</c>; none when empty.
  val reduce: op: ('T -> 'T -> 'T) -> list: AList<'T> -> AVal<'T voption>

  val reduceBy:
    by: ('T -> 'M) ->
    op: ('M -> 'M -> 'M) ->
    list: AList<'T> ->
      AVal<'M voption>

  val reduceByA:
    by: ('T -> AVal<'M>) ->
    op: ('M -> 'M -> 'M) ->
    list: AList<'T> ->
      AVal<'M voption>

  val fold: add: ('s -> 'T -> 's) -> zero: 's -> list: AList<'T> -> AVal<'s>

  val foldGroup:
    add: ('s -> 'T -> 's) ->
    subtract: ('s -> 'T -> 's) ->
    zero: 's ->
    list: AList<'T> ->
      AVal<'s>

  val foldHalfGroup:
    add: ('s -> 'T -> 's) ->
    trySubtract: ('s -> 'T -> 's voption) ->
    zero: 's ->
    list: AList<'T> ->
      AVal<'s>

  /// Average of the elements. Empty lists average to 0.0.
  val average: list: AList<float> -> AVal<float>
  val averageBy: by: ('T -> float) -> list: AList<'T> -> AVal<float>
  val averageByA: by: ('T -> AVal<float>) -> list: AList<'T> -> AVal<float>

  /// Sum of the elements. Empty lists sum to 0.0.
  val sum: list: AList<float> -> AVal<float>
  val sumBy: by: ('T -> float) -> list: AList<'T> -> AVal<float>
  val sumByA: by: ('T -> AVal<float>) -> list: AList<'T> -> AVal<float>

  /// Maps every element in order. Element cells are keyed by the stable
  /// internal ids: a move or insert does not recompute mapped values —
  /// only order-dependent consumers re-derive.
  val map: mapping: ('T -> 'M) -> list: AList<'T> -> AList<'M>

  /// Map where the mapping returns an already-tracked view per element.
  val mapA: mapping: ('T -> AVal<'M>) -> list: AList<'T> -> AList<'M>

  // <summary>
  // Element disposal is out of scope for v1 — a mapped element with a
  // lifetime has no sink to release it. Use `map`.

  // val mapUse: ruled out for v1
  // val mapUsei: ruled out for v1

  /// Maps with the position. Positions are indices, so any structural
  /// change re-derives the mapping.
  val mapi: mapping: (int -> 'T -> 'M) -> list: AList<'T> -> AList<'M>
  val mapiA: mapping: (int -> 'T -> AVal<'M>) -> list: AList<'T> -> AList<'M>

  val choose: chooser: ('T -> 'M option) -> list: AList<'T> -> AList<'M>
  val chooseA: chooser: ('T -> AVal<'M option>) -> list: AList<'T> -> AList<'M>
  val chooseAV: chooser: ('T -> AVal<'M option>) -> list: AList<'T> -> AList<'M>
  val chooseV: chooser: ('T -> 'M option) -> list: AList<'T> -> AList<'M>

  /// Chooses with the position — positions are indices, so structural
  /// changes re-derive the choice.
  val choosei: chooser: (int -> 'T -> 'M option) -> list: AList<'T> -> AList<'M>

  val chooseiA:
    chooser: (int -> 'T -> AVal<'M option>) -> list: AList<'T> -> AList<'M>

  val chooseiAV:
    chooser: (int -> 'T -> AVal<'M option>) -> list: AList<'T> -> AList<'M>

  val chooseiV:
    chooser: (int -> 'T -> 'M option) -> list: AList<'T> -> AList<'M>

  val filter: predicate: ('T -> bool) -> list: AList<'T> -> AList<'T>
  val filterA: predicate: ('T -> AVal<bool>) -> list: AList<'T> -> AList<'T>

  /// Filters with the position — positions are indices, so structural
  /// changes re-derive the filter.
  val filteri: predicate: (int -> 'T -> bool) -> list: AList<'T> -> AList<'T>

  val filteriA:
    predicate: (int -> 'T -> AVal<bool>) -> list: AList<'T> -> AList<'T>

  /// Flattens in order: every element maps to a whole list; the result is
  /// the concatenation of the inner lists.
  val bind: binder: ('T -> AList<'M>) -> list: AList<'T> -> AList<'M>

  /// Pairwise flattening: elements at the same position drive the binder;
  /// the result has the shorter length.
  val bind2:
    binder: ('A -> 'B -> AList<'M>) ->
    list1: AList<'A> ->
    list2: AList<'B> ->
      AList<'M>

  /// Three-way pairwise flattening — see <c>bind2</c>.
  val bind3:
    binder: ('A -> 'B -> 'C -> AList<'M>) ->
    list1: AList<'A> ->
    list2: AList<'B> ->
    list3: AList<'C> ->
      AList<'M>

  /// Concatenates the lists held by <c>lists</c> in its order.
  val concat: lists: AList<AList<'T>> -> AList<'T>

  /// A list of position-element pairs.
  val indexed: list: AList<'T> -> AList<int * 'T>

  /// The elements in reverse order.
  val rev: list: AList<'T> -> AList<'T>

  /// The subrange from <c>index</c> taking <c>count</c> elements.
  val sub: index: int -> count: int -> list: AList<'T> -> AList<'T>
  val subA: index: AVal<int> -> count: AVal<int> -> list: AList<'T> -> AList<'T>

  /// The first <c>count</c> elements.
  val take: count: int -> list: AList<'T> -> AList<'T>
  val takeA: count: AVal<int> -> list: AList<'T> -> AList<'T>

  /// Every element after the first <c>count</c>.
  val skip: count: int -> list: AList<'T> -> AList<'T>
  val skipA: count: AVal<int> -> list: AList<'T> -> AList<'T>

  /// Adjacent element pairs; empty for lists of fewer than two elements.
  val pairwise: list: AList<'T> -> AList<'T * 'T>

  /// Adjacent pairs plus the wrap-around pair (last, first).
  val pairwiseCyclic: list: AList<'T> -> AList<'T * 'T>

  val sort: list: AList<'T> -> AList<'T> when 'T: comparison
  val sortDescending: list: AList<'T> -> AList<'T> when 'T: comparison

  val sortBy:
    projection: ('T -> 'K) -> list: AList<'T> -> AList<'T> when 'K: comparison

  val sortByDescending:
    projection: ('T -> 'K) -> list: AList<'T> -> AList<'T> when 'K: comparison

  /// Sorts with the position in the projection — positions are indices,
  /// so structural changes re-derive the sort.
  val sortByi:
    projection: (int -> 'T -> 'K) -> list: AList<'T> -> AList<'T>
      when 'K: comparison

  val sortByDescendingi:
    projection: (int -> 'T -> 'K) -> list: AList<'T> -> AList<'T>
      when 'K: comparison

  /// Sorts with the given comparison function.
  val sortWith: comparison: ('T -> 'T -> int) -> list: AList<'T> -> AList<'T>
