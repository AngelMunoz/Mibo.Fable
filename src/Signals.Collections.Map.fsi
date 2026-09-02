namespace Mibo.Signals

// The changeable map and the core adaptive map surface: builders, the write
// trio, reads, and the delta-sink bridge. Derivations live in their own
// group further down the file. Semantics follow Mibo.Adaptive's
// Api.fs/Changeable.fs: `add` inserts a new key, `set` writes an existing
// key, `addOrUpdate` upserts, `post*` queues boundary intents that apply at
// the next operation, and every materialization is guarded against
// pack-inside-a-batch.

/// <summary>A changeable map: the writable counterpart of <see cref="AMap"/>.</summary>
type CMap<'K, 'V when 'K: comparison and 'V: equality> =

  /// <summary>An empty changeable map.</summary>
  new: unit -> CMap<'K, 'V>

  /// <summary>A changeable map with the given entries (later duplicates win).</summary>
  new: items: seq<'K * 'V> -> CMap<'K, 'V>

  /// <summary>Views the changeable map as an adaptive map.</summary>
  member Value: AMap<'K, 'V>

  /// <summary>
  /// Inserts or overwrites. No-op when the value is unchanged.
  /// </summary>
  member AddOrUpdate: key: 'K * value: 'V -> unit

  /// <summary>Removes a key. No-op when absent.</summary>
  member Remove: key: 'K -> unit

  /// <summary>Removes all entries.</summary>
  member Clear: unit -> unit

  /// <summary>Replaces the whole map (later duplicates win).</summary>
  member Set: items: seq<'K * 'V> -> unit

  /// <summary>
  /// Replaces the whole map and returns whether the content changed. An
  /// equal target marks nothing.
  /// </summary>
  member UpdateTo: items: seq<'K * 'V> -> bool

  /// <summary>Queues an add-or-update as a boundary intent.</summary>
  member PostAddOrUpdate: key: 'K * value: 'V -> unit

  /// <summary>Queues a remove as a boundary intent.</summary>
  member PostRemove: key: 'K -> unit

  /// <summary>Queues a clear as a boundary intent.</summary>
  member PostClear: unit -> unit

  /// <summary>
  /// Queues a full replace as a boundary intent; the replace supersedes the
  /// other pending intents queued before it.
  /// </summary>
  member PostSet: items: seq<'K * 'V> -> unit

  /// <summary>Applies the pending boundary intents now.</summary>
  member FlushPosts: unit -> unit

  /// <summary>Tests whether the key is present.</summary>
  member ContainsKey: key: 'K -> bool

  /// <summary>Gets the value for the key, or <c>ValueNone</c> when absent.</summary>
  member TryGetValue: key: 'K -> 'V voption

  /// <summary>Gets the value for the key (throws when absent).</summary>
  member Item: key: 'K -> 'V with get

/// <summary>Operations on adaptive maps.</summary>
[<RequireQualifiedAccess>]
module AMap =

  /// <summary>An empty adaptive map.</summary>
  val empty<'K, 'V when 'K: comparison> : AMap<'K, 'V>

  /// <summary>
  /// An adaptive map whose content is fixed but computed lazily, once, at
  /// first read. Enables self-referential definitions.
  /// </summary>
  val constant: create: (unit -> Map<'K, 'V>) -> AMap<'K, 'V>

  /// <summary>Alias of <see cref="constant"/>.</summary>
  val delay: create: (unit -> Map<'K, 'V>) -> AMap<'K, 'V>

  /// <summary>An adaptive map over fixed entries (a seq).</summary>
  val ofSeq: items: seq<'K * 'V> -> AMap<'K, 'V>

  /// <summary>An adaptive map over a fixed array of entries.</summary>
  val ofArray: items: ('K * 'V) array -> AMap<'K, 'V>

  /// <summary>An adaptive map over a fixed list of entries.</summary>
  val ofList: items: ('K * 'V) list -> AMap<'K, 'V>

  /// <summary>An adaptive map over a fixed F# Map.</summary>
  val ofMap: items: Map<'K, 'V> -> AMap<'K, 'V>

  /// <summary>A constant map with a single entry.</summary>
  val single: key: 'K * value: 'V -> AMap<'K, 'V>

  /// <summary>
  /// An adaptive map over an adaptive value of a sequence of entries. Every
  /// change of the value replaces the whole state and emits the diff as the
  /// delta.
  /// </summary>
  val ofAVal<'K, 'V when 'K: comparison and 'V: equality> :
    value: AVal<seq<'K * 'V>> -> AMap<'K, 'V>

  /// <summary>
  /// An adaptive map driven by an external snapshot: the snapshot runs at
  /// most once per invalidate, on the next read, and is diffed against the
  /// previous snapshot. Returns the map and the invalidate handle.
  /// </summary>
  val ofExternal<'K, 'V when 'K: comparison and 'V: equality> :
    snapshot: (unit -> Map<'K, 'V>) -> AMap<'K, 'V> * (unit -> unit)

  /// <summary>
  /// Returns the current state as a plain F# Map snapshot. Materializes the
  /// collection — raises inside a batch (the pack-once-per-step contract).
  /// </summary>
  val force: map: AMap<'K, 'V> -> Map<'K, 'V>

  /// <summary>The transient view of the current state (allocates; guarded).</summary>
  val getValue: map: AMap<'K, 'V> -> Map<'K, 'V>

  /// <summary>Materializes the F# Map counterpart (guarded).</summary>
  val toMap: map: AMap<'K, 'V> -> Map<'K, 'V>

  /// <summary>The entries as a sequence (guarded).</summary>
  val toSeq: map: AMap<'K, 'V> -> seq<'K * 'V>

  /// <summary>
  /// Materializes the map as an adaptive value; every structural or value
  /// change re-materializes a new snapshot (the retain boundary).
  /// </summary>
  val toAVal: map: AMap<'K, 'V> -> AVal<Map<'K, 'V>>

  /// <summary>
  /// The number of entries. Ignores value writes: only structural changes
  /// re-evaluate this value or its dependents.
  /// </summary>
  val count: map: AMap<'K, 'V> -> AVal<int>

  /// <summary>
  /// Tests if the map is empty. Only a change crossing the empty/non-empty
  /// boundary changes the result.
  /// </summary>
  val isEmpty: map: AMap<'K, 'V> -> AVal<bool>

  /// <summary>
  /// Adaptively tests whether the key is present. Per-key precise: a value
  /// write to the key re-evaluates; a write to another key does not.
  /// </summary>
  val containsKey: key: 'K -> map: AMap<'K, 'V> -> AVal<bool>

  /// <summary>
  /// Adaptively looks up the key: the value, or <c>ValueNone</c> when
  /// absent. Per-key precise, like <see cref="containsKey"/>.
  /// </summary>
  val tryFind: key: 'K -> map: AMap<'K, 'V> -> AVal<'V voption>

  /// <summary>
  /// Adaptively looks up the key; reading the value throws
  /// <see cref="System.Collections.Generic.KeyNotFoundException"/> when absent.
  /// </summary>
  val find: key: 'K -> map: AMap<'K, 'V> -> AVal<'V>

  /// <summary>Adaptively tests if any entry satisfies the predicate.</summary>
  val exists: predicate: ('K -> 'V -> bool) -> map: AMap<'K, 'V> -> AVal<bool>

  /// <summary>Adaptively tests if every entry satisfies the predicate.</summary>
  val forall: predicate: ('K -> 'V -> bool) -> map: AMap<'K, 'V> -> AVal<bool>

  /// <summary>Adaptively counts the entries that satisfy the predicate.</summary>
  val countBy: predicate: ('K -> 'V -> bool) -> map: AMap<'K, 'V> -> AVal<int>

  /// <summary>
  /// Adaptively folds the map with <c>add</c>. Coarse: re-runs on any write.
  /// </summary>
  val fold:
    add: ('s -> 'K -> 'V -> 's) -> zero: 's -> map: AMap<'K, 'V> -> AVal<'s>

  /// <summary>
  /// Adaptively folds the map with an invertible <c>subtract</c>. Coarse on
  /// the web model: re-runs on any write (the subtract exists for
  /// signature parity with Mibo.Adaptive).
  /// </summary>
  val foldGroup:
    add: ('s -> 'K -> 'V -> 's) ->
    subtract: ('s -> 'K -> 'V -> 's) ->
    zero: 's ->
    map: AMap<'K, 'V> ->
      AVal<'s>

  /// <summary>
  /// Adaptively folds the map with a partially invertible
  /// <c>trySubtract</c>. Coarse on the web model.
  /// </summary>
  val foldHalfGroup:
    add: ('s -> 'K -> 'V -> 's) ->
    trySubtract: ('s -> 'K -> 'V -> 's voption) ->
    zero: 's ->
    map: AMap<'K, 'V> ->
      AVal<'s>

  /// <summary>An adaptive set of the map's keys.</summary>
  val keys: map: AMap<'K, 'V> -> ASet<'K>

  /// <summary>An adaptive set of the map's key/value struct pairs.</summary>
  val toASet: map: AMap<'K, 'V> -> ASet<struct ('K * 'V)>

  /// <summary>An adaptive set of the map's distinct values.</summary>
  val toASetValues: map: AMap<'K, 'V> -> ASet<'V>

  /// <summary>
  /// An adaptive list of the map's entries. The order is the map's
  /// iteration order, stable while the map does not change; the entry cells
  /// are shared with the map (value writes do not rebuild the list).
  /// </summary>
  val toAList: map: AMap<'K, 'V> -> AList<'K * 'V>
  // AMap derivations and aggregates. Derivations re-derive their cells from the
  // source on any source change (coarse granularity, same cost class as
  // Mibo.Adaptive's filter); element reads inside the derived map stay per-key
  // signals. Aggregates re-run on any write — no incremental reductions.

  /// <summary>
  /// A pull-model compute (Mibo.Adaptive <c>AMap.custom</c> parity): the
  /// compute receives the current content and a delta builder; it appends
  /// the operations that describe the change since its previous call (for
  /// example, by consuming its own event queue) and the map applies them.
  /// The compute re-runs on every read.
  /// </summary>
  val custom:
    compute: (Map<'K, 'V> -> MapDeltaBuilder<'K, 'V> -> unit) -> AMap<'K, 'V>
      when 'K: comparison and 'V: equality

  /// <summary>
  /// Derives a map from a set of keys: each key maps to <c>mapping key</c>.
  /// Duplicate keys cannot occur — the source is a set.
  /// </summary>
  val ofASet: keys: ASet<'K> -> mapping: ('K -> 'V) -> AMap<'K, 'V>

  /// <summary>
  /// <see cref="ofASet"/> under the ignore-duplicates name: sets cannot
  /// carry duplicates, so this is the same derivation.
  /// </summary>
  val ofASetIgnoreDuplicates:
    keys: ASet<'K> -> mapping: ('K -> 'V) -> AMap<'K, 'V>

  /// <summary>Derives a map from a set of keys with the mapping argument last.</summary>
  val ofASetMapped: mapping: ('K -> 'V) -> keys: ASet<'K> -> AMap<'K, 'V>

  /// <summary><see cref="ofASetMapped"/> under the ignore-duplicates name.</summary>
  val ofASetMappedIgnoreDuplicates:
    mapping: ('K -> 'V) -> keys: ASet<'K> -> AMap<'K, 'V>

  /// <summary>
  /// Derives a map from a list of entries. Entries repeat freely; for a
  /// duplicated key the last entry wins, matching <c>Map</c> semantics.
  /// </summary>
  val ofAList: entries: AList<'K * 'V> -> AMap<'K, 'V>

  /// <summary>
  /// Maps every entry through <c>mapping key value</c>. Keys keep their
  /// cells: a value write recomputes only the mapped value of that key.
  /// </summary>
  val map: mapping: ('K -> 'V -> 'M) -> map: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>
  /// Maps every entry through <c>mapping key value</c> where the result is
  /// already a view: the derived cell tracks the returned view.
  /// </summary>
  val mapA: mapping: ('K -> 'V -> AVal<'M>) -> map: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>Maps values only, keys pass through unchanged.</summary>
  val mapV: mapping: ('V -> 'M) -> source: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>
  /// Maps every entry to a set of elements and derives the union set.
  /// </summary>
  val mapSet: mapping: ('K -> 'V -> Set<'M>) -> map: AMap<'K, 'V> -> ASet<'M>

  /// <summary>
  /// Filtered view: keeps the entries for which <c>predicate key value</c>
  /// holds. Dropped keys lose their cells; re-added keys get fresh ones.
  /// </summary>
  val filter: predicate: ('K -> 'V -> bool) -> map: AMap<'K, 'V> -> AMap<'K, 'V>

  /// <summary>Filter where the predicate result is already a view.</summary>
  val filterA:
    predicate: ('K -> 'V -> AVal<bool>) -> map: AMap<'K, 'V> -> AMap<'K, 'V>

  /// <summary>
  /// Filter by comparing the value view to a fixed threshold value view —
  /// entries drop while the comparison fails.
  /// </summary>
  val filterV:
    predicate: ('K -> 'V -> AVal<bool>) -> map: AMap<'K, 'V> -> AMap<'K, 'V>

  /// <summary>Keeps entries whose mapped option holds a value.</summary>
  val choose:
    chooser: ('K -> 'V -> 'M option) -> map: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>Choose where the chooser result is already a view.</summary>
  val chooseA:
    chooser: ('K -> 'V -> AVal<'M option>) -> map: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>Choose over an aval-valued mapping — see <c>chooseA</c>.</summary>
  val chooseAV:
    chooser: ('K -> 'V -> AVal<'M option>) -> map: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>Choose over value-only mappings — see <c>choose</c>.</summary>
  val chooseV: chooser: ('V -> 'M option) -> map: AMap<'K, 'V> -> AMap<'K, 'M>

  /// <summary>
  /// Keywise outer merge of two maps: the chooser sees both values as
  /// voptions and picks the merged value (or drops the key).
  /// </summary>
  val choose2:
    chooser: ('K -> 'V1 voption -> 'V2 voption -> 'M voption) ->
    map1: AMap<'K, 'V1> ->
    map2: AMap<'K, 'V2> ->
      AMap<'K, 'M>

  /// <summary>
  /// Keywise inner merge: the chooser runs only where both maps hold the
  /// key; it returns the merged value or drops the key.
  /// </summary>
  val choose2V:
    chooser: ('K -> 'V1 -> 'V2 -> 'M voption) ->
    map1: AMap<'K, 'V1> ->
    map2: AMap<'K, 'V2> ->
      AMap<'K, 'M>

  /// <summary>
  /// Flattens: every entry maps to a whole map and the result is the union
  /// of the inner maps (later entries win on key collisions).
  /// </summary>
  val bind:
    binder: ('K -> 'V -> AMap<'K2, 'V2>) -> map: AMap<'K, 'V> -> AMap<'K2, 'V2>

  /// <summary>
  /// Keywise flattening over two maps: where both hold the key the binder
  /// produces the inner map; the union of the inner maps is the result.
  /// </summary>
  val bind2:
    binder: ('K -> 'V1 -> 'V2 -> AMap<'K2, 'R>) ->
    map1: AMap<'K, 'V1> ->
    map2: AMap<'K, 'V2> ->
      AMap<'K2, 'R>

  /// <summary>Keywise flattening over three maps — see <c>bind2</c>.</summary>
  val bind3:
    binder: ('K -> 'V1 -> 'V2 -> 'V3 -> AMap<'K2, 'R>) ->
    map1: AMap<'K, 'V1> ->
    map2: AMap<'K, 'V2> ->
    map3: AMap<'K, 'V3> ->
      AMap<'K2, 'R>

  /// <summary>
  /// Lookup join: every entry of <c>outer</c> pairs with the entry of
  /// <c>inner</c> found under <c>keyOf outerValue</c>. Entries whose join
  /// key has no inner match drop out. A join-key change swaps the old and
  /// the new target inside one recompute — no stale read is observable.
  /// </summary>
  val joinOn:
    keyOf: ('V1 -> 'M) ->
    outer: AMap<'K, 'V1> ->
    inner: AMap<'M, 'V2> ->
      AMap<'K, struct ('V1 * 'V2)>

  /// <summary>
  /// Groups entries by <c>keyOf key value</c>. Each group is a nested map
  /// view; group membership re-derives on any write (coarse granularity).
  /// </summary>
  val groupBy:
    keyOf: ('K -> 'V -> 'G) -> map: AMap<'K, 'V> -> AMap<'G, AMap<'K, 'V>>
      when 'G: comparison

  /// <summary>
  /// Entries whose key does not occur in <c>other</c>. Value writes do not
  /// recompute the structure — only key sets matter.
  /// </summary>
  val difference: map: AMap<'K, 'V> -> other: AMap<'K, 'M> -> AMap<'K, 'V>

  /// <summary>
  /// Entries whose key occurs in both maps, keeping the left value.
  /// </summary>
  val intersect: map: AMap<'K, 'V> -> other: AMap<'K, 'M> -> AMap<'K, 'V>

  /// <summary>Intersect pairing both values per key.</summary>
  val intersectV:
    map: AMap<'K, 'V1> -> other: AMap<'K, 'V2> -> AMap<'K, struct ('V1 * 'V2)>

  /// <summary>Intersect with a per-key value combiner.</summary>
  val intersectWith:
    combine: ('K -> 'V1 -> 'V2 -> 'M) ->
    map: AMap<'K, 'V1> ->
    other: AMap<'K, 'V2> ->
      AMap<'K, 'M>

  /// <summary>
  /// Union of two maps; where both hold a key the right value wins.
  /// </summary>
  val union: map: AMap<'K, 'V> -> other: AMap<'K, 'V> -> AMap<'K, 'V>

  /// <summary>Union with a per-key value combiner for shared keys.</summary>
  val unionWith:
    combine: ('K -> 'V -> 'V -> 'V) ->
    map: AMap<'K, 'V> ->
    other: AMap<'K, 'V> ->
      AMap<'K, 'V>

  /// <summary>Average of <c>by key value</c> over the entries.</summary>
  val averageBy: by: ('K -> 'V -> float) -> map: AMap<'K, 'V> -> AVal<float>

  /// <summary>Average of the tracked views returned by <c>by</c>.</summary>
  val averageByA:
    by: ('K -> 'V -> AVal<float>) -> map: AMap<'K, 'V> -> AVal<float>

  /// <summary>Sum of <c>by key value</c> over the entries.</summary>
  val sumBy: by: ('K -> 'V -> float) -> map: AMap<'K, 'V> -> AVal<float>

  /// <summary>Sum of the tracked views returned by <c>by</c>.</summary>
  val sumByA: by: ('K -> 'V -> AVal<float>) -> map: AMap<'K, 'V> -> AVal<float>

  /// <summary>Largest tracked view value, or none on an empty map.</summary>
  val tryMaxA:
    by: ('K -> 'V -> AVal<'M>) -> map: AMap<'K, 'V> -> AVal<'M voption>
      when 'M: comparison

  /// <summary>Smallest tracked view value, or none on an empty map.</summary>
  val tryMinA:
    by: ('K -> 'V -> AVal<'M>) -> map: AMap<'K, 'V> -> AVal<'M voption>
      when 'M: comparison

  /// <summary>Reduces the current values with <c>op</c>; none when empty.</summary>
  val reduce: op: ('V -> 'V -> 'V) -> map: AMap<'K, 'V> -> AVal<'V voption>

  /// <summary>Reduces the mapped values; none when empty.</summary>
  val reduceBy:
    by: ('K -> 'V -> 'M) ->
    op: ('M -> 'M -> 'M) ->
    map: AMap<'K, 'V> ->
      AVal<'M voption>

  /// <summary>Reduces the tracked views returned by <c>by</c>; none when empty.</summary>
  val reduceByA:
    by: ('K -> 'V -> AVal<'M>) ->
    op: ('M -> 'M -> 'M) ->
    map: AMap<'K, 'V> ->
      AVal<'M voption>

/// <summary>Operations on changeable maps.</summary>
[<RequireQualifiedAccess>]
module CMap =

  /// <summary>An empty changeable map.</summary>
  val empty<'K, 'V when 'K: comparison and 'V: equality> : CMap<'K, 'V>

  /// <summary>A changeable map with the given entries.</summary>
  val ofSeq: items: seq<'K * 'V> -> CMap<'K, 'V>

  /// <summary>Inserts or overwrites. No-op when the value is unchanged.</summary>
  val addOrUpdate: key: 'K -> value: 'V -> map: CMap<'K, 'V> -> unit

  /// <summary>Removes a key (no-op when absent).</summary>
  val remove: key: 'K -> map: CMap<'K, 'V> -> unit

  /// <summary>Removes all entries.</summary>
  val clear: map: CMap<'K, 'V> -> unit

  /// <summary>Replaces the whole map with the given entries.</summary>
  val set: value: Map<'K, 'V> -> map: CMap<'K, 'V> -> unit

  /// <summary>
  /// Replaces the content only when it differs; true when it changed.
  /// </summary>
  val updateTo: target: seq<'K * 'V> -> map: CMap<'K, 'V> -> bool

  /// <summary>
  /// Applies a batch of map operations atomically: one net change at the
  /// end of the batch (Mibo.Adaptive <c>CMap.perform</c> parity).
  /// </summary>
  val perform: delta: MapDeltaBuilder<'K, 'V> -> map: CMap<'K, 'V> -> unit

  /// <summary>Queues an add-or-update as a boundary intent.</summary>
  val postAddOrUpdate: key: 'K -> value: 'V -> map: CMap<'K, 'V> -> unit

  /// <summary>Queues a remove as a boundary intent.</summary>
  val postRemove: key: 'K -> map: CMap<'K, 'V> -> unit

  /// <summary>Queues a clear as a boundary intent.</summary>
  val postClear: map: CMap<'K, 'V> -> unit

  /// <summary>Queues a full replace as a boundary intent.</summary>
  val postSet: items: seq<'K * 'V> -> map: CMap<'K, 'V> -> unit

  /// <summary>Views the changeable map as an adaptive map.</summary>
  val value: map: CMap<'K, 'V> -> AMap<'K, 'V>

  /// <summary>Materializes the current state (guarded).</summary>
  val force: map: CMap<'K, 'V> -> Map<'K, 'V>

  /// <summary>Materializes the F# Map counterpart (guarded).</summary>
  val toMap: map: CMap<'K, 'V> -> Map<'K, 'V>

  /// <summary>Tests whether the key is present.</summary>
  val containsKey: key: 'K -> map: CMap<'K, 'V> -> bool

  /// <summary>Gets the value for the key, or <c>ValueNone</c> when absent.</summary>
  val tryGetValue: key: 'K -> map: CMap<'K, 'V> -> 'V voption

  /// <summary>Gets the value for the key (throws when absent).</summary>
  val item: key: 'K -> map: CMap<'K, 'V> -> 'V

// ─────────────────────────────────────────────────────────────────────────────
