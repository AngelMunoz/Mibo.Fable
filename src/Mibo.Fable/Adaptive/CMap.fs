module Mibo.Fable.Adaptive.CMap

open System
open System.Collections.Generic

/// <summary>An empty changeable map.</summary>
let inline empty<'K, 'V when 'K: equality> = new cmap<'K, 'V>(Seq.empty)

/// <summary>A changeable map with the given entries.</summary>
let inline ofSeq(items: seq<'K * 'V>) = new cmap<'K, 'V>(items)

/// <summary>Adds or updates an entry. No-op when the value is unchanged.</summary>
let inline addOrUpdate (key: 'K) (value: 'V) (mapValue: cmap<'K, 'V>) =
  mapValue.AddOrUpdate key value

/// <summary>Removes an entry. No-op when absent.</summary>
let inline remove (key: 'K) (mapValue: cmap<'K, 'V>) = mapValue.Remove key

/// <summary>
/// Posts an add or update (the <c>cval.Post</c> handoff pattern): queues
/// the operation and returns immediately. The queued operations apply at
/// the next graph operation (reads and writes auto-drain) or at
/// <c>Posting.pump</c>, as one batch: one net delta, one notification
/// delivery. A burst is coalesced into a single handoff.
/// </summary>
let inline postAddOrUpdate (key: 'K) (value: 'V) (mapValue: cmap<'K, 'V>) =
  mapValue.PostAddOrUpdate key value

/// <summary>Posts a remove. See <see cref="postAddOrUpdate"/> for the application contract.</summary>
let inline postRemove (key: 'K) (mapValue: cmap<'K, 'V>) =
  mapValue.PostRemove key

/// <summary>
/// Posts a full replace. See <see cref="postAddOrUpdate"/> for the
/// application contract; a posted replace supersedes the other ops of the
/// same pending batch (the transaction semantics of <see cref="set"/>).
/// The content is passed as a seq so the posting side allocates nothing:
/// convert a <c>Map</c> with <c>Map.toSeq</c> at the call site when needed.
/// </summary>
let inline postSet (value: seq<'K * 'V>) (mapValue: cmap<'K, 'V>) =
  mapValue.PostSet value

/// <summary>
/// Posts a clear (a full replace with the empty map). See
/// <see cref="postAddOrUpdate"/> for the application contract.
/// </summary>
let inline postClear(mapValue: cmap<'K, 'V>) = mapValue.PostClear()

/// <summary>Replaces the whole map.</summary>
let inline set (value: Map<'K, 'V>) (mapValue: cmap<'K, 'V>) =
  mapValue.Set(Map.toSeq value)

/// <summary>Tests whether the key is present (FDA <c>cmap.ContainsKey</c> parity).</summary>
let inline containsKey (key: 'K) (mapValue: cmap<'K, 'V>) : bool =
  (AMap.getValue mapValue).ContainsKey key

/// <summary>Gets the value for the key, or <c>ValueNone</c> when absent (FDA <c>cmap.TryGetValue</c> parity).</summary>
let inline tryGetValue (key: 'K) (mapValue: cmap<'K, 'V>) : 'V voption =
  let view = AMap.getValue mapValue
  let mutable v = Unchecked.defaultof<'V>

  if view.TryGetValue(key, &v) then ValueSome v else ValueNone

/// <summary>Gets the value for the key (FDA <c>cmap.Item</c> parity; <see cref="KeyNotFoundException"/> when absent).</summary>
let inline item (key: 'K) (mapValue: cmap<'K, 'V>) : 'V =
  (AMap.getValue mapValue)[key]

/// <summary>
/// Replaces the whole map and returns whether the content changed (FDA
/// <c>cmap.UpdateTo</c> parity; deviation: FDA merges with init/update, we
/// replace, matching <see cref="set"/>). An equal target marks nothing.
/// </summary>
let inline updateTo (target: seq<'K * 'V>) (mapValue: cmap<'K, 'V>) : bool =
  let targetMap = Dictionary<'K, 'V>()

  for k, v in target do
    targetMap[k] <- v

  let view = AMap.getValue mapValue
  let mutable changed = view.Count <> targetMap.Count

  if not changed then
    let mutable e = view.GetEnumerator()

    while e.MoveNext() do
      let k = e.Current.Key
      let v = e.Current.Value
      let mutable t = Unchecked.defaultof<'V>

      if
        not(targetMap.TryGetValue(k, &t))
        || not(EqualityComparer<'V>.Default.Equals(t, v))
      then
        changed <- true

  if changed then
    mapValue.Set target

  changed

/// <summary>
/// Applies a batch of map operations (FDA <c>cmap.Perform</c> parity). The
/// batch is applied atomically: sinks receive one net delta.
/// </summary>
let perform (delta: MapDeltaBuilder<'K, 'V>) (mapValue: cmap<'K, 'V>) : unit =
  let d = delta.Snapshot()

  if not d.IsEmpty then
    Transaction.run(fun () ->
      let sets = d.SetEntries

      for i in 0 .. d.SetCount - 1 do
        let struct (k, v) = sets[i]
        mapValue.AddOrUpdate k v |> ignore

      let rems = d.RemovedKeys

      for i in 0 .. d.RemovedCount - 1 do
        mapValue.Remove rems[i] |> ignore)

/// <summary>Removes all entries (FDA <c>cmap.Clear</c> parity; one atomic batch).</summary>
let inline clear(mapValue: cmap<'K, 'V>) : unit =
  Transaction.run(fun () ->
    let view = AMap.getValue mapValue
    let keys = ResizeArray<'K>()
    let mutable e = view.GetEnumerator()

    while e.MoveNext() do
      keys.Add e.Current.Key

    for k in keys do
      mapValue.Remove k |> ignore)

/// <summary>Views the changeable map as an adaptive map.</summary>
let inline value(mapValue: cmap<'K, 'V>) : amap<'K, 'V> = mapValue

/// <summary>Materializes the current state as an immutable snapshot.</summary>
let inline force(mapValue: cmap<'K, 'V>) : Dictionary<'K, 'V> =
  AMap.force mapValue

/// <summary>Materializes the F# <c>Map</c> counterpart.</summary>
let inline toMap(mapValue: cmap<'K, 'V>) : Map<'K, 'V> = AMap.toMap mapValue
