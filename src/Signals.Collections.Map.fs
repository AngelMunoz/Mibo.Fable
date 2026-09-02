namespace Mibo.Signals

open System
open System.Collections.Generic

// Implementation of the signature-file surface. The mutable store lives in
// CMap; everything else is a view over per-key cells. The rebuildable core
// (ofAVal/ofExternal) diffs snapshots so unchanged keys keep their cells and
// unchanged values do not notify.

// One queued boundary intent of the changeable map. A posted replace
// supersedes every intent queued before it; later intents still apply on
// top of the replace.
type internal MapPostOp<'K, 'V> =
  | MapPostEntry of struct ('K * 'V voption)
  | MapPostClear
  | MapPostReplace of ('K * 'V) array

[<RequireQualifiedAccess>]
module internal MapInternals =

  /// Builds a plain Map from a seq of entries (later duplicates win).
  let entriesToMap(items: seq<'K * 'V>) : Map<'K, 'V> =
    items |> Seq.fold (fun acc (k, v) -> Map.add k v acc) Map.empty

  /// Reads the current cells, flushing the owner's posted intents first.
  let readCells(map: AMap<'K, 'V>) : Map<'K, AVal<'V voption>> =
    map.OnRead()
    let struct (_, cs) = AVal.get map.State
    cs

  /// Materializes the current cells into a plain Map. No pack guard: the
  /// delta sinks run during effect flushes, outside batches.
  let materialize(map: AMap<'K, 'V>) : Map<'K, 'V> =
    readCells map
    |> Map.fold
      (fun acc k cell ->
        match AVal.get cell with
        | ValueSome v -> Map.add k v acc
        | ValueNone -> acc)
      Map.empty

  /// Applies a snapshot over the previous cell map: dropped keys leave,
  /// kept cells reuse and write only changed values, new keys get fresh
  /// cells. Returns the new cell map.
  let diffCells
    (previous: Map<'K, AVal<'V voption>>)
    (snapshot: Map<'K, 'V>)
    : Map<'K, AVal<'V voption>> =
    let kept =
      Map.fold
        (fun acc k cell ->
          match Map.tryFind k snapshot with
          | Some v ->
            if AVal.peek cell <> ValueSome v then
              cell.value <- ValueSome v

            Map.add k cell acc
          | None -> acc)
        Map.empty
        previous

    Map.fold
      (fun acc k v ->
        if Map.containsKey k acc then
          acc
        else
          Map.add k (CVal.create(ValueSome v)) acc)
      kept
      snapshot

  /// Builds fresh cells from a snapshot.
  let cellsOf(snapshot: Map<'K, 'V>) : Map<'K, AVal<'V voption>> =
    snapshot
    |> Map.fold
      (fun acc k v -> Map.add k (CVal.create(ValueSome v)) acc)
      Map.empty

[<Class>]
type CMap<'K, 'V when 'K: comparison and 'V: equality>(items: seq<'K * 'V>) =

  let mutable cells: Map<'K, AVal<'V voption>> = Map.empty
  let mutable version = 0L
  let root = CVal.create 0L

  let state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)> =
    AVal.computed(fun () ->
      AVal.get root |> ignore
      struct (version, cells))

  let posts = ResizeArray<MapPostOp<'K, 'V>>()
  let mutable flushing = false

  let bump() =
    version <- version + 1L
    CVal.set version root

  let dropAll() =
    if not(Map.isEmpty cells) then
      cells <- Map.empty
      bump()

  let applyEntry (k: 'K) (vopt: 'V voption) : bool =
    match vopt, Map.tryFind k cells with
    | ValueSome v, Some cell ->
      if AVal.peek cell <> ValueSome v then
        cell.value <- ValueSome v
        true
      else
        false
    | ValueSome v, None ->
      cells <- Map.add k (CVal.create(ValueSome v)) cells
      bump()
      true
    | ValueNone, Some _ ->
      cells <- Map.remove k cells
      bump()
      true
    | ValueNone, None -> false

  let replaceWith(items: ('K * 'V) array) =
    dropAll()
    items |> Array.iter(fun (k, v) -> applyEntry k (ValueSome v) |> ignore)

  let flushPosts() =
    if posts.Count > 0 && not flushing then
      flushing <- true

      try
        let ops = Array.ofSeq posts
        posts.Clear()

        // The last posted replace supersedes every intent before it.
        let mutable lastReplace = -1

        for i in 0 .. ops.Length - 1 do
          match ops.[i] with
          | MapPostReplace _ -> lastReplace <- i
          | _ -> ()

        for i in 0 .. ops.Length - 1 do
          if i >= lastReplace then
            match ops.[i] with
            | MapPostEntry(k, v) -> applyEntry k v |> ignore
            | MapPostClear -> dropAll()
            | MapPostReplace items -> replaceWith items
      finally
        flushing <- false

  let amap = AMap(state, OnRead = flushPosts)

  do
    cells <-
      items
      |> Seq.fold
        (fun acc (k, v) -> Map.add k (CVal.create(ValueSome v)) acc)
        Map.empty

  new() = CMap(Seq.empty)

  member _.Value: AMap<'K, 'V> = amap

  /// Applies the pending boundary intents now.
  member _.FlushPosts() = flushPosts()

  member _.Add(key, value) =
    flushPosts()

    if Map.containsKey key cells then
      false
    else
      applyEntry key (ValueSome value) |> ignore
      true

  member _.Set(key, value) =
    flushPosts()

    if Map.containsKey key cells then
      applyEntry key (ValueSome value) |> ignore
      true
    else
      false

  member _.AddOrUpdate(key, value) =
    flushPosts()
    applyEntry key (ValueSome value)

  member _.Remove(key) =
    flushPosts()
    applyEntry key ValueNone

  member _.Clear() =
    flushPosts()
    dropAll()

  member _.SetAll(items: seq<'K * 'V>) =
    flushPosts()
    replaceWith(Array.ofSeq items)

  member _.UpdateTo(items: seq<'K * 'V>) =
    flushPosts()
    let target = MapInternals.entriesToMap items
    let mutable changed = Map.count cells <> Map.count target

    if not changed then
      let mutable found = false

      for KeyValue(k, cell) in cells do
        if found then
          ()
        else
          match Map.tryFind k target with
          | None -> found <- true
          | Some t ->
            if AVal.peek cell <> ValueSome t then
              found <- true

      changed <- found

    if changed then
      replaceWith(Array.ofSeq items)

    changed

  member _.PostAddOrUpdate(key, value) =
    posts.Add(MapPostEntry(struct (key, ValueSome value)))

  member _.PostRemove(key) =
    posts.Add(MapPostEntry(struct (key, ValueNone)))

  member _.PostClear() = posts.Add(MapPostClear)

  member _.PostSet(items: seq<'K * 'V>) =
    posts.Add(MapPostReplace(Array.ofSeq items))

  member _.ContainsKey(key) =
    flushPosts()
    Map.containsKey key cells

  member _.TryGetValue(key) =
    flushPosts()

    match Map.tryFind key cells with
    | Some cell -> AVal.peek cell
    | None -> ValueNone

  member this.Item
    with get (key: 'K): 'V =
      match this.TryGetValue(key) with
      | ValueSome v -> v
      | ValueNone ->
        raise(KeyNotFoundException(sprintf "could not get key: %O" key))

[<RequireQualifiedAccess>]
module AMap =

  let empty<'K, 'V when 'K: comparison> : AMap<'K, 'V> =
    AMap(AVal.computed(fun () -> struct (0L, Map.empty)))

  let constant(create: unit -> Map<'K, 'V>) : AMap<'K, 'V> =
    let mutable cache = ValueNone

    AMap(
      AVal.computed(fun () ->
        match cache with
        | ValueSome s -> s
        | ValueNone ->
          let s = struct (0L, MapInternals.cellsOf(create()))
          cache <- ValueSome s
          s)
    )

  let delay create = constant create

  let ofCells(cells: Map<'K, AVal<'V voption>>) : AMap<'K, 'V> =
    AMap(AVal.computed(fun () -> struct (0L, cells)))

  let ofSeq(items: seq<'K * 'V>) : AMap<'K, 'V> =
    items
    |> Seq.fold
      (fun acc (k, v) -> Map.add k (CVal.create(ValueSome v)) acc)
      Map.empty
    |> ofCells

  let ofArray(items: ('K * 'V) array) : AMap<'K, 'V> = ofSeq items

  let ofList(items: ('K * 'V) list) : AMap<'K, 'V> = ofSeq items

  let ofMap(items: Map<'K, 'V>) : AMap<'K, 'V> =
    items |> Seq.map(fun kv -> kv.Key, kv.Value) |> ofSeq

  let single(key: 'K, value: 'V) : AMap<'K, 'V> = ofSeq [ (key, value) ]

  let ofAVal(value: AVal<seq<'K * 'V>>) : AMap<'K, 'V> =
    let mutable version = 0L
    let mutable previous: Map<'K, AVal<'V voption>> = Map.empty
    let mutable primed = false

    let state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)> =
      AVal.computed(fun () ->
        let snapshot = value |> AVal.get |> MapInternals.entriesToMap

        previous <-
          if primed then
            MapInternals.diffCells previous snapshot
          else
            MapInternals.cellsOf snapshot

        primed <- true
        version <- version + 1L
        struct (version, previous))

    AMap(state)

  let ofExternal
    (snapshot: unit -> Map<'K, 'V>)
    : AMap<'K, 'V> * (unit -> unit) =
    let mutable version = 0L
    let mutable previous: Map<'K, AVal<'V voption>> = Map.empty
    let mutable primed = false
    let invalidations = CVal.create 0L

    let state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)> =
      AVal.computed(fun () ->
        AVal.get invalidations |> ignore

        previous <-
          if primed then
            // The snapshot runs at most once per invalidate, on the next
            // read: this computed re-runs only when the handle fired.
            MapInternals.diffCells previous (snapshot())
          else
            primed <- true
            MapInternals.cellsOf(snapshot())

        version <- version + 1L
        struct (version, previous))

    let invalidate() =
      CVal.set (AVal.peek invalidations + 1L) invalidations

    (AMap(state), invalidate)

  let observe
    (sink: MapDelta<'K, 'V> -> unit)
    (map: AMap<'K, 'V>)
    : IDisposable =
    let mutable prev = ValueNone

    let disposer =
      Signals.effect(fun () ->
        let next = MapInternals.materialize map

        match prev with
        | ValueNone ->
          // Registration delivers the initial content as one net delta.
          sink {
            Sets = Map.toArray next
            Removes = [||]
          }
        | ValueSome p ->
          let sets = ResizeArray<'K * 'V>()
          let rems = ResizeArray<'K>()

          next
          |> Map.iter(fun k v ->
            match Map.tryFind k p with
            | Some old when old = v -> ()
            | _ -> sets.Add(k, v))

          p
          |> Map.iter(fun k _ ->
            if not(Map.containsKey k next) then
              rems.Add k)

          if sets.Count > 0 || rems.Count > 0 then
            sink {
              Sets = Array.ofSeq sets
              Removes = Array.ofSeq rems
            }

        prev <- ValueSome next)

    Unsubscriber(disposer)

  let force(map: AMap<'K, 'V>) : Map<'K, 'V> =
    Collections.guardPack "AMap.force"
    MapInternals.materialize map

  let getValue map = force map

  let toMap map = force map

  let toSeq(map: AMap<'K, 'V>) : seq<'K * 'V> =
    force map |> Seq.map(fun kv -> kv.Key, kv.Value)

  let toAVal(map: AMap<'K, 'V>) : AVal<Map<'K, 'V>> =
    AVal.computed(fun () ->
      Collections.guardPack "AMap.toAVal"
      MapInternals.materialize map)

  let count(map: AMap<'K, 'V>) : AVal<int> =
    AVal.computed(fun () ->
      map.OnRead()
      let struct (_, cs) = AVal.get map.State
      Map.count cs)

  let isEmpty(map: AMap<'K, 'V>) : AVal<bool> =
    count map |> AVal.map(fun c -> c = 0)

  let containsKey (key: 'K) (map: AMap<'K, 'V>) : AVal<bool> =
    AVal.computed(fun () ->
      map.OnRead()
      let struct (_, cs) = AVal.get map.State
      Map.containsKey key cs)

  let tryFind (key: 'K) (map: AMap<'K, 'V>) : AVal<'V voption> =
    AVal.computed(fun () ->
      map.OnRead()
      let struct (_, cs) = AVal.get map.State

      match Map.tryFind key cs with
      | Some cell -> AVal.get cell
      | None -> ValueNone)

  let find (key: 'K) (map: AMap<'K, 'V>) : AVal<'V> =
    tryFind key map
    |> AVal.map(fun v ->
      match v with
      | ValueSome value -> value
      | ValueNone ->
        raise(KeyNotFoundException(sprintf "could not get key: %O" key)))

  let internal coarse(map: AMap<'K, 'V>) : Map<'K, 'V> =
    map.OnRead()
    let struct (_, cs) = AVal.get map.State

    cs
    |> Seq.fold
      (fun acc kv ->
        match AVal.get kv.Value with
        | ValueSome v -> Map.add kv.Key v acc
        | ValueNone -> acc)
      Map.empty

  let exists (predicate: 'K -> 'V -> bool) (map: AMap<'K, 'V>) : AVal<bool> =
    AVal.computed(fun () -> coarse map |> Map.exists predicate)

  let forall (predicate: 'K -> 'V -> bool) (map: AMap<'K, 'V>) : AVal<bool> =
    AVal.computed(fun () -> coarse map |> Map.forall predicate)

  let countBy (predicate: 'K -> 'V -> bool) (map: AMap<'K, 'V>) : AVal<int> =
    AVal.computed(fun () ->
      coarse map
      |> Seq.filter(fun kv -> predicate kv.Key kv.Value)
      |> Seq.length)

  let fold
    (add: 's -> 'K -> 'V -> 's)
    (zero: 's)
    (map: AMap<'K, 'V>)
    : AVal<'s> =
    AVal.computed(fun () ->
      coarse map |> Map.fold (fun s k v -> add s k v) zero)

  let foldGroup
    (add: 's -> 'K -> 'V -> 's)
    (_subtract: 's -> 'K -> 'V -> 's)
    (zero: 's)
    (map: AMap<'K, 'V>)
    : AVal<'s> =
    fold add zero map

  let foldHalfGroup
    (add: 's -> 'K -> 'V -> 's)
    (_trySubtract: 's -> 'K -> 'V -> 's voption)
    (zero: 's)
    (map: AMap<'K, 'V>)
    : AVal<'s> =
    fold add zero map

  let keys(map: AMap<'K, 'V>) : ASet<'K> =
    ASet(
      AVal.computed(fun () ->
        map.OnRead()
        let struct (v, cs) = AVal.get map.State
        struct (v, Map.fold (fun s k _ -> Set.add k s) Set.empty cs))
    )

  let toASet(map: AMap<'K, 'V>) : ASet<struct ('K * 'V)> =
    ASet(
      AVal.computed(fun () ->
        map.OnRead()
        let struct (v, cs) = AVal.get map.State
        let mutable s = Set.empty

        for KeyValue(k, cell) in cs do
          match AVal.get cell with
          | ValueSome value -> s <- Set.add (struct (k, value)) s
          | ValueNone -> ()

        struct (v, s))
    )

  let toASetValues(map: AMap<'K, 'V>) : ASet<'V> =
    ASet(
      AVal.computed(fun () ->
        map.OnRead()
        let struct (v, cs) = AVal.get map.State
        let mutable s = Set.empty

        for KeyValue(_, cell) in cs do
          match AVal.get cell with
          | ValueSome value -> s <- Set.add value s
          | ValueNone -> ()

        struct (v, s))
    )

  let toAList(map: AMap<'K, 'V>) : AList<'K * 'V> =
    // The entry cells are shared with the map: a value write does not
    // rebuild the list, and the iteration order is stable per structure.
    AList(
      AVal.computed(fun () ->
        map.OnRead()
        let struct (v, cs) = AVal.get map.State

        let order = Array.zeroCreate(Map.count cs)
        let mutable byId = Map.empty
        let mutable i = 0

        for KeyValue(k, cell) in cs do
          order.[i] <- i

          byId <-
            Map.add
              i
              (AVal.map
                (fun vo ->
                  match vo with
                  | ValueSome v -> ValueSome(k, v)
                  | ValueNone -> ValueNone)
                cell)
              byId

          i <- i + 1

        struct (v, ListCells(order, byId)))
    )

[<RequireQualifiedAccess>]
module CMap =

  let empty<'K, 'V when 'K: comparison and 'V: equality> : CMap<'K, 'V> = CMap()

  let ofSeq(items: seq<'K * 'V>) : CMap<'K, 'V> = CMap(items)

  let add key value (map: CMap<'K, 'V>) = map.Add(key, value)

  let set key value (map: CMap<'K, 'V>) = map.Set(key, value)

  let addOrUpdate key value (map: CMap<'K, 'V>) = map.AddOrUpdate(key, value)

  let remove key (map: CMap<'K, 'V>) = map.Remove(key)

  let clear(map: CMap<'K, 'V>) = map.Clear()

  let setAll items (map: CMap<'K, 'V>) = map.SetAll(items)

  let postAddOrUpdate key value (map: CMap<'K, 'V>) =
    map.PostAddOrUpdate(key, value)

  let postRemove key (map: CMap<'K, 'V>) = map.PostRemove(key)

  let postClear(map: CMap<'K, 'V>) = map.PostClear()

  let postSet items (map: CMap<'K, 'V>) = map.PostSet(items)

  let value(map: CMap<'K, 'V>) : AMap<'K, 'V> = map.Value

  let force(map: CMap<'K, 'V>) : Map<'K, 'V> =
    Collections.guardPack "CMap.force"
    map.FlushPosts()
    MapInternals.materialize map.Value

  let toMap map = force map

  let containsKey key (map: CMap<'K, 'V>) = map.ContainsKey(key)

  let tryGetValue key (map: CMap<'K, 'V>) = map.TryGetValue(key)

  let item key (map: CMap<'K, 'V>) = map.[key]
