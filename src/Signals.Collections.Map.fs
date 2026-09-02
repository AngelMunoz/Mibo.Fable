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

  /// Replaces the whole content with the given entries.
  member _.Set(items: seq<'K * 'V>) =
    flushPosts()
    replaceWith(Array.ofSeq items)

  /// Inserts or overwrites. No-op when the value is unchanged.
  member _.AddOrUpdate(key, value) =
    flushPosts()
    applyEntry key (ValueSome value) |> ignore

  /// Removes a key. No-op when absent.
  member _.Remove(key) =
    flushPosts()
    applyEntry key ValueNone |> ignore

  member _.Clear() =
    flushPosts()
    dropAll()

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
module internal MapDerive =

  /// Re-derives the cell map from the source inside one computed: the
  /// derived structure tracks the source version, so it recomputes only
  /// when the source actually moved. Cells are rebuilt from the derived
  /// snapshot (coarse granularity — the same cost class as Mibo.Adaptive's
  /// filter).
  let from
    (snapshot: Map<'K, 'V>)
    (version: int64)
    : AVal<struct (int64 * Map<'K, AVal<'V voption>>)> =
    AVal.computed(fun () -> struct (version, MapInternals.cellsOf snapshot))

  /// Coarse read of the source: version plus plain snapshot.
  let read(map: AMap<'K, 'V>) : struct (int64 * Map<'K, 'V>) =
    map.OnRead()
    let struct (v, cs) = AVal.get map.State

    let snapshot =
      Map.fold
        (fun acc k cell ->
          match AVal.get cell with
          | ValueSome v -> Map.add k v acc
          | ValueNone -> acc)
        Map.empty
        cs

    struct (v, snapshot)

  let derive
    (f: Map<'K, 'V> -> Map<'K2, 'V2>)
    (map: AMap<'K, 'V>)
    : AMap<'K2, 'V2> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = read map
        struct (v, MapInternals.cellsOf(f snapshot)))
    )

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
    (subtract: 's -> 'K -> 'V -> 's)
    (zero: 's)
    (map: AMap<'K, 'V>)
    : AVal<'s> =
    fold add zero map

  let foldHalfGroup
    (add: 's -> 'K -> 'V -> 's)
    (trySubtract: 's -> 'K -> 'V -> 's voption)
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


  let custom
    (compute: Map<'K, 'V> -> MapDeltaBuilder<'K, 'V> -> unit)
    : AMap<'K, 'V> =
    // Pull model, Mibo.Adaptive CustomMapNode parity: the compute runs on
    // every read (via the poll hook) and appends the operations that
    // describe the change since its previous run; the node applies them.
    let mutable cells: Map<'K, AVal<'V voption>> = Map.empty
    let mutable version = 0L
    let root = CVal.create 0L

    let state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)> =
      AVal.computed(fun () ->
        AVal.get root |> ignore
        struct (version, cells))

    let poll() =
      let builder = MapDeltaBuilder<'K, 'V>()

      let view =
        Map.fold
          (fun acc k cell ->
            match AVal.peek cell with
            | ValueSome v -> Map.add k v acc
            | ValueNone -> acc)
          Map.empty
          cells

      compute view builder

      let struct (sets, removes) = struct (builder.Sets, builder.Removes)

      let mutable changed = sets.Length > 0 || removes.Length > 0

      if changed then
        for struct (k, v) in sets do
          match Map.tryFind k cells with
          | Some cell ->
            if AVal.peek cell <> ValueSome v then
              cell.value <- ValueSome v
          | None -> cells <- Map.add k (CVal.create(ValueSome v)) cells

        for k in removes do
          cells <- Map.remove k cells

        version <- version + 1L
        CVal.set version root

    AMap(state, OnRead = poll)

  let ofASet (keys: ASet<'K>) (mapping: 'K -> 'V) : AMap<'K, 'V> =
    AMap(
      AVal.computed(fun () ->
        keys.OnRead()
        let struct (v, s) = AVal.get keys.State

        let cells =
          Set.fold
            (fun acc k -> Map.add k (CVal.create(ValueSome(mapping k))) acc)
            Map.empty
            s

        struct (v, cells))
    )

  let ofASetIgnoreDuplicates keys mapping = ofASet keys mapping

  let ofASetMapped mapping (keys: ASet<'K>) : AMap<'K, 'V> = ofASet keys mapping

  let ofASetMappedIgnoreDuplicates mapping keys = ofASetMapped mapping keys

  let ofAList(entries: AList<'K * 'V>) : AMap<'K, 'V> =
    AMap(
      AVal.computed(fun () ->
        entries.OnRead()
        let struct (v, listCells) = AVal.get entries.State
        let mutable cells = Map.empty

        for id in listCells.Order do
          match Map.tryFind id listCells.ById with
          | Some cell ->
            match AVal.peek cell with
            | ValueSome(k, value) ->
              cells <- Map.add k (CVal.create(ValueSome value)) cells
            | ValueNone -> ()
          | None -> ()

        struct (v, cells))
    )

  let map (mapping: 'K -> 'V -> 'M) (map: AMap<'K, 'V>) : AMap<'K, 'M> =
    AMap(
      AVal.computed(fun () ->
        map.OnRead()
        let struct (v, cs) = AVal.get map.State
        // Per-key mapping: each derived cell tracks only its own source
        // cell, so a value write recomputes just that key's mapping.
        let cells =
          Map.fold
            (fun acc k cell ->
              acc
              |> Map.add
                k
                (AVal.computed(fun () ->
                  match AVal.get cell with
                  | ValueSome v -> ValueSome(mapping k v)
                  | ValueNone -> ValueNone)))
            Map.empty
            cs

        struct (v, cells))
    )

  let mapA (mapping: 'K -> 'V -> AVal<'M>) (map: AMap<'K, 'V>) : AMap<'K, 'M> =
    AMap(
      AVal.computed(fun () ->
        map.OnRead()
        let struct (v, cs) = AVal.get map.State

        let cells =
          Map.fold
            (fun acc k cell ->
              acc
              |> Map.add
                k
                (AVal.computed(fun () ->
                  match AVal.get cell with
                  | ValueSome v -> ValueSome(AVal.get(mapping k v))
                  | ValueNone -> ValueNone)))
            Map.empty
            cs

        struct (v, cells))
    )

  let mapV (mapping: 'V -> 'M) (source: AMap<'K, 'V>) : AMap<'K, 'M> =
    map (fun _ v -> mapping v) source

  let mapSet (mapping: 'K -> 'V -> Set<'M>) (map: AMap<'K, 'V>) : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map
        let mutable s = Set.empty

        for KeyValue(k, value) in snapshot do
          s <- Set.union s (mapping k value)

        struct (v, s))
    )

  let filter (predicate: 'K -> 'V -> bool) (map: AMap<'K, 'V>) : AMap<'K, 'V> =
    MapDerive.derive (Map.filter predicate) map

  let filterA
    (predicate: 'K -> 'V -> AVal<bool>)
    (map: AMap<'K, 'V>)
    : AMap<'K, 'V> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map

        let cells =
          snapshot
          |> Map.fold
            (fun acc k value ->
              acc
              |> Map.add
                k
                (AVal.computed(fun () ->
                  if AVal.get(predicate k value) then
                    ValueSome value
                  else
                    ValueNone)))
            Map.empty

        struct (v, cells))
    )

  let filterV predicate map = filterA predicate map

  let choose
    (chooser: 'K -> 'V -> 'M option)
    (map: AMap<'K, 'V>)
    : AMap<'K, 'M> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map
        let mutable cells = Map.empty

        for KeyValue(k, value) in snapshot do
          match chooser k value with
          | Some m -> cells <- Map.add k (CVal.create(ValueSome m)) cells
          | None -> ()

        struct (v, cells))
    )

  let chooseA
    (chooser: 'K -> 'V -> AVal<'M option>)
    (map: AMap<'K, 'V>)
    : AMap<'K, 'M> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map

        let cells =
          snapshot
          |> Map.fold
            (fun acc k value ->
              acc
              |> Map.add
                k
                (AVal.computed(fun () ->
                  match AVal.get(chooser k value) with
                  | Some m -> ValueSome m
                  | None -> ValueNone)))
            Map.empty

        struct (v, cells))
    )

  let chooseAV chooser map = chooseA chooser map

  let chooseV (chooser: 'V -> 'M option) (map: AMap<'K, 'V>) : AMap<'K, 'M> =
    choose (fun _ v -> chooser v) map

  let choose2
    (chooser: 'K -> 'V1 voption -> 'V2 voption -> 'M voption)
    (map1: AMap<'K, 'V1>)
    (map2: AMap<'K, 'V2>)
    : AMap<'K, 'M> =
    AMap(
      AVal.computed(fun () ->
        map1.OnRead()
        map2.OnRead()
        let struct (v1, cs1) = AVal.get map1.State
        let struct (v2, cs2) = AVal.get map2.State
        // Outer merge: the version tracks both structures.
        let version = max v1 v2
        let mutable keys = Set.empty

        for KeyValue(k, _) in cs1 do
          keys <- Set.add k keys

        for KeyValue(k, _) in cs2 do
          keys <- Set.add k keys

        let mutable cells = Map.empty

        for k in keys do
          let a =
            match Map.tryFind k cs1 with
            | Some cell -> AVal.peek cell
            | None -> ValueNone

          let b =
            match Map.tryFind k cs2 with
            | Some cell -> AVal.peek cell
            | None -> ValueNone

          match chooser k a b with
          | ValueSome m -> cells <- Map.add k (CVal.create(ValueSome m)) cells
          | ValueNone -> ()

        struct (version, cells))
    )

  let choose2V
    (chooser: 'K -> 'V1 -> 'V2 -> 'M voption)
    (map1: AMap<'K, 'V1>)
    (map2: AMap<'K, 'V2>)
    : AMap<'K, 'M> =
    choose2
      (fun k a b ->
        match a, b with
        | ValueSome x, ValueSome y -> chooser k x y
        | _ -> ValueNone)
      map1
      map2

  let bind
    (binder: 'K -> 'V -> AMap<'K2, 'V2>)
    (map: AMap<'K, 'V>)
    : AMap<'K2, 'V2> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map
        let mutable cells = Map.empty

        // Union of the inner maps; later entries win on key collisions.
        for KeyValue(k, value) in snapshot do
          let inner = binder k value
          inner.OnRead()
          let struct (_, innerCells) = AVal.get inner.State

          for KeyValue(ik, icell) in innerCells do
            match AVal.peek icell with
            | ValueSome iv ->
              cells <- Map.add ik (CVal.create(ValueSome iv)) cells
            | ValueNone -> ()

        struct (v, cells))
    )

  let bind2
    (binder: 'K -> 'V1 -> 'V2 -> AMap<'K2, 'R>)
    (map1: AMap<'K, 'V1>)
    (map2: AMap<'K, 'V2>)
    : AMap<'K2, 'R> =
    AMap(
      AVal.computed(fun () ->
        let struct (v1, snapshot1) = MapDerive.read map1
        let struct (v2, snapshot2) = MapDerive.read map2
        let mutable cells = Map.empty

        for KeyValue(k, a) in snapshot1 do
          match Map.tryFind k snapshot2 with
          | Some b ->
            let inner = binder k a b
            inner.OnRead()
            let struct (_, innerCells) = AVal.get inner.State

            for KeyValue(ik, icell) in innerCells do
              match AVal.peek icell with
              | ValueSome iv ->
                cells <- Map.add ik (CVal.create(ValueSome iv)) cells
              | ValueNone -> ()
          | None -> ()

        struct (max v1 v2, cells))
    )

  let bind3
    (binder: 'K -> 'V1 -> 'V2 -> 'V3 -> AMap<'K2, 'R>)
    (map1: AMap<'K, 'V1>)
    (map2: AMap<'K, 'V2>)
    (map3: AMap<'K, 'V3>)
    : AMap<'K2, 'R> =
    AMap(
      AVal.computed(fun () ->
        let struct (v1, snapshot1) = MapDerive.read map1
        let struct (v2, snapshot2) = MapDerive.read map2
        let struct (v3, snapshot3) = MapDerive.read map3
        let mutable cells = Map.empty

        for KeyValue(k, a) in snapshot1 do
          match Map.tryFind k snapshot2, Map.tryFind k snapshot3 with
          | Some b, Some c ->
            let inner = binder k a b c
            inner.OnRead()
            let struct (_, innerCells) = AVal.get inner.State

            for KeyValue(ik, icell) in innerCells do
              match AVal.peek icell with
              | ValueSome iv ->
                cells <- Map.add ik (CVal.create(ValueSome iv)) cells
              | ValueNone -> ()
          | _ -> ()

        struct (max v1 (max v2 v3), cells))
    )

  let joinOn
    (keyOf: 'V1 -> 'M)
    (outer: AMap<'K, 'V1>)
    (inner: AMap<'M, 'V2>)
    : AMap<'K, struct ('V1 * 'V2)> =
    AMap(
      AVal.computed(fun () ->
        outer.OnRead()
        inner.OnRead()
        let struct (v1, outerCells) = AVal.get outer.State
        let struct (v2, innerCells) = AVal.get inner.State
        let version = max v1 v2
        let mutable cells = Map.empty

        for KeyValue(k, cell) in outerCells do
          match AVal.peek cell with
          | ValueSome value ->
            match Map.tryFind (keyOf value) innerCells with
            | Some target ->
              match AVal.peek target with
              | ValueSome targetValue ->
                cells <-
                  Map.add
                    k
                    (CVal.create(ValueSome(struct (value, targetValue))))
                    cells
              | ValueNone -> ()
            | None -> ()
          | ValueNone -> ()

        struct (version, cells))
    )

  let groupBy
    (keyOf: 'K -> 'V -> 'G)
    (map: AMap<'K, 'V>)
    : AMap<'G, AMap<'K, 'V>> when 'G: comparison =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map
        let groups = ResizeArray<'G * Map<'K, 'V>>()
        let index = System.Collections.Generic.Dictionary<'G, int>()

        for KeyValue(k, value) in snapshot do
          let g = keyOf k value

          match index.TryGetValue g with
          | true, i ->
            let _, acc = groups.[i]
            groups.[i] <- (g, Map.add k value acc)
          | false, _ ->
            index.[g] <- groups.Count
            groups.Add(g, Map.add k value Map.empty)

        let mutable cells = Map.empty

        for g, items in groups do
          let inner =
            AMap(
              AVal.computed(fun () -> struct (v, MapInternals.cellsOf items))
            )

          cells <- Map.add g (CVal.create(ValueSome inner)) cells

        struct (v, cells))
    )

  let difference (map: AMap<'K, 'V>) (other: AMap<'K, 'M>) : AMap<'K, 'V> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map
        let struct (_, otherSnapshot) = MapDerive.read other

        let kept =
          Map.filter (fun k _ -> not(Map.containsKey k otherSnapshot)) snapshot
          |> MapInternals.cellsOf

        struct (v, kept))
    )

  let intersect (map: AMap<'K, 'V>) (other: AMap<'K, 'M>) : AMap<'K, 'V> =
    AMap(
      AVal.computed(fun () ->
        let struct (v, snapshot) = MapDerive.read map
        let struct (_, otherSnapshot) = MapDerive.read other

        let kept =
          Map.filter (fun k _ -> Map.containsKey k otherSnapshot) snapshot
          |> MapInternals.cellsOf

        struct (v, kept))
    )

  let intersectWith
    (combine: 'K -> 'V1 -> 'V2 -> 'M)
    (map: AMap<'K, 'V1>)
    (other: AMap<'K, 'V2>)
    : AMap<'K, 'M> =
    AMap(
      AVal.computed(fun () ->
        let struct (v1, snapshot1) = MapDerive.read map
        let struct (v2, snapshot2) = MapDerive.read other
        let version = max v1 v2
        let mutable cells = Map.empty

        for KeyValue(k, a) in snapshot1 do
          match Map.tryFind k snapshot2 with
          | Some b ->
            cells <- Map.add k (CVal.create(ValueSome(combine k a b))) cells
          | None -> ()

        struct (version, cells))
    )

  let intersectV
    (map: AMap<'K, 'V1>)
    (other: AMap<'K, 'V2>)
    : AMap<'K, struct ('V1 * 'V2)> =
    intersectWith (fun _ a b -> struct (a, b)) map other

  let unionWith
    (combine: 'K -> 'V -> 'V -> 'V)
    (map: AMap<'K, 'V>)
    (other: AMap<'K, 'V>)
    : AMap<'K, 'V> =
    AMap(
      AVal.computed(fun () ->
        let struct (v1, snapshot1) = MapDerive.read map
        let struct (v2, snapshot2) = MapDerive.read other
        let version = max v1 v2
        let mutable cells = Map.empty

        for KeyValue(k, a) in snapshot1 do
          cells <- Map.add k (CVal.create(ValueSome a)) cells

        for KeyValue(k, b) in snapshot2 do
          let v =
            match Map.tryFind k snapshot1 with
            | Some a -> combine k a b
            | None -> b

          cells <- Map.add k (CVal.create(ValueSome v)) cells

        struct (version, cells))
    )

  let union (map: AMap<'K, 'V>) (other: AMap<'K, 'V>) : AMap<'K, 'V> =
    unionWith (fun _ _ right -> right) map other

  let averageBy (by: 'K -> 'V -> float) (map: AMap<'K, 'V>) : AVal<float> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable total = 0.0
      let mutable n = 0

      for KeyValue(k, v) in snapshot do
        total <- total + by k v
        n <- n + 1

      if n = 0 then 0.0 else total / float n)

  let averageByA
    (by: 'K -> 'V -> AVal<float>)
    (map: AMap<'K, 'V>)
    : AVal<float> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable total = 0.0
      let mutable n = 0

      for KeyValue(k, v) in snapshot do
        total <- total + AVal.get(by k v)
        n <- n + 1

      if n = 0 then 0.0 else total / float n)

  let sumBy (by: 'K -> 'V -> float) (map: AMap<'K, 'V>) : AVal<float> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable total = 0.0

      for KeyValue(k, v) in snapshot do
        total <- total + by k v

      total)

  let sumByA (by: 'K -> 'V -> AVal<float>) (map: AMap<'K, 'V>) : AVal<float> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable total = 0.0

      for KeyValue(k, v) in snapshot do
        total <- total + AVal.get(by k v)

      total)

  let tryMaxA
    (by: 'K -> 'V -> AVal<'M>)
    (map: AMap<'K, 'V>)
    : AVal<'M voption> when 'M: comparison =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable best = ValueNone

      for KeyValue(k, v) in snapshot do
        let candidate = AVal.get(by k v)

        match best with
        | ValueNone -> best <- ValueSome candidate
        | ValueSome b when candidate > b -> best <- ValueSome candidate
        | ValueSome _ -> ()

      best)

  let tryMinA
    (by: 'K -> 'V -> AVal<'M>)
    (map: AMap<'K, 'V>)
    : AVal<'M voption> when 'M: comparison =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable best = ValueNone

      for KeyValue(k, v) in snapshot do
        let candidate = AVal.get(by k v)

        match best with
        | ValueNone -> best <- ValueSome candidate
        | ValueSome b when candidate < b -> best <- ValueSome candidate
        | ValueSome _ -> ()

      best)

  let reduce (op: 'V -> 'V -> 'V) (map: AMap<'K, 'V>) : AVal<'V voption> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable acc = ValueNone

      for KeyValue(_, v) in snapshot do
        match acc with
        | ValueNone -> acc <- ValueSome v
        | ValueSome a -> acc <- ValueSome(op a v)

      acc)

  let reduceBy
    (by: 'K -> 'V -> 'M)
    (op: 'M -> 'M -> 'M)
    (map: AMap<'K, 'V>)
    : AVal<'M voption> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable acc = ValueNone

      for KeyValue(k, v) in snapshot do
        match acc with
        | ValueNone -> acc <- ValueSome(by k v)
        | ValueSome a -> acc <- ValueSome(op a (by k v))

      acc)

  let reduceByA
    (by: 'K -> 'V -> AVal<'M>)
    (op: 'M -> 'M -> 'M)
    (map: AMap<'K, 'V>)
    : AVal<'M voption> =
    AVal.computed(fun () ->
      let struct (_, snapshot) = MapDerive.read map
      let mutable acc = ValueNone

      for KeyValue(k, v) in snapshot do
        match acc with
        | ValueNone -> acc <- ValueSome(AVal.get(by k v))
        | ValueSome a -> acc <- ValueSome(op a (AVal.get(by k v)))

      acc)

[<RequireQualifiedAccess>]
module CMap =

  let empty<'K, 'V when 'K: comparison and 'V: equality> : CMap<'K, 'V> = CMap()

  let ofSeq(items: seq<'K * 'V>) : CMap<'K, 'V> = CMap(items)

  let addOrUpdate key value (map: CMap<'K, 'V>) = map.AddOrUpdate(key, value)

  let remove key (map: CMap<'K, 'V>) = map.Remove(key)

  let clear(map: CMap<'K, 'V>) = map.Clear()

  let set (value: Map<'K, 'V>) (map: CMap<'K, 'V>) =
    map.Set(value |> Seq.map(fun kv -> kv.Key, kv.Value))

  let updateTo (target: seq<'K * 'V>) (map: CMap<'K, 'V>) : bool =
    map.UpdateTo(target)

  /// Applies a batch of map operations atomically: one net change at the
  /// end of the batch (Mibo.Adaptive <c>CMap.perform</c> parity).
  let perform (delta: MapDeltaBuilder<'K, 'V>) (map: CMap<'K, 'V>) : unit =
    Collections.batch(fun () ->
      let struct (sets, removes) = struct (delta.Sets, delta.Removes)

      for struct (k, v) in sets do
        map.AddOrUpdate(k, v) |> ignore

      for k in removes do
        map.Remove(k) |> ignore)

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
