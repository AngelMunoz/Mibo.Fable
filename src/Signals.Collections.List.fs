namespace Mibo.Signals

open System

// Implementation of the signature-file surface. CList owns a stable id
// counter: writes append ids, reorder the order array, or write one
// element cell — moves and inserts never touch element values. Views are
// either per-id cell transformations (map) or coarse rebuilds (everything
// position-dependent, which re-derives on any structural change anyway).

type internal ListPostOp<'T> =
  | ListPostAdd of 'T
  | ListPostAddRange of 'T array
  | ListPostPrepend of 'T
  | ListPostInsertAt of struct (int * 'T)
  | ListPostRemove of 'T
  | ListPostRemoveAt of int
  | ListPostUpdateAt of struct (int * 'T)
  | ListPostReplace of 'T array

type CList<'T when 'T: equality>(items: seq<'T>) =

  let mutable nextId = 0
  let mutable order: int list = []
  let mutable byId: Map<int, AVal<'T voption>> = Map.empty
  let mutable version = 0L
  let root = CVal.create 0L

  let state: AVal<struct (int64 * ListCells<'T>)> =
    AVal.computed(fun () ->
      AVal.get root |> ignore
      struct (version, ListCells(Array.ofList order, byId)))

  let posts = ResizeArray<ListPostOp<'T>>()
  let mutable flushing = false

  let bump() =
    version <- version + 1L
    CVal.set version root

  let freshCell(value: 'T) : CVal<'T voption> = CVal.create(ValueSome value)

  let appendOne(item: 'T) =
    let id = nextId
    nextId <- nextId + 1
    order <- order @ [ id ]
    byId <- Map.add id (freshCell item) byId
    bump()

  let prependOne(item: 'T) =
    let id = nextId
    nextId <- nextId + 1
    order <- id :: order
    byId <- Map.add id (freshCell item) byId
    bump()

  let positionValid(position: int) =
    position >= 0 && position < List.length order

  let idAt(position: int) = List.item position order

  let dropAt(position: int) =
    if not(positionValid position) then
      raise(System.ArgumentOutOfRangeException(nameof position))

    let id = idAt position
    order <- List.removeAt position order
    byId <- Map.remove id byId
    bump()

  let clearAll() =
    if order <> [] then
      order <- []
      byId <- Map.empty
      bump()

  let flushPosts() =
    if posts.Count > 0 && not flushing then
      flushing <- true

      try
        let ops = Array.ofSeq posts
        posts.Clear()

        // Intents apply in order.
        for op in ops do
          match op with
          | ListPostAdd item -> appendOne item
          | ListPostAddRange extraItems -> extraItems |> Array.iter appendOne
          | ListPostPrepend item -> prependOne item
          | ListPostInsertAt(p, item) ->
            if p <= List.length order then
              let id = nextId
              nextId <- nextId + 1
              order <- List.insertAt p id order
              byId <- Map.add id (freshCell item) byId
              bump()
          | ListPostRemove item ->

            match
              List.tryFindIndex
                (fun id ->
                  match (Map.find id byId).value with
                  | ValueSome v -> v = item
                  | ValueNone -> false)
                order
            with
            | Some p -> dropAt p
            | None -> ()
          | ListPostRemoveAt p -> dropAt p |> ignore
          | ListPostUpdateAt(p, item) ->
            if positionValid p then
              (byId.[idAt p]).value <- ValueSome item
          | ListPostReplace values ->
            clearAll()

            for item in values do
              appendOne item
      finally
        flushing <- false

  let alist = AList(state, OnRead = flushPosts)

  do
    for item in items do
      let id = nextId
      nextId <- nextId + 1
      order <- order @ [ id ]
      byId <- Map.add id (freshCell item) byId

  member _.Value: AList<'T> = alist

  /// Applies the pending boundary intents now.
  member _.FlushPosts() = flushPosts()

  member _.Append(item) =
    flushPosts()
    appendOne item

  member _.AddRange(items: seq<'T>) =
    flushPosts()

    for item in items do
      appendOne item

  member _.Prepend(item) =
    flushPosts()
    prependOne item

  member _.InsertAt(position, item) =
    flushPosts()

    if position >= 0 && position <= List.length order then
      let id = nextId
      nextId <- nextId + 1
      order <- List.insertAt position id order
      byId <- Map.add id (freshCell item) byId
      bump()

  /// Removes the first occurrence of the value. No-op when absent.
  member _.Remove(item) =
    flushPosts()

    match
      List.tryFindIndex
        (fun id ->
          match (Map.find id byId).value with
          | ValueSome v -> v = item
          | ValueNone -> false)
        order
    with
    | Some p -> dropAt p
    | None -> ()

  /// Removes the element at the position; raises when out of range.
  member _.RemoveAt(position) =
    flushPosts()

    if not(positionValid position) then
      raise(System.ArgumentOutOfRangeException(nameof position))

    dropAt position

  /// Writes the element at the position; raises when out of range.
  member _.UpdateAt(position, item) =
    flushPosts()

    if not(positionValid position) then
      raise(System.ArgumentOutOfRangeException(nameof position))

    (byId.[idAt position]).value <- ValueSome item

  /// Replaces the whole content with the given values.
  member _.Set(values: seq<'T>) =
    flushPosts()
    clearAll()

    for item in values do
      appendOne item

  member _.Clear() =
    flushPosts()
    clearAll()

  member _.UpdateTo(items: seq<'T>) =
    flushPosts()
    let target = Array.ofSeq items

    let current =
      Array.ofList order
      |> Array.map(fun id ->
        match (Map.find id byId).value with
        | ValueSome v -> v
        | ValueNone -> failwith "list cell lost its value")

    if current = target then
      false
    else
      clearAll()

      for item in target do
        appendOne item

      true

  member _.PostAdd(item) = posts.Add(ListPostAdd item)

  member _.PostAppend(items: seq<'T>) =
    posts.Add(ListPostAddRange(Array.ofSeq items))

  member _.PostPrepend(item) = posts.Add(ListPostPrepend item)

  member _.PostInsertAt(position, item) =
    posts.Add(ListPostInsertAt(struct (position, item)))

  member _.PostRemove(item) = posts.Add(ListPostRemove item)
  member _.PostRemoveAt(position) = posts.Add(ListPostRemoveAt position)

  member _.PostSet(values: seq<'T>) =
    posts.Add(ListPostReplace(Array.ofSeq values))

  member _.PostUpdateAt(position, item) =
    posts.Add(ListPostUpdateAt(struct (position, item)))

  member _.PostClear() = posts.Add(ListPostReplace [||])

  member _.Force() =
    flushPosts()

    Array.ofList order
    |> Array.map(fun id ->
      match (Map.find id byId).value with
      | ValueSome v -> v
      | ValueNone -> failwith "list cell lost its value")

[<RequireQualifiedAccess>]
module CList =

  let empty<'T when 'T: equality> : CList<'T> = CList(Seq.empty)

  let ofSeq(items: seq<'T>) : CList<'T> when 'T: equality = CList(items)

  let ofArray(items: 'T array) : CList<'T> when 'T: equality = CList(items)

  let ofList(items: 'T list) : CList<'T> when 'T: equality = CList(items)

  let init count (initializer: int -> 'T) : CList<'T> when 'T: equality =
    CList(Seq.init count initializer)

  let range first last : CList<int> = CList([ first..last ])

  let append item (list: CList<'T>) = list.Append(item)

  let addRange items (list: CList<'T>) = list.AddRange(items)

  let prepend item (list: CList<'T>) = list.Prepend(item)

  let insertAt position item (list: CList<'T>) = list.InsertAt(position, item)

  let remove item (list: CList<'T>) = list.Remove(item)

  let removeAt position (list: CList<'T>) = list.RemoveAt(position)

  let updateAt position item (list: CList<'T>) = list.UpdateAt(position, item)

  /// Replaces the whole content with the given values.
  let set (values: seq<'T>) (list: CList<'T>) = list.Set(values)

  let clear(list: CList<'T>) = list.Clear()

  let updateTo items (list: CList<'T>) = list.UpdateTo(items)

  /// Applies a batch of list operations atomically: one net change at the
  /// end of the batch (Mibo.Adaptive <c>CList.perform</c> parity).
  /// Positions refer to the state as of the previous operation.
  let perform (delta: ListDeltaBuilder<'T>) (list: CList<'T>) : unit =
    Collections.batch(fun () ->
      // Positions refer to the state as of the previous operation: each
      // operation applies to the list as the previous one left it.
      for op in delta.Operations do
        match op.Kind with
        | ListInsert -> list.InsertAt(op.Position, op.Value)
        | ListRemove -> list.RemoveAt(op.Position)
        | ListUpdate -> list.UpdateAt(op.Position, op.Value))

  let postAdd item (list: CList<'T>) = list.PostAdd(item)

  let postAppend items (list: CList<'T>) = list.PostAppend(items)

  let postPrepend item (list: CList<'T>) = list.PostPrepend(item)

  let postInsertAt position item (list: CList<'T>) =
    list.PostInsertAt(position, item)

  let postRemove item (list: CList<'T>) = list.PostRemove(item)

  let postRemoveAt position (list: CList<'T>) = list.PostRemoveAt(position)

  let postSet (values: seq<'T>) (list: CList<'T>) = list.PostSet(values)

  let postUpdateAt position item (list: CList<'T>) =
    list.PostUpdateAt(position, item)

  let postClear(list: CList<'T>) = list.PostClear()

  let value(list: CList<'T>) : AList<'T> = list.Value

  let force(list: CList<'T>) : 'T array =
    Collections.guardPack "CList.force"
    list.Force()

[<RequireQualifiedAccess>]
module internal ListInternals =

  /// Reads the current stable-id cells, flushing the owner's posted
  /// intents first.
  let read(list: AList<'T>) : struct (int64 * ListCells<'T>) =
    list.OnRead()
    AVal.get list.State

  /// Materializes the current elements in order.
  let materialize(list: AList<'T>) : 'T array =
    let struct (_, cells) = read list

    cells.Order
    |> Array.choose(fun id ->
      match Map.tryFind id cells.ById with
      | Some cell ->
        match cell.value with
        | ValueSome v -> Some v
        | ValueNone -> None
      | None -> None)

  /// Builds stable-id cells from a sequence (ids 0..n-1).
  let cellsOf(items: seq<'T>) : ListCells<'T> =
    let mutable byId = Map.empty
    let mutable order = []
    let mutable i = 0

    for item in items do
      order <- order @ [ i ]
      byId <- Map.add i (CVal.create(ValueSome item)) byId
      i <- i + 1

    ListCells(Array.ofList order, byId)

  /// Rebuilds a derived list from a plain sequence (coarse granularity —
  /// position-dependent views re-derive on any structural change anyway).
  let derive (list: AList<'T>) (f: 'T array -> 'M array) : AList<'M> =
    AList(
      AVal.computed(fun () ->
        let struct (v, _) = read list
        struct (v, cellsOf(f(materialize list))))
    )

[<RequireQualifiedAccess>]
module AList =

  let empty<'T> : AList<'T> =
    AList(AVal.computed(fun () -> struct (0L, ListCells([||], Map.empty))))

  let constant(create: unit -> seq<'T>) : AList<'T> =
    let mutable cache = ValueNone

    AList(
      AVal.computed(fun () ->
        match cache with
        | ValueSome s -> s
        | ValueNone ->
          let s = struct (0L, ListInternals.cellsOf(create()))
          cache <- ValueSome s
          s)
    )

  let delay create = constant create

  let ofCells(cells: ListCells<'T>) : AList<'T> =
    AList(AVal.computed(fun () -> struct (0L, cells)))

  let ofSeq(items: seq<'T>) : AList<'T> = ofCells(ListInternals.cellsOf items)

  let ofArray(items: 'T array) : AList<'T> = ofSeq items

  let ofList(items: 'T list) : AList<'T> = ofSeq items

  let ofResizeArray(items: 'T ResizeArray) : AList<'T> = ofSeq items

  let single(item: 'T) : AList<'T> = ofSeq [ item ]

  let init count (initializer: int -> 'T) : AList<'T> =
    ofSeq(Seq.init count initializer)

  let range first last : AList<int> = ofSeq [ first..last ]

  let ofAVal(value: AVal<seq<'T>>) : AList<'T> =
    let mutable version = 0L

    AList(
      AVal.computed(fun () ->
        let items = AVal.get value
        version <- version + 1L
        struct (version, ListInternals.cellsOf items))
    )

  let ofASet(set: ASet<'T>) : AList<'T> when 'T: comparison =
    AList(
      AVal.computed(fun () ->
        set.OnRead()
        let struct (v, s) = AVal.get set.State
        struct (v, ListInternals.cellsOf s))
    )

  let ofExternal(snapshot: unit -> seq<'T>) : AList<'T> * (unit -> unit) =
    let mutable version = 0L
    let mutable current = ListCells([||], Map.empty)
    let mutable primed = false
    let invalidations = CVal.create 0L

    let state: AVal<struct (int64 * ListCells<'T>)> =
      AVal.computed(fun () ->
        AVal.get invalidations |> ignore

        if not primed then
          primed <- true
          version <- version + 1L
          current <- ListInternals.cellsOf(snapshot())

        struct (version, current))

    let invalidate() =
      version <- version + 1L
      current <- ListInternals.cellsOf(snapshot())
      CVal.set (AVal.peek invalidations + 1L) invalidations

    (AList(state), invalidate)

  let custom(compute: 'T array -> ListDeltaBuilder<'T> -> unit) : AList<'T> =
    // Pull model, Mibo.Adaptive CustomListNode parity: the compute runs on
    // every read (via the poll hook) and appends the operations that
    // describe the change since its previous run; positions refer to the
    // state as of the previous operation, and the node applies them to the
    // stable-id cells.
    let mutable order: int list = []
    let mutable byId: Map<int, AVal<'T voption>> = Map.empty
    let mutable nextId = 0
    let mutable version = 0L
    let root = CVal.create 0L

    let positionOk p l = p >= 0 && p < List.length l

    let state: AVal<struct (int64 * ListCells<'T>)> =
      AVal.computed(fun () ->
        AVal.get root |> ignore
        struct (version, ListCells(Array.ofList order, byId)))

    let poll() =
      let view =
        Array.ofList order
        |> Array.choose(fun id ->
          match Map.tryFind id byId with
          | Some cell ->
            match cell.value with
            | ValueSome v -> Some v
            | ValueNone -> None
          | None -> None)

      let builder = ListDeltaBuilder<'T>()
      compute view builder
      let ops = builder.Operations

      if ops.Length > 0 then
        for op in ops do
          match op.Kind with
          | ListInsert ->
            if op.Position <= List.length order then
              let id = nextId
              nextId <- nextId + 1
              order <- List.insertAt op.Position id order
              byId <- Map.add id (CVal.create(ValueSome op.Value)) byId
          | ListRemove ->
            if positionOk op.Position order then
              let id = List.item op.Position order
              order <- List.removeAt op.Position order
              byId <- Map.remove id byId
          | ListUpdate ->
            if positionOk op.Position order then
              (byId.[List.item op.Position order]).value <- ValueSome op.Value

        version <- version + 1L
        CVal.set version root

    AList(state, OnRead = poll)

  let force(list: AList<'T>) : 'T array =
    Collections.guardPack "AList.force"
    ListInternals.materialize list

  let getValue list = force list

  let toArray list = force list

  let toList list = force list |> Array.toList

  let toAVal(list: AList<'T>) : AVal<'T array> =
    AVal.computed(fun () ->
      Collections.guardPack "AList.toAVal"
      ListInternals.materialize list)

  let toASet(list: AList<'T>) : ASet<'T> when 'T: comparison =
    ASet(
      AVal.computed(fun () ->
        list.OnRead()
        let struct (v, _) = AVal.get list.State
        struct (v, Set.ofArray(ListInternals.materialize list)))
    )

  let toIndexedASet(list: AList<'T>) : AMap<int, 'T> =
    AMap(
      AVal.computed(fun () ->
        list.OnRead()
        let struct (v, cells) = AVal.get list.State
        let mutable map = Map.empty

        cells.Order
        |> Array.iteri(fun pos id ->
          match Map.tryFind id cells.ById with
          | Some cell -> map <- Map.add pos cell map
          | None -> ())

        struct (v, map))
    )

  let count(list: AList<'T>) : AVal<int> =
    AVal.computed(fun () ->
      list.OnRead()
      let struct (_, cells) = AVal.get list.State
      cells.Order.Length)

  let isEmpty(list: AList<'T>) : AVal<bool> =
    count list |> AVal.map(fun c -> c = 0)

  let countBy (predicate: 'T -> bool) (list: AList<'T>) : AVal<int> =
    AVal.computed(fun () ->
      ListInternals.materialize list |> Array.filter predicate |> Array.length)

  let countByA (predicate: 'T -> AVal<bool>) (list: AList<'T>) : AVal<int> =
    AVal.computed(fun () ->
      ListInternals.materialize list
      |> Array.filter(fun t -> AVal.get(predicate t))
      |> Array.length)

  let exists (predicate: 'T -> bool) (list: AList<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      Array.exists predicate (ListInternals.materialize list))

  let existsA (predicate: 'T -> AVal<bool>) (list: AList<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      Array.exists
        (fun t -> AVal.get(predicate t))
        (ListInternals.materialize list))

  let forall (predicate: 'T -> bool) (list: AList<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      Array.forall predicate (ListInternals.materialize list))

  let forallA (predicate: 'T -> AVal<bool>) (list: AList<'T>) : AVal<bool> =
    AVal.computed(fun () ->
      Array.forall
        (fun t -> AVal.get(predicate t))
        (ListInternals.materialize list))

  let tryAt (index: int) (list: AList<'T>) : AVal<'T voption> =
    AVal.computed(fun () ->
      list.OnRead()
      let struct (_, cells) = AVal.get list.State

      if index >= 0 && index < cells.Order.Length then
        match Map.tryFind cells.Order.[index] cells.ById with
        | Some cell -> cell.value
        | None -> ValueNone
      else
        ValueNone)

  let tryGet index list = tryAt index list

  let tryFirst(list: AList<'T>) : AVal<'T voption> = tryAt 0 list

  let tryLast(list: AList<'T>) : AVal<'T voption> =
    AVal.computed(fun () ->
      list.OnRead()
      let struct (_, cells) = AVal.get list.State

      if cells.Order.Length = 0 then
        ValueNone
      else
        tryAt (cells.Order.Length - 1) list |> AVal.get)

  let tryMax(list: AList<'T>) : AVal<'T voption> when 'T: comparison =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list

      if items.Length = 0 then
        ValueNone
      else
        ValueSome(Array.max items))

  let tryMin(list: AList<'T>) : AVal<'T voption> when 'T: comparison =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list

      if items.Length = 0 then
        ValueNone
      else
        ValueSome(Array.min items))

  let tryMaxA
    (by: 'T -> AVal<'M>)
    (list: AList<'T>)
    : AVal<'M voption> when 'M: comparison =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list
      let mutable best = ValueNone

      for t in items do
        let candidate = AVal.get(by t)

        match best with
        | ValueNone -> best <- ValueSome candidate
        | ValueSome b when candidate > b -> best <- ValueSome candidate
        | ValueSome _ -> ()

      best)

  let tryMinA
    (by: 'T -> AVal<'M>)
    (list: AList<'T>)
    : AVal<'M voption> when 'M: comparison =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list
      let mutable best = ValueNone

      for t in items do
        let candidate = AVal.get(by t)

        match best with
        | ValueNone -> best <- ValueSome candidate
        | ValueSome b when candidate < b -> best <- ValueSome candidate
        | ValueSome _ -> ()

      best)

  let reduce (op: 'T -> 'T -> 'T) (list: AList<'T>) : AVal<'T voption> =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list
      let mutable acc = ValueNone

      for t in items do
        match acc with
        | ValueNone -> acc <- ValueSome t
        | ValueSome a -> acc <- ValueSome(op a t)

      acc)

  let reduceBy
    (by: 'T -> 'M)
    (op: 'M -> 'M -> 'M)
    (list: AList<'T>)
    : AVal<'M voption> =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list
      let mutable acc = ValueNone

      for t in items do
        match acc with
        | ValueNone -> acc <- ValueSome(by t)
        | ValueSome a -> acc <- ValueSome(op a (by t))

      acc)

  let reduceByA
    (by: 'T -> AVal<'M>)
    (op: 'M -> 'M -> 'M)
    (list: AList<'T>)
    : AVal<'M voption> =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list
      let mutable acc = ValueNone

      for t in items do
        match acc with
        | ValueNone -> acc <- ValueSome(AVal.get(by t))
        | ValueSome a -> acc <- ValueSome(op a (AVal.get(by t)))

      acc)

  let fold (add: 's -> 'T -> 's) (zero: 's) (list: AList<'T>) : AVal<'s> =
    AVal.computed(fun () ->
      Array.fold add zero (ListInternals.materialize list))

  let foldGroup
    (add: 's -> 'T -> 's)
    (subtract: 's -> 'T -> 's)
    (zero: 's)
    (list: AList<'T>)
    : AVal<'s> =
    fold add zero list

  let foldHalfGroup
    (add: 's -> 'T -> 's)
    (trySubtract: 's -> 'T -> 's voption)
    (zero: 's)
    (list: AList<'T>)
    : AVal<'s> =
    fold add zero list

  let average(list: AList<float>) : AVal<float> =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list

      if items.Length = 0 then
        0.0
      else
        Array.sum items / float items.Length)

  let averageBy (by: 'T -> float) (list: AList<'T>) : AVal<float> =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list

      if items.Length = 0 then
        0.0
      else
        Array.sumBy by items / float items.Length)

  let averageByA (by: 'T -> AVal<float>) (list: AList<'T>) : AVal<float> =
    AVal.computed(fun () ->
      let items = ListInternals.materialize list

      if items.Length = 0 then
        0.0
      else
        Array.sumBy (fun t -> AVal.get(by t)) items / float items.Length)

  let sum(list: AList<float>) : AVal<float> =
    AVal.computed(fun () -> Array.sum(ListInternals.materialize list))

  let sumBy (by: 'T -> float) (list: AList<'T>) : AVal<float> =
    AVal.computed(fun () -> Array.sumBy by (ListInternals.materialize list))

  let sumByA (by: 'T -> AVal<float>) (list: AList<'T>) : AVal<float> =
    AVal.computed(fun () ->
      Array.sumBy (fun t -> AVal.get(by t)) (ListInternals.materialize list))

  let map (mapping: 'T -> 'M) (list: AList<'T>) : AList<'M> =
    // Per-id mapping: derived cells are cached by id and reused across
    // recomputes, so a move or insert reorders without recomputing mapped
    // values — only new ids run the mapping.
    let derived = System.Collections.Generic.Dictionary<int, AVal<'M voption>>()

    AList(
      AVal.computed(fun () ->
        list.OnRead()
        let struct (v, cells) = AVal.get list.State

        let mapped =
          Map.fold
            (fun acc id cell ->
              let mappedCell =
                match derived.TryGetValue id with
                | true, cached -> cached
                | false, _ ->
                  let fresh =
                    AVal.computed(fun () ->
                      match AVal.get cell with
                      | ValueSome t -> ValueSome(mapping t)
                      | ValueNone -> ValueNone)

                  derived.[id] <- fresh
                  fresh

              acc |> Map.add id mappedCell)
            Map.empty
            cells.ById

        struct (v, ListCells(cells.Order, mapped)))
    )

  let mapA (mapping: 'T -> AVal<'M>) (list: AList<'T>) : AList<'M> =
    let derived = System.Collections.Generic.Dictionary<int, AVal<'M voption>>()

    AList(
      AVal.computed(fun () ->
        list.OnRead()
        let struct (v, cells) = AVal.get list.State

        let mapped =
          Map.fold
            (fun acc id cell ->
              let mappedCell =
                match derived.TryGetValue id with
                | true, cached -> cached
                | false, _ ->
                  let fresh =
                    AVal.computed(fun () ->
                      match AVal.get cell with
                      | ValueSome t -> ValueSome(AVal.get(mapping t))
                      | ValueNone -> ValueNone)

                  derived.[id] <- fresh
                  fresh

              acc |> Map.add id mappedCell)
            Map.empty
            cells.ById

        struct (v, ListCells(cells.Order, mapped)))
    )

  let mapi (mapping: int -> 'T -> 'M) (list: AList<'T>) : AList<'M> =
    ListInternals.derive list (Array.mapi mapping)

  let mapiA (mapping: int -> 'T -> AVal<'M>) (list: AList<'T>) : AList<'M> =
    ListInternals.derive list (fun items ->
      items |> Array.mapi(fun i t -> AVal.get(mapping i t)))

  let choose (chooser: 'T -> 'M option) (list: AList<'T>) : AList<'M> =
    ListInternals.derive list (fun items -> items |> Array.choose chooser)

  let chooseA (chooser: 'T -> AVal<'M option>) (list: AList<'T>) : AList<'M> =
    ListInternals.derive list (fun items ->
      items |> Array.choose(fun t -> AVal.get(chooser t)))

  let chooseAV chooser list = chooseA chooser list

  let chooseV chooser list = choose chooser list

  let choosei (chooser: int -> 'T -> 'M option) (list: AList<'T>) : AList<'M> =
    ListInternals.derive list (fun items ->
      items |> Array.mapi(fun i t -> chooser i t) |> Array.choose id)

  let chooseiA
    (chooser: int -> 'T -> AVal<'M option>)
    (list: AList<'T>)
    : AList<'M> =
    ListInternals.derive list (fun items ->
      items |> Array.mapi(fun i t -> AVal.get(chooser i t)) |> Array.choose id)

  let chooseiAV chooser list = chooseiA chooser list

  let chooseiV (chooser: int -> 'T -> 'M option) (list: AList<'T>) : AList<'M> =
    choosei chooser list

  let filter (predicate: 'T -> bool) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (Array.filter predicate)

  let filterA (predicate: 'T -> AVal<bool>) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (fun items ->
      items |> Array.filter(fun t -> AVal.get(predicate t)))

  let filteri (predicate: int -> 'T -> bool) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (fun items ->
      items
      |> Array.mapi(fun i t -> (i, t))
      |> Array.filter(fun (i, t) -> predicate i t)
      |> Array.map snd)

  let filteriA
    (predicate: int -> 'T -> AVal<bool>)
    (list: AList<'T>)
    : AList<'T> =
    ListInternals.derive list (fun items ->
      items
      |> Array.mapi(fun i t -> (i, t))
      |> Array.filter(fun (i, t) -> AVal.get(predicate i t))
      |> Array.map snd)

  let bind (binder: 'T -> AList<'M>) (list: AList<'T>) : AList<'M> =
    AList(
      AVal.computed(fun () ->
        let struct (v, _) = ListInternals.read list
        let mutable items = []

        for t in ListInternals.materialize list do
          let inner = binder t
          inner.OnRead()
          items <- items @ List.ofArray(ListInternals.materialize inner)

        struct (v, ListInternals.cellsOf items))
    )

  let bind2
    (binder: 'A -> 'B -> AList<'M>)
    (list1: AList<'A>)
    (list2: AList<'B>)
    : AList<'M> =
    AList(
      AVal.computed(fun () ->
        let struct (v1, _) = ListInternals.read list1
        let struct (v2, _) = ListInternals.read list2
        let a = ListInternals.materialize list1
        let b = ListInternals.materialize list2
        let shared = min a.Length b.Length
        let mutable items = []

        for i in 0 .. shared - 1 do
          let inner = binder a.[i] b.[i]
          inner.OnRead()
          items <- items @ List.ofArray(ListInternals.materialize inner)

        struct (max v1 v2, ListInternals.cellsOf items))
    )

  let bind3
    (binder: 'A -> 'B -> 'C -> AList<'M>)
    (list1: AList<'A>)
    (list2: AList<'B>)
    (list3: AList<'C>)
    : AList<'M> =
    AList(
      AVal.computed(fun () ->
        let struct (v1, _) = ListInternals.read list1
        let struct (v2, _) = ListInternals.read list2
        let struct (v3, _) = ListInternals.read list3
        let a = ListInternals.materialize list1
        let b = ListInternals.materialize list2
        let c = ListInternals.materialize list3
        let shared = min a.Length (min b.Length c.Length)
        let mutable items = []

        for i in 0 .. shared - 1 do
          let inner = binder a.[i] b.[i] c.[i]
          inner.OnRead()
          items <- items @ List.ofArray(ListInternals.materialize inner)

        struct (max v1 (max v2 v3), ListInternals.cellsOf items))
    )

  /// The concatenation of two lists (Mibo.Adaptive <c>AList.append</c> parity).
  let append (left: AList<'T>) (right: AList<'T>) : AList<'T> =
    AList(
      AVal.computed(fun () ->
        let struct (v1, _) = ListInternals.read left
        let struct (v2, _) = ListInternals.read right

        struct (max v1 v2,
                ListInternals.cellsOf(
                  Array.append
                    (ListInternals.materialize left)
                    (ListInternals.materialize right)
                )))
    )

  let concat(lists: AList<AList<'T>>) : AList<'T> =
    AList(
      AVal.computed(fun () ->
        let struct (v, _) = ListInternals.read lists
        let mutable items = []

        for inner in ListInternals.materialize lists do
          inner.OnRead()
          items <- items @ List.ofArray(ListInternals.materialize inner)

        struct (v, ListInternals.cellsOf items))
    )

  let indexed(list: AList<'T>) : AList<int * 'T> =
    ListInternals.derive list Array.indexed

  let rev(list: AList<'T>) : AList<'T> = ListInternals.derive list Array.rev

  let sub (index: int) (count: int) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (fun items ->
      items |> Array.skip(min index items.Length) |> Array.truncate count)

  let subA (index: AVal<int>) (count: AVal<int>) (list: AList<'T>) : AList<'T> =
    AList(
      AVal.computed(fun () ->
        let struct (v, _) = ListInternals.read list
        let i = AVal.get index
        let n = AVal.get count

        struct (v,
                ListInternals.cellsOf(
                  ListInternals.materialize list
                  |> Array.skip(
                    min i (Array.length(ListInternals.materialize list))
                  )
                  |> Array.truncate n
                )))
    )

  let take (count: int) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (Array.truncate count)

  let takeA (count: AVal<int>) (list: AList<'T>) : AList<'T> =
    AList(
      AVal.computed(fun () ->
        let struct (v, _) = ListInternals.read list

        struct (v,
                ListInternals.cellsOf(
                  Array.truncate
                    (AVal.get count)
                    (ListInternals.materialize list)
                )))
    )

  let skip (count: int) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (fun items ->
      items |> Array.skip(min count items.Length))

  let skipA (count: AVal<int>) (list: AList<'T>) : AList<'T> =
    AList(
      AVal.computed(fun () ->
        let struct (v, _) = ListInternals.read list
        let items = ListInternals.materialize list

        struct (v,
                ListInternals.cellsOf(
                  items |> Array.skip(min (AVal.get count) items.Length)
                )))
    )

  let pairwise(list: AList<'T>) : AList<'T * 'T> =
    ListInternals.derive list Array.pairwise

  let pairwiseCyclic(list: AList<'T>) : AList<'T * 'T> =
    ListInternals.derive list (fun items ->
      if items.Length < 2 then
        [||]
      else
        Array.append (Array.pairwise items) [|
          (items.[items.Length - 1], items.[0])
        |])

  let sort(list: AList<'T>) : AList<'T> when 'T: comparison =
    ListInternals.derive list Array.sort

  let sortDescending(list: AList<'T>) : AList<'T> when 'T: comparison =
    ListInternals.derive list (fun items -> Array.sortDescending items)

  let sortBy
    (projection: 'T -> 'K)
    (list: AList<'T>)
    : AList<'T> when 'K: comparison =
    ListInternals.derive list (Array.sortBy projection)

  let sortByDescending
    (projection: 'T -> 'K)
    (list: AList<'T>)
    : AList<'T> when 'K: comparison =
    ListInternals.derive list (Array.sortByDescending projection)

  let sortByi
    (projection: int -> 'T -> 'K)
    (list: AList<'T>)
    : AList<'T> when 'K: comparison =
    ListInternals.derive list (fun items ->
      items
      |> Array.mapi(fun i t -> (projection i t, t))
      |> Array.sortBy fst
      |> Array.map snd)

  let sortByDescendingi
    (projection: int -> 'T -> 'K)
    (list: AList<'T>)
    : AList<'T> when 'K: comparison =
    ListInternals.derive list (fun items ->
      items
      |> Array.mapi(fun i t -> (projection i t, t))
      |> Array.sortByDescending fst
      |> Array.map snd)

  let sortWith (comparison: 'T -> 'T -> int) (list: AList<'T>) : AList<'T> =
    ListInternals.derive list (Array.sortWith comparison)
