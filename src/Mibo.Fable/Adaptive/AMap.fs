module Mibo.Fable.Adaptive.AMap

open System
open System.Collections.Generic

/// <summary>An empty adaptive map (FDA <c>AMap.empty</c> parity).</summary>
let empty<'K, 'V when 'K: equality> : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () -> Seq.empty)

/// <summary>
/// An adaptive map whose content is fixed but computed lazily, once, at
/// first read. Enables self-referential definitions (FDA parity: the
/// create function runs at most once; deviation: FDA's create returns a
/// <c>HashMap</c>, ours returns a <c>Dictionary</c>).
/// </summary>
let inline constant(create: unit -> Dictionary<'K, 'V>) : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () ->
    let d = create()
    let items = ResizeArray<'K * 'V>()
    let mutable e = d.GetEnumerator()

    while e.MoveNext() do
      items.Add((e.Current.Key, e.Current.Value))

    items :> seq<'K * 'V>)

/// <summary>Alias of <see cref="constant"/> (symmetry with <c>ASet.delay</c>; FDA has no <c>AMap.delay</c>).</summary>
let inline delay(create: unit -> Dictionary<'K, 'V>) : amap<'K, 'V> =
  constant create

/// <summary>An adaptive map over fixed, immutable entries.</summary>
let inline ofSeq(items: seq<'K * 'V>) : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () -> items)

/// <summary>An adaptive map over a fixed array of entries.</summary>
let inline ofArray(items: ('K * 'V)[]) : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () -> items)

/// <summary>An adaptive map over a fixed list of entries.</summary>
let inline ofList(items: ('K * 'V) list) : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () -> items)

/// <summary>An adaptive map over a fixed F# <c>Map</c>.</summary>
let inline ofMap(items: Map<'K, 'V>) : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () -> Map.toSeq items)

/// <summary>Maps every entry of the map.</summary>
let inline map (f: 'K -> 'V -> 'U) (mapValue: amap<'K, 'V>) : amap<'K, 'U> =
  new MapMapNode<'K, 'V, 'U>(mapValue, fun k v -> ValueSome(f k v))

/// <summary>Maps every entry, keeping only the ones the mapping returns a value for.</summary>
let inline choose
  (f: 'K -> 'V -> 'U option)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'U> =
  new MapMapNode<'K, 'V, 'U>(mapValue, fun k v -> f k v |> Option.toValueOption)

/// <summary>Maps every entry, keeping only the ones the mapping returns a value for.</summary>
let inline chooseV
  (f: 'K -> 'V -> 'U voption)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'U> =
  new MapMapNode<'K, 'V, 'U>(mapValue, f)

/// <summary>Maps the values only (FDA <c>AMap.map'</c> parity; the V suffix is the value-only convention).</summary>
let inline mapV (f: 'V -> 'U) (mapValue: amap<'K, 'V>) : amap<'K, 'U> =
  map (fun _ v -> f v) mapValue

/// <summary>Unions both maps, resolving colliding keys with the given function.</summary>
let inline unionWith
  (resolve: 'K -> 'V -> 'V -> 'V)
  (left: amap<'K, 'V>)
  (right: amap<'K, 'V>)
  : amap<'K, 'V> =
  new Choose2MapNode<'K, 'V, 'V, 'V>(
    left,
    right,
    fun k lv rv ->
      match struct (lv, rv) with
      | ValueSome l, ValueSome r -> ValueSome(resolve k l r)
      | ValueSome l, ValueNone -> ValueSome l
      | ValueNone, ValueSome r -> ValueSome r
      | ValueNone, ValueNone -> ValueNone
  )

/// <summary>
/// Unions both maps, preferring the RIGHT value when keys collide
/// (FDA parity: <c>union a b = unionWith (fun _ _ r -> r) a b</c>).
/// </summary>
let inline union (left: amap<'K, 'V>) (right: amap<'K, 'V>) : amap<'K, 'V> =
  unionWith (fun _ _ r -> r) left right

/// <summary>
/// The keys present in both maps, with the values paired. Struct pair:
/// the voption-first convention collapses FDA's <c>intersect</c> (tuple)
/// and <c>intersectV</c> (struct) into the struct form.
/// </summary>
let inline intersect
  (left: amap<'K, 'V1>)
  (right: amap<'K, 'V2>)
  : amap<'K, struct ('V1 * 'V2)> =
  new Choose2MapNode<'K, 'V1, 'V2, struct ('V1 * 'V2)>(
    left,
    right,
    fun k lv rv ->
      match struct (lv, rv) with
      | ValueSome l, ValueSome r -> ValueSome(struct (l, r))
      | _ -> ValueNone
  )

/// <summary>
/// Alias of <see cref="intersect"/> (FDA parity name; our intersect is
/// already the struct-pair form).
/// </summary>
let inline intersectV
  (left: amap<'K, 'V1>)
  (right: amap<'K, 'V2>)
  : amap<'K, struct ('V1 * 'V2)> =
  intersect left right

/// <summary>Intersects both maps, combining the paired values.</summary>
let inline intersectWith
  (combine: 'K -> 'V1 -> 'V2 -> 'V3)
  (left: amap<'K, 'V1>)
  (right: amap<'K, 'V2>)
  : amap<'K, 'V3> =
  new Choose2MapNode<'K, 'V1, 'V2, 'V3>(
    left,
    right,
    fun k lv rv ->
      match struct (lv, rv) with
      | ValueSome l, ValueSome r -> ValueSome(combine k l r)
      | _ -> ValueNone
  )

/// <summary>
/// The keys present in the left map but not in the right map, with the
/// left values (the AMap counterpart of <c>ASet.difference</c>). Right
/// values are ignored; only the right keys matter.
/// </summary>
let inline difference
  (left: amap<'K, 'V>)
  (right: amap<'K, 'V>)
  : amap<'K, 'V> =
  new Choose2MapNode<'K, 'V, 'V, 'V>(
    left,
    right,
    fun k lv rv ->
      match struct (lv, rv) with
      | ValueSome l, ValueNone -> ValueSome l
      | _ -> ValueNone
  )

/// <summary>
/// Groups the entries of a map by a computed key (FDA <c>AMap.groupBy</c>
/// parity). The output entries are live adaptive maps: the per-group
/// content follows the source adaptively, and the groups' own changes
/// reach the consumers without re-reading the whole map. A group
/// disappears when it becomes empty (removed at the next drain); a key
/// whose value changes group is moved between groups.
/// </summary>
let inline groupBy
  (keyOf: 'K -> 'V -> 'G)
  (mapValue: amap<'K, 'V>)
  : amap<'G, amap<'K, 'V>> =
  new GroupByMapNode<'K, 'V, 'G>(mapValue, keyOf)

/// <summary>
/// Merges both maps with a mapping that receives the key and both side
/// values (options) and returns the output value (option). The mapping
/// is called only when at least one side has a value (FDA parity); a key
/// with no value on either side is removed without a call.
/// </summary>
let inline choose2
  (mapping: 'K -> 'V1 option -> 'V2 option -> 'V3 option)
  (left: amap<'K, 'V1>)
  (right: amap<'K, 'V2>)
  : amap<'K, 'V3> =
  new Choose2MapNode<'K, 'V1, 'V2, 'V3>(
    left,
    right,
    fun k v v2 ->
      mapping k (v |> Option.ofValueOption) (v2 |> Option.ofValueOption)
      |> Option.toValueOption
  )

/// <summary>
/// Merges both maps with a mapping that receives the key and both side
/// values (voptions) and returns the output value (voption). The mapping
/// is called only when at least one side has a value (FDA parity); a key
/// with no value on either side is removed without a call. Voption-first:
/// this is FDA's <c>choose2V</c> (the option variant is not provided).
/// </summary>
let inline choose2V
  (mapping: 'K -> 'V1 voption -> 'V2 voption -> 'V3 voption)
  (left: amap<'K, 'V1>)
  (right: amap<'K, 'V2>)
  : amap<'K, 'V3> =
  new Choose2MapNode<'K, 'V1, 'V2, 'V3>(left, right, mapping)

/// <summary>
/// A map from a set of entries, keeping ALL values of a key in a HashSet
/// (FDA <c>ofASet</c> parity). A changed value set emits a fresh HashSet
/// in the delta (this node allocates by design).
/// </summary>
let inline ofASet(elements: aset<'K * 'V>) : amap<'K, HashSet<'V>> =
  new SetToMapKeepAllNode<'K, 'V, 'K * 'V>(elements, id)

/// <summary>
/// A map from a set of entries; duplicate keys keep the LAST value
/// (FDA <c>ofASetIgnoreDuplicates</c> parity: the constant path keeps the
/// last value, the delta path is arbitrary).
/// </summary>
let inline ofASetIgnoreDuplicates(elements: aset<'K * 'V>) : amap<'K, 'V> =
  new SetToMapNode<'K, 'V, 'K * 'V>(elements, id, true)

/// <summary>
/// A map from a set, deriving the key from every value and keeping ALL
/// values of a key in a HashSet (FDA <c>ofASetMapped</c> parity).
/// </summary>
let inline ofASetMapped
  (getKey: 'V -> 'K)
  (elements: aset<'V>)
  : amap<'K, HashSet<'V>> =
  new SetToMapKeepAllNode<'K, 'V, 'V>(elements, fun v -> (getKey v, v))

/// <summary>
/// A map from a set, deriving the key from every value; duplicate keys
/// keep the LAST value.
/// </summary>
let inline ofASetMappedIgnoreDuplicates
  (getKey: 'V -> 'K)
  (elements: aset<'V>)
  : amap<'K, 'V> =
  new SetToMapNode<'K, 'V, 'V>(elements, (fun v -> (getKey v, v)), true)

/// <summary>Maps the keys of a set to entries (FDA <c>mapSet</c> parity: the mapping runs per key).</summary>
let inline mapSet (mapping: 'K -> 'V) (set: aset<'K>) : amap<'K, 'V> =
  new SetToMapNode<'K, 'V, 'K>(set, (fun k -> (k, mapping k)), false)

/// <summary>
/// An adaptive set of the map's key/value pairs (FDA <c>AMap.toASet</c>
/// parity). Struct pairs: the library convention (cf.
/// <see cref="intersect"/>). The former keys behavior moved to
/// <see cref="keys"/>.
/// </summary>
let inline toASet(mapValue: amap<'K, 'V>) : aset<struct ('K * 'V)> =
  new MapToSetNode<'K, 'V, struct ('K * 'V)>(mapValue, fun k v -> struct (k, v))

/// <summary>An adaptive set of the map's keys.</summary>
let inline keys(mapValue: amap<'K, 'V>) : aset<'K> =
  new MapToSetNode<'K, 'V, 'K>(mapValue, fun k _ -> k)

/// <summary>
/// An adaptive list of the map's entries (FDA <c>AMap.toAList</c> parity,
/// poll node). The order is the map's iteration order, stable while the
/// map does not change.
/// </summary>
let inline toAList(mapValue: amap<'K, 'V>) : alist<'K * 'V> =
  new MapToAListNode<'K, 'V>(mapValue)

/// <summary>
/// An adaptive map of a list of entries (FDA <c>AMap.ofAList</c> parity).
/// Duplicate keys: the last entry wins.
/// </summary>
let inline ofAList(list: alist<'K * 'V>) : amap<'K, 'V> =
  new AListToMapNode<'K, 'V>(list)

/// <summary>An adaptive set of the map's distinct values (FDA <c>toASetValues</c> parity).</summary>
let inline toASetValues(mapValue: amap<'K, 'V>) : aset<'V> =
  new MapToSetNode<'K, 'V, 'V>(mapValue, fun _ v -> v)

/// <summary>
/// An adaptive map over an adaptive value of a sequence of entries. Every
/// change of the value replaces the whole state and emits the diff as the
/// delta (FDA <c>AMap.ofAVal</c> parity; the value carries no deltas).
/// </summary>
let inline ofAVal<'K, 'V, 'S when 'K: equality and 'S :> seq<'K * 'V>>
  (value: aval<'S>)
  : amap<'K, 'V> =
  new OfAvalMapNode<'K, 'V, 'S>(value)

/// <summary>
/// Adaptively maps over the given value and returns the resulting map (FDA
/// <c>AMap.bind</c> parity). When the value changes, the whole inner map is
/// swapped: the old content is removed, the inner sink is unregistered
/// eagerly, and <c>mapping</c> selects the new inner map. The inner map's
/// own changes propagate while it is bound.
/// </summary>
let inline bind (mapping: 'T -> amap<'K, 'V>) (value: aval<'T>) : amap<'K, 'V> =
  new BindMapNode<'K, 'V, 'T>(value, mapping)

/// <summary>
/// Adaptively maps over the two values and returns the resulting map (FDA
/// <c>AMap.bind2</c> parity). When either value changes, the whole inner
/// map is swapped (the bind semantics). Composed as one bind over the
/// mapped pair (the FDA approach: nested binds would miss the inner
/// bind's swap, which signals by version only, not by delta).
/// </summary>
let inline bind2
  (mapping: 'A -> 'B -> amap<'K, 'V>)
  (a: aval<'A>)
  (b: aval<'B>)
  : amap<'K, 'V> =
  bind (fun (av, bv) -> mapping av bv) (AVal.map2 (fun av bv -> (av, bv)) a b)

/// <summary>
/// Adaptively maps over the three values and returns the resulting map
/// (FDA <c>AMap.bind3</c> parity). When any value changes, the whole
/// inner map is swapped (the bind semantics).
/// </summary>
let inline bind3
  (mapping: 'A -> 'B -> 'C -> amap<'K, 'V>)
  (a: aval<'A>)
  (b: aval<'B>)
  (c: aval<'C>)
  : amap<'K, 'V> =
  bind
    (fun (av, bv, cv) -> mapping av bv cv)
    (AVal.map3 (fun av bv cv -> (av, bv, cv)) a b c)

/// <summary>
/// An adaptive map driven by a compute function (FDA <c>AMap.custom</c>
/// parity, pull model). The compute receives the current view and a delta
/// builder; it appends the operations that describe the change since the
/// previous call (for example, consuming its own event queue).
/// </summary>
let inline custom
  (compute: Dictionary<'K, 'V> -> MapDeltaBuilder<'K, 'V> -> unit)
  : amap<'K, 'V> =
  new CustomMapNode<'K, 'V>(compute)

/// <summary>
/// Creates an adaptive map from an external snapshot function and an
/// invalidate handle (FDA <c>AMap.ofExternal</c> parity). The snapshot
/// runs at most once per invalidate, on the next read, and is diffed
/// against the previous snapshot (equal values elided); not invalidated →
/// reads are O(1) and allocate nothing. The handle is O(1) to call.
/// </summary>
let inline ofExternal
  (snapshot: unit -> IReadOnlyDictionary<'K, 'V>)
  : amap<'K, 'V> * (unit -> unit) =
  let node = new ExternalMapNode<'K, 'V>(snapshot)
  (node :> amap<'K, 'V>, fun () -> node.Invalidate())

/// <summary>
/// Maps every entry, disposing the mapped value when its key leaves (FDA
/// <c>AMap.mapUse</c> parity). The mapped values are stable (the mapping
/// runs once per key). Disposing the returned disposable disposes all
/// live mapped values and clears the output map.
/// </summary>
let inline mapUse
  (mapping: 'K -> 'V -> 'W)
  (mapValue: amap<'K, 'V>)
  : IDisposable * amap<'K, 'W> =
  let node = new MapUseMapNode<'K, 'V, 'W>(mapValue, mapping)
  (node :> IDisposable, node :> amap<'K, 'W>)

/// <summary>Keeps the entries that satisfy the predicate.</summary>
let inline filter
  (predicate: 'K -> 'V -> bool)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'V> =
  new FilterMapNode<'K, 'V>(mapValue, predicate)

/// <summary>Keeps the entries whose value satisfies the predicate (FDA <c>AMap.filter'</c> parity; the V suffix is the value-only convention).</summary>
let inline filterV
  (predicate: 'V -> bool)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'V> =
  filter (fun _ v -> predicate v) mapValue

/// <summary>
/// Adaptively maps every entry of the map to an adaptive value (FDA
/// <c>AMap.mapA</c> parity). The output follows the aval returned for
/// each entry; writes to the avals deliver targeted deltas.
/// </summary>
let inline mapA
  (mapping: 'K -> 'V -> aval<'U>)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'U> =
  new ElementMapNode<'K, 'V, 'U>(
    mapValue,
    fun k v -> AVal.map ValueSome (mapping k v)
  )

/// <summary>
/// Adaptively maps every entry of the map to an adaptive value, keeping
/// only the entries whose aval holds <c>Some</c> (FDA
/// <c>AMap.chooseA</c> parity).
/// </summary>
let inline chooseA
  (mapping: 'K -> 'V -> aval<'U option>)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'U> =
  new ElementMapNode<'K, 'V, 'U>(
    mapValue,
    fun k v -> AVal.map Option.toValueOption (mapping k v)
  )

/// <summary>
/// The voption counterpart of <see cref="chooseA"/>: the mapping returns
/// <c>aval&lt;'U voption&gt;</c> directly, without the option-to-voption
/// wrapper node per entry (the no-allocation path).
/// </summary>
let inline chooseAV
  (mapping: 'K -> 'V -> aval<'U voption>)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'U> =
  new ElementMapNode<'K, 'V, 'U>(mapValue, mapping)

/// <summary>
/// Adaptively keeps the entries whose predicate aval holds <c>true</c>
/// (FDA <c>AMap.filterA</c> parity).
/// </summary>
let inline filterA
  (predicate: 'K -> 'V -> aval<bool>)
  (mapValue: amap<'K, 'V>)
  : amap<'K, 'V> =
  new ElementMapNode<'K, 'V, 'V>(
    mapValue,
    fun k v ->
      AVal.map (fun b -> if b then ValueSome v else ValueNone) (predicate k v)
  )

/// <summary>
/// Equi-joins two maps on a computed key (the map analog of
/// <c>AVal.map2</c>). The left map is enumerated per key; the join key is
/// computed from the left entry; the right map is looked up per entry
/// (never enumerated or rebuilt). The mapping receives the left key, the
/// left value as an adaptive value, and the right-side value (or
/// <c>ValueNone</c> when the join key is absent), and returns the output
/// aval; a <c>ValueNone</c> output drops the entry (choose semantics).
/// Output entries are keyed by the left key.
/// </summary>
/// <remarks>
/// The per-key subgraph is built once and updated in place: a left update
/// re-applies a swappable value cell (no rebuild, no per-frame
/// allocation), and a join-key change (rare) re-runs the mapping against
/// the new lookup. Right-map changes reach the entries through the lookup
/// (read-time gate) on the next read. A left-join is the <c>ValueNone</c>
/// case of the mapping; an inner join drops the entry when the right side
/// is <c>ValueNone</c>.
/// </remarks>
/// <remarks>
/// Argument order: the two maps come first, so the lambdas elaborate
/// after the map types are pinned — a record-field access in
/// <c>keyOfLeft</c> or the mapping resolves against the actual map types,
/// not the first record in scope with a matching field. The pipe form
/// does not apply: a piped value lands in the mapping slot, a compile
/// error.
/// </remarks>
let inline joinOn
  (left: amap<'K1, 'V1>)
  (right: amap<'K2, 'V2>)
  (keyOfLeft: 'K1 -> 'V1 -> 'K2)
  (mapping: 'K1 -> aval<'V1> -> aval<'V2 voption> -> aval<'U voption>)
  : amap<'K1, 'U> =
  new JoinMapNode<'K1, 'V1, 'K2, 'V2, 'U>(left, right, keyOfLeft, mapping)

/// <summary>
/// Adaptively reduces the map with the given <see cref="AdaptiveReduction"/>
/// over the values. The state is updated incrementally from deltas: a Set
/// on an existing key subtracts the old value, then adds the new one.
/// </summary>
let inline reduce
  (reduction: AdaptiveReduction<'a, 's, 'v>)
  (mapValue: amap<'k, 'a>)
  : aval<'v> =
  new MapReduceNode<'k, 'a, 'a, 's, 'v>(mapValue, (fun _ v -> v), reduction)

/// <summary>
/// Maps every entry, then reduces the mapped values with the given
/// <see cref="AdaptiveReduction"/>. The mapping runs per delta entry.
/// </summary>
let inline reduceBy
  (reduction: AdaptiveReduction<'b, 's, 'v>)
  (mapping: 'k -> 'a -> 'b)
  (mapValue: amap<'k, 'a>)
  : aval<'v> =
  new MapReduceNode<'k, 'a, 'b, 's, 'v>(mapValue, mapping, reduction)

/// <summary>
/// Adaptively folds the map with <c>add</c>; every removal recomputes the
/// whole fold. Use <see cref="foldGroup"/> when the operation has an inverse.
/// </summary>
let inline fold
  (add: 's -> 'k -> 'v -> 's)
  (zero: 's)
  (mapValue: amap<'k, 'v>)
  : aval<'s> =
  let mapping k v = struct (k, v)
  let add2 s struct (k, v) = add s k v

  new MapReduceNode<'k, 'v, struct ('k * 'v), 's, 's>(
    mapValue,
    mapping,
    AdaptiveReduction.fold zero add2
  )

/// <summary>
/// Adaptively folds the map with an invertible <c>subtract</c>: removals
/// update the state without a recompute.
/// </summary>
let inline foldGroup
  (add: 's -> 'k -> 'v -> 's)
  (subtract: 's -> 'k -> 'v -> 's)
  (zero: 's)
  (mapValue: amap<'k, 'v>)
  : aval<'s> =
  let inline mapping k v = struct (k, v)
  let inline add2 s struct (k, v) = add s k v
  let inline sub2 s struct (k, v) = subtract s k v

  new MapReduceNode<'k, 'v, struct ('k * 'v), 's, 's>(
    mapValue,
    mapping,
    AdaptiveReduction.group zero add2 sub2
  )

/// <summary>
/// Adaptively folds the map with a partially invertible <c>trySubtract</c>:
/// removals that cannot be inverted recompute the whole fold (FDA
/// <c>AMap.foldHalfGroup</c> parity).
/// </summary>
let inline foldHalfGroup
  (add: 's -> 'k -> 'v -> 's)
  (trySubtract: 's -> 'k -> 'v -> 's voption)
  (zero: 's)
  (mapValue: amap<'k, 'v>)
  : aval<'s> =
  let inline mapping k v = struct (k, v)
  let inline add2 s struct (k, v) = add s k v
  let inline sub2 s struct (k, v) = trySubtract s k v

  new MapReduceNode<'k, 'v, struct ('k * 'v), 's, 's>(
    mapValue,
    mapping,
    AdaptiveReduction.halfGroup zero add2 sub2
  )

/// <summary>
/// Adaptively sums the mapped entries (FDA <c>AMap.sumBy</c> parity).
/// </summary>
let inline sumBy (mapping: 'k -> 'v -> 'u) (mapValue: amap<'k, 'v>) : aval<'u> =
  reduceBy (AdaptiveReduction.sum()) mapping mapValue

/// <summary>
/// Adaptively gets the number of entries. Incremental: an update of an
/// existing key does not re-evaluate this value or its dependents.
/// </summary>
let inline count(mapValue: amap<'K, 'V>) : aval<int> =
  new MapCountNode<'K, 'V, int>(mapValue, id)

/// <summary>
/// Adaptively averages the mapped entries (needs a numeric type with
/// <c>DivideByInt</c>, e.g. <c>float</c>). The average is sum/count.
/// </summary>
let inline averageBy
  (mapping: 'k -> 'v -> ^u)
  (mapValue: amap<'k, 'v>)
  : aval< ^u > =
  AVal.map2
    (fun total c -> LanguagePrimitives.DivideByInt total c)
    (reduceBy (AdaptiveReduction.sum()) mapping mapValue)
    (count mapValue)

/// <summary>
/// Adaptively tests if the map is empty. Incremental: only a change that
/// crosses the empty/non-empty boundary re-evaluates this value or its
/// dependents.
/// </summary>
let inline isEmpty(mapValue: amap<'K, 'V>) : aval<bool> =
  new MapCountNode<'K, 'V, bool>(mapValue, fun c -> c = 0)

/// <summary>Adaptively tests if any entry satisfies the predicate.</summary>
let inline exists
  (predicate: 'K -> 'V -> bool)
  (mapValue: amap<'K, 'V>)
  : aval<bool> =
  new MapReduceNode<'K, 'V, bool, int, bool>(
    mapValue,
    predicate,
    AdaptiveReduction.countPositive |> AdaptiveReduction.mapOut(fun c -> c <> 0)
  )

/// <summary>Adaptively tests if every entry satisfies the predicate.</summary>
let inline forall
  (predicate: 'K -> 'V -> bool)
  (mapValue: amap<'K, 'V>)
  : aval<bool> =
  new MapReduceNode<'K, 'V, bool, int, bool>(
    mapValue,
    predicate,
    AdaptiveReduction.countNegative |> AdaptiveReduction.mapOut(fun c -> c = 0)
  )

/// <summary>Adaptively counts the entries that satisfy the predicate.</summary>
let inline countBy
  (predicate: 'K -> 'V -> bool)
  (mapValue: amap<'K, 'V>)
  : aval<int> =
  new MapReduceNode<'K, 'V, bool, int, int>(
    mapValue,
    predicate,
    AdaptiveReduction.countPositive
  )

// =========================================================================
// The *A reductions: composition over mapA/filterA + the existing
// reduction nodes. No new node types. FDA argument order: reduction,
// mapping, map.
// =========================================================================

/// <summary>
/// Adaptively reduces the map after mapping every entry to an adaptive
/// value (the AMap counterpart of <c>ASet.reduceByA</c>). The mapping
/// produces distinct pairs <c>struct (k, x)</c>, so duplicate mapped
/// values keep their multiplicity (a plain mapA would deduplicate them);
/// the reduction projects the value side.
/// </summary>
let inline reduceByA
  (reduction: AdaptiveReduction<'U, 's, 'v>)
  (mapping: 'K -> 'V -> aval<'U>)
  (mapValue: amap<'K, 'V>)
  : aval<'v> =
  mapValue
  |> mapA(fun k v -> AVal.map (fun x -> struct (k, x)) (mapping k v))
  |> reduceBy reduction (fun _ struct (_, x) -> x)

/// <summary>Adaptively counts the entries whose predicate aval holds <c>true</c> (the AMap counterpart of <c>ASet.countByA</c>).</summary>
let inline countByA
  (predicate: 'K -> 'V -> aval<bool>)
  (mapValue: amap<'K, 'V>)
  : aval<int> =
  mapValue |> filterA predicate |> count

/// <summary>Adaptively tests if any entry's predicate aval holds <c>true</c> (the AMap counterpart of <c>ASet.existsA</c>).</summary>
let inline existsA
  (predicate: 'K -> 'V -> aval<bool>)
  (mapValue: amap<'K, 'V>)
  : aval<bool> =
  mapValue |> countByA predicate |> AVal.map(fun c -> c <> 0)

/// <summary>Adaptively tests if every entry's predicate aval holds <c>true</c> (the AMap counterpart of <c>ASet.forallA</c>).</summary>
let inline forallA
  (predicate: 'K -> 'V -> aval<bool>)
  (mapValue: amap<'K, 'V>)
  : aval<bool> =
  mapValue
  |> filterA(fun k v -> AVal.map not (predicate k v))
  |> count
  |> AVal.map(fun c -> c = 0)

/// <summary>Adaptively sums the avals mapped from the entries (the AMap counterpart of <c>ASet.sumByA</c>).</summary>
let inline sumByA
  (mapping: 'K -> 'V -> aval<'U>)
  (mapValue: amap<'K, 'V>)
  : aval<'U> =
  reduceByA (AdaptiveReduction.sum()) mapping mapValue

/// <summary>
/// Adaptively averages the avals mapped from the entries (needs a numeric
/// type with <c>DivideByInt</c>, e.g. <c>float</c>; the AMap counterpart
/// of <c>ASet.averageByA</c>).
/// </summary>
let inline averageByA
  (mapping: 'K -> 'V -> aval<'U>)
  (mapValue: amap<'K, 'V>)
  : aval<'U> =
  AVal.map2
    (fun total count -> LanguagePrimitives.DivideByInt total count)
    (reduceByA (AdaptiveReduction.sum()) mapping mapValue)
    (count mapValue)

/// <summary>
/// Adaptively gets the minimum of the avals mapped from the entries, or
/// <c>ValueNone</c> when empty (the voption counterpart of the <c>*A</c>
/// family; AMap has no plain <c>tryMin</c>, this mirrors the ASet/AList
/// members for family symmetry).
/// </summary>
let inline tryMinA
  (mapping: 'K -> 'V -> aval<'U>)
  (mapValue: amap<'K, 'V>)
  : aval<'U voption> =
  reduceByA (AdaptiveReduction.tryMin()) mapping mapValue

/// <summary>
/// Adaptively gets the maximum of the avals mapped from the entries, or
/// <c>ValueNone</c> when empty (the voption counterpart of the <c>*A</c>
/// family; AMap has no plain <c>tryMax</c>, this mirrors the ASet/AList
/// members for family symmetry).
/// </summary>
let inline tryMaxA
  (mapping: 'K -> 'V -> aval<'U>)
  (mapValue: amap<'K, 'V>)
  : aval<'U voption> =
  reduceByA (AdaptiveReduction.tryMax()) mapping mapValue

/// <summary>
/// Adaptively looks up the key: the value, or <c>ValueNone</c> when the
/// key is absent. The lookup is per-key precise (O(1) on read) for a
/// direct changeable source: a write to an unrelated key does not
/// re-evaluate this value or its dependents. On a derived source the
/// branch re-evaluates at most once per upstream change (pull-lazy: the
/// per-key gate runs at the next read's drain).
/// </summary>
let inline tryFind (key: 'K) (mapValue: amap<'K, 'V>) : aval<'V voption> =
  new MapLookupNode<'K, 'V>(mapValue, key)

/// <summary>
/// Adaptively looks up the key. Reading the value throws
/// <see cref="KeyNotFoundException"/> when the key is absent. Per-key
/// precise, like <see cref="tryFind"/>.
/// </summary>
let inline find (key: 'K) (mapValue: amap<'K, 'V>) : aval<'V> =
  tryFind key mapValue
  |> AVal.map(fun v ->
    match v with
    | ValueSome value -> value
    | ValueNone ->
      raise(KeyNotFoundException(sprintf "could not get key: %A" key)))

/// <summary>A constant map with a single entry.</summary>
let inline single (key: 'K) (value: 'V) : amap<'K, 'V> =
  new ConstantMap<'K, 'V>(fun () -> Seq.singleton(key, value))

/// <summary>
/// Materializes the map as an adaptive value. Every change materializes a
/// new immutable copy (the retain boundary, like <see cref="force"/>); the
/// value is safe to retain.
/// </summary>
let inline toAVal(mapValue: amap<'K, 'V>) : aval<Dictionary<'K, 'V>> =
  new AdaptiveNode<Dictionary<'K, 'V>>(fun () ->
    let view = mapValue.GetValue()
    let d = Dictionary<'K, 'V>()
    let mutable e = view.GetEnumerator()

    while e.MoveNext() do
      d[e.Current.Key] <- e.Current.Value

    d)

/// <summary>
/// Returns a transient view of the current state. Valid only until the next
/// write; do not retain or mutate it. Use <see cref="force"/> to
/// materialize a snapshot that is safe to retain.
/// </summary>
let inline getValue(mapValue: amap<'K, 'V>) = mapValue.GetValue()

/// <summary>
/// Materializes the current state as an immutable copy. This is the only
/// collection operation that allocates; the result is safe to retain and
/// the library never touches it again. Runs the pending delta processing
/// (drain) first.
/// </summary>
let inline force(mapValue: amap<'K, 'V>) : Dictionary<'K, 'V> =
  let view = mapValue.GetValue()
  let d = Dictionary<'K, 'V>()
  let mutable e = view.GetEnumerator()

  while e.MoveNext() do
    d[e.Current.Key] <- e.Current.Value

  d

/// <summary>Materializes the F# <c>Map</c> counterpart (sorted, structural equality).</summary>
let inline toMap(mapValue: amap<'K, 'V>) : Map<'K, 'V> =
  let view = mapValue.GetValue()
  let mutable e = view.GetEnumerator()
  let items = ResizeArray<'K * 'V>()

  while e.MoveNext() do
    items.Add(e.Current.Key, e.Current.Value)

  items |> Seq.toList |> Map.ofList
