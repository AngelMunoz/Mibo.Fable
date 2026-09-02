namespace Mibo.Signals

// The changeable map and the core adaptive map surface: builders, the write
// trio, reads, and the delta-sink bridge. Derivations live in their own
// group further down the file. Semantics follow Mibo.Adaptive's
// Api.fs/Changeable.fs: `add` inserts a new key, `set` writes an existing
// key, `addOrUpdate` upserts, `post*` queues boundary intents that apply at
// the next operation, and every materialization is guarded against
// pack-inside-a-batch.

/// <summary>A changeable map: the writable counterpart of <see cref="AMap"/>.</summary>
[<Class>]
type CMap<'K, 'V when 'K: comparison and 'V: equality> =

  /// <summary>An empty changeable map.</summary>
  new: unit -> CMap<'K, 'V>

  /// <summary>A changeable map with the given entries (later duplicates win).</summary>
  new: items: seq<'K * 'V> -> CMap<'K, 'V>

  /// <summary>Views the changeable map as an adaptive map.</summary>
  member Value: AMap<'K, 'V>

  /// <summary>
  /// Inserts a new key. No-op (returns false) when the key already exists.
  /// </summary>
  member Add: key: 'K * value: 'V -> bool

  /// <summary>
  /// Writes an existing key. No-op (returns false) when the key is absent.
  /// </summary>
  member Set: key: 'K * value: 'V -> bool

  /// <summary>
  /// Inserts or overwrites. No-op when the value is unchanged.
  /// </summary>
  member AddOrUpdate: key: 'K * value: 'V -> bool

  /// <summary>Removes a key. No-op when absent. Returns whether it existed.</summary>
  member Remove: key: 'K -> bool

  /// <summary>Removes all entries.</summary>
  member Clear: unit -> unit

  /// <summary>Replaces the whole map (later duplicates win).</summary>
  member SetAll: items: seq<'K * 'V> -> unit

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
  /// Registers a delta sink. The sink receives the initial content as one
  /// net delta, then one net delta per change (one per batch when the
  /// writes happen inside <see cref="Collections.batch"/>). Returns the
  /// disposer; callers must dispose or the map keeps notifying.
  /// </summary>
  val observe<'K, 'V when 'K: comparison and 'V: equality> :
    sink: (MapDelta<'K, 'V> -> unit) -> map: AMap<'K, 'V> -> System.IDisposable

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

/// <summary>Operations on changeable maps.</summary>
[<RequireQualifiedAccess>]
module CMap =

  /// <summary>An empty changeable map.</summary>
  val empty<'K, 'V when 'K: comparison and 'V: equality> : CMap<'K, 'V>

  /// <summary>A changeable map with the given entries.</summary>
  val ofSeq: items: seq<'K * 'V> -> CMap<'K, 'V>

  /// <summary>Inserts a new key (no-op when present).</summary>
  val add: key: 'K -> value: 'V -> map: CMap<'K, 'V> -> bool

  /// <summary>Writes an existing key (no-op when absent).</summary>
  val set: key: 'K -> value: 'V -> map: CMap<'K, 'V> -> bool

  /// <summary>Inserts or overwrites.</summary>
  val addOrUpdate: key: 'K -> value: 'V -> map: CMap<'K, 'V> -> bool

  /// <summary>Removes a key (no-op when absent).</summary>
  val remove: key: 'K -> map: CMap<'K, 'V> -> bool

  /// <summary>Removes all entries.</summary>
  val clear: map: CMap<'K, 'V> -> unit

  /// <summary>Replaces the whole map.</summary>
  val setAll: items: seq<'K * 'V> -> map: CMap<'K, 'V> -> unit

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
