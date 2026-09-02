namespace Mibo.Signals

open System
open System.Collections.Generic

// Implementation of the signature-file surface. ASet is structure-only:
// one version channel over a Set snapshot, no per-element cells. The
// changeable CSet mirrors CMap's write path, boundary intents included.

type internal SetPostOp<'T when 'T: comparison> =
  | SetPostAdd of 'T
  | SetPostRemove of 'T
  | SetPostReplace of Set<'T>

type CSet<'T when 'T: comparison>(items: seq<'T>) =

  let mutable current: Set<'T> =
    items |> Seq.fold (fun acc t -> Set.add t acc) Set.empty

  let mutable version = 0L
  let root = CVal.create 0L

  let state: AVal<struct (int64 * Set<'T>)> =
    AVal.computed(fun () ->
      AVal.get root |> ignore
      struct (version, current))

  let posts = ResizeArray<SetPostOp<'T>>()
  let mutable flushing = false

  let bump() =
    version <- version + 1L
    CVal.set version root

  let dropAll() =
    if not(Set.isEmpty current) then
      current <- Set.empty
      bump()

  let applyAdd(item: 'T) : unit =
    if not(Set.contains item current) then
      current <- Set.add item current
      bump()

  let applyRemove(item: 'T) : unit =
    if Set.contains item current then
      current <- Set.remove item current
      bump()

  let replaceWith(target: Set<'T>) =
    // Reuse one bump for a whole replacement.
    if current <> target then
      current <- target
      bump()

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
          | SetPostReplace _ -> lastReplace <- i
          | _ -> ()

        for i in 0 .. ops.Length - 1 do
          if i >= lastReplace then
            match ops.[i] with
            | SetPostAdd item -> applyAdd item
            | SetPostRemove item -> applyRemove item
            | SetPostReplace target -> replaceWith target
      finally
        flushing <- false

  let aset = ASet(state, OnRead = flushPosts)

  new() = CSet(Seq.empty)

  member _.Value: ASet<'T> = aset

  /// Applies the pending boundary intents now.
  member _.FlushPosts() = flushPosts()

  /// Adds an element. No-op when already present.
  member _.Add(item) =
    flushPosts()
    applyAdd item

  /// Removes an element. No-op when absent.
  member _.Remove(item) =
    flushPosts()
    applyRemove item

  /// Replaces the whole content with the given items.
  member _.Set(items: seq<'T>) =
    flushPosts()
    replaceWith(Set.ofSeq items)

  member _.UpdateTo(items: seq<'T>) =
    flushPosts()
    let target = Set.ofSeq items

    if current = target then
      false
    else
      replaceWith target
      true

  member _.PostAdd(item) = posts.Add(SetPostAdd item)
  member _.PostRemove(item) = posts.Add(SetPostRemove item)

  member _.PostSet(items: seq<'T>) =
    posts.Add(SetPostReplace(Set.ofSeq items))

[<RequireQualifiedAccess>]
module CSet =

  let empty<'T when 'T: comparison> : CSet<'T> = CSet()

  let ofSeq(items: seq<'T>) : CSet<'T> = CSet(items)

  let add item (set: CSet<'T>) = set.Add(item)

  let remove item (set: CSet<'T>) = set.Remove(item)

  /// Replaces the whole content with the given set.
  let set (value: Set<'T>) (set: CSet<'T>) = set.Set(value)

  /// Replaces the content only when it differs; true when it changed.
  let updateTo items (set: CSet<'T>) = set.UpdateTo(items)

  /// Adds all the given elements in one atomic batch
  /// (Mibo.Adaptive <c>CSet.unionWith</c> parity).
  let unionWith (other: seq<'T>) (set: CSet<'T>) : unit =
    Collections.batch(fun () ->
      for x in other do
        set.Add(x))

  /// Removes all the given elements in one atomic batch
  /// (Mibo.Adaptive <c>CSet.exceptWith</c> parity).
  let exceptWith (other: seq<'T>) (set: CSet<'T>) : unit =
    Collections.batch(fun () ->
      for x in other do
        set.Remove(x))

  /// Reduces the content to its intersection with the given elements in
  /// one atomic batch (Mibo.Adaptive <c>CSet.intersectWith</c> parity).
  let intersectWith (other: seq<'T>) (set: CSet<'T>) : unit =
    let otherSet = Set.ofSeq other
    set.FlushPosts()
    let struct (_, current) = AVal.peek set.Value.State

    Collections.batch(fun () ->
      let removals = Set.filter (fun x -> not(Set.contains x otherSet)) current

      for x in removals do
        set.Remove(x))

  /// Applies a batch of set operations atomically: one net change at the
  /// end of the batch (Mibo.Adaptive <c>CSet.perform</c> parity).
  let perform (delta: SetDeltaBuilder<'T>) (set: CSet<'T>) : unit =
    Collections.batch(fun () ->
      for x in delta.Adds do
        set.Add(x)

      for x in delta.Removes do
        set.Remove(x))

  let postAdd item (set: CSet<'T>) = set.PostAdd(item)

  let postRemove item (set: CSet<'T>) = set.PostRemove(item)

  let postSet items (set: CSet<'T>) = set.PostSet(items)

  let value(set: CSet<'T>) : ASet<'T> = set.Value

  let force(set: CSet<'T>) : Set<'T> =
    Collections.guardPack "CSet.force"
    set.FlushPosts()
    let struct (_, current) = AVal.get set.Value.State
    current

  let toSet set = force set

[<RequireQualifiedAccess>]
module internal SetInternals =

  let read(set: ASet<'T>) : struct (int64 * Set<'T>) =
    set.OnRead()
    AVal.get set.State

[<RequireQualifiedAccess>]
module ASet =

  let empty<'T when 'T: comparison> : ASet<'T> =
    ASet(AVal.computed(fun () -> struct (0L, Set.empty)))

  let constant(create: unit -> Set<'T>) : ASet<'T> =
    let mutable cache = ValueNone

    ASet(
      AVal.computed(fun () ->
        match cache with
        | ValueSome s -> s
        | ValueNone ->
          let s = struct (0L, create())
          cache <- ValueSome s
          s)
    )

  let delay create = constant create

  let ofCells(cells: Set<'T>) : ASet<'T> =
    ASet(AVal.computed(fun () -> struct (0L, cells)))

  let ofSeq(items: seq<'T>) : ASet<'T> = ofCells(Set.ofSeq items)

  let ofArray(items: 'T array) : ASet<'T> = ofSeq items

  let ofList(items: 'T list) : ASet<'T> = ofSeq items

  let ofHashSet(items: HashSet<'T>) : ASet<'T> = ofSeq items

  let single(item: 'T) : ASet<'T> = ofSeq [ item ]

  let range (first: int) (last: int) : ASet<int> = ofSeq [ first..last ]

  let ofAVal(value: AVal<seq<'T>>) : ASet<'T> =
    let mutable version = 0L

    ASet(
      AVal.computed(fun () ->
        let snapshot = Set.ofSeq(AVal.get value)
        version <- version + 1L
        struct (version, snapshot))
    )

  let ofExternal(snapshot: unit -> Set<'T>) : ASet<'T> * (unit -> unit) =
    let mutable version = 0L
    let mutable current: Set<'T> = Set.empty
    let mutable primed = false
    let invalidations = CVal.create 0L

    let state: AVal<struct (int64 * Set<'T>)> =
      AVal.computed(fun () ->
        AVal.get invalidations |> ignore

        if not primed then
          primed <- true
          version <- version + 1L
          current <- snapshot()

        struct (version, current))

    let invalidate() =
      version <- version + 1L
      current <- snapshot()
      CVal.set (AVal.peek invalidations + 1L) invalidations

    (ASet(state), invalidate)

  /// <summary>
  /// A set driven by an external snapshot function with no invalidate
  /// handle: the reader runs on every read (Mibo.Adaptive
  /// <c>ASet.ofReader</c> parity, pull model).
  /// </summary>
  let ofReader(reader: unit -> Set<'T>) : ASet<'T> =
    // Pull model: the reader runs on every read (via the poll hook).
    let mutable current: Set<'T> = Set.empty
    let mutable version = 0L
    let root = CVal.create 0L

    let state: AVal<struct (int64 * Set<'T>)> =
      AVal.computed(fun () ->
        AVal.get root |> ignore
        struct (version, current))

    let poll() =
      let snapshot = reader()

      if current <> snapshot then
        current <- snapshot
        version <- version + 1L
        CVal.set version root

    ASet(state, OnRead = poll)

  let custom(compute: Set<'T> -> SetDeltaBuilder<'T> -> unit) : ASet<'T> =
    // Pull model, Mibo.Adaptive CustomSetNode parity: the compute runs on
    // every read (via the poll hook) and appends the operations that
    // describe the change since its previous run; the node applies them.
    let mutable current: Set<'T> = Set.empty
    let mutable version = 0L
    let root = CVal.create 0L

    let state: AVal<struct (int64 * Set<'T>)> =
      AVal.computed(fun () ->
        AVal.get root |> ignore
        struct (version, current))

    let poll() =
      let builder = SetDeltaBuilder<'T>()
      compute current builder
      let struct (adds, removes) = struct (builder.Adds, builder.Removes)

      if adds.Length > 0 || removes.Length > 0 then
        for x in adds do
          current <- Set.add x current

        for x in removes do
          current <- Set.remove x current

        version <- version + 1L
        CVal.set version root

    ASet(state, OnRead = poll)

  let force(set: ASet<'T>) : Set<'T> =
    Collections.guardPack "ASet.force"
    set.OnRead()
    let struct (_, s) = AVal.get set.State
    s

  let getValue set = force set

  let toAVal(set: ASet<'T>) : AVal<Set<'T>> =
    AVal.computed(fun () ->
      Collections.guardPack "ASet.toAVal"
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      s)

  let toSet set = force set

  let count(set: ASet<'T>) : AVal<int> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.count s)

  let isEmpty(set: ASet<'T>) : AVal<bool> =
    count set |> AVal.map(fun c -> c = 0)

  let contains (item: 'T) (set: ASet<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.contains item s)

  let exists (predicate: 'T -> bool) (set: ASet<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.exists predicate s)

  let existsA (predicate: 'T -> AVal<bool>) (set: ASet<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      s |> Set.exists(fun t -> AVal.get(predicate t)))

  let forall (predicate: 'T -> bool) (set: ASet<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.forall predicate s)

  let forallA (predicate: 'T -> AVal<bool>) (set: ASet<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      s |> Set.forall(fun t -> AVal.get(predicate t)))

  let countBy (predicate: 'T -> bool) (set: ASet<'T>) : AVal<int> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      s |> Seq.filter predicate |> Seq.length)

  let countByA (predicate: 'T -> AVal<bool>) (set: ASet<'T>) : AVal<int> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      s |> Seq.filter(fun t -> AVal.get(predicate t)) |> Seq.length)

  let average(set: ASet<float>) : AVal<float> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State

      if Set.isEmpty s then
        0.0
      else
        Set.fold (fun acc t -> acc + t) 0.0 s / float(Set.count s))

  let averageBy (by: 'T -> float) (set: ASet<'T>) : AVal<float> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State

      if Set.isEmpty s then
        0.0
      else
        Set.fold (fun acc t -> acc + by t) 0.0 s / float(Set.count s))

  let averageByA (by: 'T -> AVal<float>) (set: ASet<'T>) : AVal<float> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State

      if Set.isEmpty s then
        0.0
      else
        Set.fold (fun acc t -> acc + AVal.get(by t)) 0.0 s / float(Set.count s))

  let sum(set: ASet<float>) : AVal<float> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.fold (fun acc t -> acc + t) 0.0 s)

  let sumBy (by: 'T -> float) (set: ASet<'T>) : AVal<float> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.fold (fun acc t -> acc + by t) 0.0 s)

  let sumByA (by: 'T -> AVal<float>) (set: ASet<'T>) : AVal<float> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.fold (fun acc t -> acc + AVal.get(by t)) 0.0 s)

  let tryMax(set: ASet<'T>) : AVal<'T voption> when 'T: comparison =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State

      if Set.isEmpty s then
        ValueNone
      else
        ValueSome(Set.maxElement s))

  let tryMin(set: ASet<'T>) : AVal<'T voption> when 'T: comparison =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State

      if Set.isEmpty s then
        ValueNone
      else
        ValueSome(Set.minElement s))

  let tryMaxA
    (by: 'T -> AVal<'M>)
    (set: ASet<'T>)
    : AVal<'M voption> when 'M: comparison =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      let mutable best = ValueNone

      s
      |> Set.iter(fun t ->
        let candidate = AVal.get(by t)

        match best with
        | ValueNone -> best <- ValueSome candidate
        | ValueSome b when candidate > b -> best <- ValueSome candidate
        | ValueSome _ -> ())

      best)

  let tryMinA
    (by: 'T -> AVal<'M>)
    (set: ASet<'T>)
    : AVal<'M voption> when 'M: comparison =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      let mutable best = ValueNone

      s
      |> Set.iter(fun t ->
        let candidate = AVal.get(by t)

        match best with
        | ValueNone -> best <- ValueSome candidate
        | ValueSome b when candidate < b -> best <- ValueSome candidate
        | ValueSome _ -> ())

      best)

  let reduce (op: 'T -> 'T -> 'T) (set: ASet<'T>) : AVal<'T voption> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      let mutable acc = ValueNone

      s
      |> Set.iter(fun t ->
        match acc with
        | ValueNone -> acc <- ValueSome t
        | ValueSome a -> acc <- ValueSome(op a t))

      acc)

  let reduceBy
    (by: 'T -> 'M)
    (op: 'M -> 'M -> 'M)
    (set: ASet<'T>)
    : AVal<'M voption> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      let mutable acc = ValueNone

      s
      |> Set.iter(fun t ->
        match acc with
        | ValueNone -> acc <- ValueSome(by t)
        | ValueSome a -> acc <- ValueSome(op a (by t)))

      acc)

  let reduceByA
    (by: 'T -> AVal<'M>)
    (op: 'M -> 'M -> 'M)
    (set: ASet<'T>)
    : AVal<'M voption> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      let mutable acc = ValueNone

      s
      |> Set.iter(fun t ->
        match acc with
        | ValueNone -> acc <- ValueSome(AVal.get(by t))
        | ValueSome a -> acc <- ValueSome(op a (AVal.get(by t))))

      acc)

  let fold (add: 's -> 'T -> 's) (zero: 's) (set: ASet<'T>) : AVal<'s> =
    AVal.computed(fun () ->
      set.OnRead()
      let struct (_, s) = AVal.get set.State
      Set.fold add zero s)

  let foldGroup
    (add: 's -> 'T -> 's)
    (subtract: 's -> 'T -> 's)
    (zero: 's)
    (set: ASet<'T>)
    : AVal<'s> =
    fold add zero set

  let foldHalfGroup
    (add: 's -> 'T -> 's)
    (trySubtract: 's -> 'T -> 's voption)
    (zero: 's)
    (set: ASet<'T>)
    : AVal<'s> =
    fold add zero set

  let map (mapping: 'T -> 'V) (set: ASet<'T>) : AMap<'T, 'V> =
    AMap(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State

        let cells =
          Set.fold
            (fun acc t -> Map.add t (CVal.create(ValueSome(mapping t))) acc)
            Map.empty
            s

        struct (v, cells))
    )

  let mapA (mapping: 'T -> AVal<'V>) (set: ASet<'T>) : AMap<'T, 'V> =
    AMap(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State

        let cells =
          Set.fold
            (fun acc t ->
              acc
              |> Map.add
                t
                (AVal.computed(fun () -> ValueSome(AVal.get(mapping t)))))
            Map.empty
            s

        struct (v, cells))
    )

  let mapTo (value: AVal<'V>) (set: ASet<'T>) : AMap<'T, 'V> =
    mapA (fun _ -> value) set

  let bind (binder: 'T -> ASet<'M>) (set: ASet<'T>) : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        let mutable result = Set.empty

        s
        |> Set.iter(fun t ->
          let inner = binder t
          inner.OnRead()
          let struct (_, innerSet) = AVal.get inner.State
          result <- Set.union result innerSet)

        struct (v, result))
    )

  let bind2
    (binder: 'T -> 'T -> ASet<'M>)
    (set1: ASet<'T>)
    (set2: ASet<'T>)
    : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        set1.OnRead()
        set2.OnRead()
        let struct (v1, s1) = AVal.get set1.State
        let struct (v2, s2) = AVal.get set2.State
        let mutable result = Set.empty

        s1
        |> Set.iter(fun a ->
          match (if Set.contains a s2 then Some a else None) with
          | Some b ->
            let inner = binder a b
            inner.OnRead()
            let struct (_, innerSet) = AVal.get inner.State
            result <- Set.union result innerSet
          | None -> ())

        struct (max v1 v2, result))
    )

  let bind3
    (binder: 'T -> 'T -> 'T -> ASet<'M>)
    (set1: ASet<'T>)
    (set2: ASet<'T>)
    (set3: ASet<'T>)
    : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        set1.OnRead()
        set2.OnRead()
        set3.OnRead()
        let struct (v1, s1) = AVal.get set1.State
        let struct (v2, s2) = AVal.get set2.State
        let struct (v3, s3) = AVal.get set3.State
        let mutable result = Set.empty

        s1
        |> Set.iter(fun a ->
          match
            (if Set.contains a s2 then Some a else None),
            (if Set.contains a s3 then Some a else None)
          with
          | Some b, Some c ->
            let inner = binder a b c
            inner.OnRead()
            let struct (_, innerSet) = AVal.get inner.State
            result <- Set.union result innerSet
          | _ -> ())

        struct (max v1 (max v2 v3), result))
    )

  let collect (binder: 'T -> ASet<'M>) (set: ASet<'T>) : ASet<'M> =
    bind binder set

  let collect' (binder: 'T -> seq<'M>) (set: ASet<'T>) : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        let mutable result = Set.empty

        s
        |> Set.iter(fun t -> result <- Set.union result (Set.ofSeq(binder t)))

        struct (v, result))
    )

  let filter (predicate: 'T -> bool) (set: ASet<'T>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        struct (v, Set.filter predicate s))
    )

  let filterA (predicate: 'T -> AVal<bool>) (set: ASet<'T>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State

        let filtered = s |> Set.filter(fun t -> AVal.get(predicate t))
        struct (v, filtered))
    )

  let choose (chooser: 'T -> 'M option) (set: ASet<'T>) : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        let mutable result = Set.empty

        s
        |> Set.iter(fun t ->
          match chooser t with
          | Some m -> result <- Set.add m result
          | None -> ())

        struct (v, result))
    )

  let chooseA (chooser: 'T -> AVal<'M option>) (set: ASet<'T>) : ASet<'M> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        let mutable result = Set.empty

        s
        |> Set.iter(fun t ->
          match AVal.get(chooser t) with
          | Some m -> result <- Set.add m result
          | None -> ())

        struct (v, result))
    )

  let chooseAV chooser set = chooseA chooser set

  let chooseV chooser set = choose chooser set

  let difference (set: ASet<'T>) (other: ASet<'T>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        other.OnRead()
        let struct (v, s) = AVal.get set.State
        let struct (_, o) = AVal.get other.State
        struct (v, Set.filter (fun t -> not(Set.contains t o)) s))
    )

  let exceptWith set other = difference set other

  let intersect (set: ASet<'T>) (other: ASet<'T>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        other.OnRead()
        let struct (v1, s) = AVal.get set.State
        let struct (v2, o) = AVal.get other.State
        struct (max v1 v2, Set.intersect s o))
    )

  let intersectWith set other = intersect set other

  let union (set: ASet<'T>) (other: ASet<'T>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        other.OnRead()
        let struct (v1, s) = AVal.get set.State
        let struct (v2, o) = AVal.get other.State
        struct (max v1 v2, Set.union s o))
    )

  let unionWith
    (combine: 'T -> 'T -> 'T)
    (set: ASet<'T>)
    (other: ASet<'T>)
    : ASet<'T> =
    // Sets hold no values; the combiner can only pick between the two
    // equal keys, and either choice yields the same element.
    union set other

  let unionMany(sets: AVal<seq<ASet<'T>>>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        let mutable result = Set.empty
        let mutable version = 0L

        for inner in AVal.get sets do
          inner.OnRead()
          let struct (v, innerSet) = AVal.get inner.State
          version <- max version v
          result <- Set.union result innerSet

        struct (version, result))
    )

  let xor (set: ASet<'T>) (other: ASet<'T>) : ASet<'T> =
    ASet(
      AVal.computed(fun () ->
        set.OnRead()
        other.OnRead()
        let struct (v1, s) = AVal.get set.State
        let struct (v2, o) = AVal.get other.State

        let both = Set.intersect s o

        struct (max v1 v2,
                Set.union (Set.difference s both) (Set.difference o both)))
    )


  // ─── Sorting (Mibo.Adaptive ASet.sort family: the sorted set as a list) ───

  let sortWith (comparison: 'T -> 'T -> int) (set: ASet<'T>) : AList<'T> =
    AList(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        let items = Set.toArray s
        Array.sortInPlaceWith comparison items
        let mutable byId = Map.empty
        let mutable order = []
        let mutable i = 0

        for item in items do
          order <- order @ [ i ]
          byId <- Map.add i (CVal.create(ValueSome item)) byId
          i <- i + 1

        struct (v, ListCells(Array.ofList order, byId)))
    )

  let sort(set: ASet<'T>) : AList<'T> when 'T: comparison = sortWith compare set

  let sortDescending(set: ASet<'T>) : AList<'T> when 'T: comparison =
    sortWith (fun a b -> compare b a) set

  let sortBy
    (projection: 'T -> 'K)
    (set: ASet<'T>)
    : AList<'T> when 'K: comparison =
    sortWith (fun a b -> compare (projection a) (projection b)) set

  let sortByDescending
    (projection: 'T -> 'K)
    (set: ASet<'T>)
    : AList<'T> when 'K: comparison =
    sortWith (fun a b -> compare (projection b) (projection a)) set
