module Mibo.Fable.Adaptive.AList

open System
open System.Collections.Generic

/// <summary>An empty adaptive list (FDA <c>AList.empty</c> parity).</summary>
let empty<'T> : alist<'T> = new ConstantList<'T>(fun () -> Array.empty)

/// <summary>An adaptive list over fixed, immutable items.</summary>
let inline ofSeq(items: seq<'T>) : alist<'T> =
  new ConstantList<'T>(fun () -> Seq.toArray items)

/// <summary>An adaptive list over a fixed array.</summary>
let inline ofArray(items: 'T[]) : alist<'T> =
  new ConstantList<'T>(fun () -> items)

/// <summary>An adaptive list over a fixed list.</summary>
let inline ofList(items: 'T list) : alist<'T> =
  new ConstantList<'T>(fun () -> List.toArray items)

/// <summary>An adaptive list over a fixed ResizeArray.</summary>
let inline ofResizeArray(items: ResizeArray<'T>) : alist<'T> =
  new ConstantList<'T>(fun () -> items.ToArray())

/// <summary>A constant list with a single element.</summary>
let inline single(value: 'T) : alist<'T> =
  new ConstantList<'T>(fun () -> [| value |])

/// <summary>
/// An adaptive list whose content is fixed but computed lazily, once, at
/// first read (FDA parity: the create function runs at most once).
/// </summary>
let inline constant(create: unit -> ResizeArray<'T>) : alist<'T> =
  new ConstantList<'T>(fun () -> create().ToArray())

/// <summary>Alias of <see cref="constant"/> (FDA parity: delay is constant).</summary>
let inline delay(create: unit -> ResizeArray<'T>) : alist<'T> = constant create

/// <summary>Maps every element of the list.</summary>
let inline map (f: 'T -> 'U) (list: alist<'T>) : alist<'U> =
  new FilterMapListNode<'T, 'U>(list, fun _ x -> ValueSome(f x))

/// <summary>Maps every element, keeping only the ones the mapping returns a value for.</summary>
let inline choose (f: 'T -> 'U option) (list: alist<'T>) : alist<'U> =
  new FilterMapListNode<'T, 'U>(
    list,
    fun _ x ->
      match f x with
      | Some u -> ValueSome u
      | None -> ValueNone
  )

/// <summary>Maps every element, keeping only the ones the mapping returns a value for (voption form).</summary>
let inline chooseV (f: 'T -> 'U voption) (list: alist<'T>) : alist<'U> =
  new FilterMapListNode<'T, 'U>(list, fun _ x -> f x)

/// <summary>Keeps the elements that satisfy the predicate.</summary>
let inline filter (predicate: 'T -> bool) (list: alist<'T>) : alist<'T> =
  new FilterMapListNode<'T, 'T>(
    list,
    fun _ x -> if predicate x then ValueSome x else ValueNone
  )

/// <summary>
/// Maps every element, passing the input position to the mapping (FDA
/// <c>AList.mapi</c> parity; the index is the <c>int</c> input position,
/// the positional deviation).
/// </summary>
let inline mapi (f: int -> 'T -> 'U) (list: alist<'T>) : alist<'U> =
  new FilterMapListNode<'T, 'U>(list, fun i x -> ValueSome(f i x))

/// <summary>Keeps the entries whose index-aware mapping returns a value (FDA <c>AList.choosei</c> parity).</summary>
let inline choosei (f: int -> 'T -> 'U option) (list: alist<'T>) : alist<'U> =
  new FilterMapListNode<'T, 'U>(
    list,
    fun i x ->
      match f i x with
      | Some u -> ValueSome u
      | None -> ValueNone
  )

/// <summary>Keeps the entries whose index-aware mapping returns a value (the voption counterpart of <see cref="choosei"/>).</summary>
let inline chooseiV (f: int -> 'T -> 'U voption) (list: alist<'T>) : alist<'U> =
  new FilterMapListNode<'T, 'U>(list, fun i x -> f i x)

/// <summary>Keeps the elements whose index-aware predicate holds (FDA <c>AList.filteri</c> parity).</summary>
let inline filteri
  (predicate: int -> 'T -> bool)
  (list: alist<'T>)
  : alist<'T> =
  new FilterMapListNode<'T, 'T>(
    list,
    fun i x -> if predicate i x then ValueSome x else ValueNone
  )

/// <summary>
/// An adaptive list of the elements paired with their input positions
/// (FDA <c>AList.indexed</c> parity; struct pair, the library convention;
/// the position is the <c>int</c> input position).
/// </summary>
let inline indexed(list: alist<'T>) : alist<struct (int * 'T)> =
  mapi (fun i v -> struct (i, v)) list

/// <summary>
/// Adaptively maps every element of the list to an adaptive value (FDA
/// <c>AList.mapA</c> parity). The output follows the aval returned for
/// each element; writes to the avals deliver targeted deltas.
/// </summary>
let inline mapA (mapping: 'T -> aval<'U>) (list: alist<'T>) : alist<'U> =
  new ElementListNode<'T, 'U>(list, fun _ x -> AVal.map ValueSome (mapping x))

/// <summary>
/// Adaptively maps every element of the list to an adaptive value, keeping
/// only the elements whose aval holds <c>Some</c> (FDA
/// <c>AList.chooseA</c> parity).
/// </summary>
let inline chooseA
  (mapping: 'T -> aval<'U option>)
  (list: alist<'T>)
  : alist<'U> =
  new ElementListNode<'T, 'U>(
    list,
    fun _ x -> AVal.map Option.toValueOption (mapping x)
  )

/// <summary>
/// The voption counterpart of <see cref="chooseA"/>: the mapping returns
/// <c>aval&lt;'U voption&gt;</c> directly, without the option-to-voption
/// wrapper node per element (the no-allocation path).
/// </summary>
let inline chooseAV
  (mapping: 'T -> aval<'U voption>)
  (list: alist<'T>)
  : alist<'U> =
  new ElementListNode<'T, 'U>(list, fun _ x -> mapping x)

/// <summary>
/// Adaptively keeps the elements whose predicate aval holds <c>true</c>
/// (FDA <c>AList.filterA</c> parity).
/// </summary>
let inline filterA (predicate: 'T -> aval<bool>) (list: alist<'T>) : alist<'T> =
  new ElementListNode<'T, 'T>(
    list,
    fun _ x ->
      AVal.map (fun b -> if b then ValueSome x else ValueNone) (predicate x)
  )

/// <summary>
/// Adaptively maps every element of the list to an adaptive value, passing
/// the input position to the mapping (FDA <c>AList.mapiA</c> parity;
/// FDA passes an <c>Index</c>, we pass the <c>int</c> position).
/// </summary>
let inline mapiA
  (mapping: int -> 'T -> aval<'U>)
  (list: alist<'T>)
  : alist<'U> =
  new ElementListNode<'T, 'U>(list, fun i x -> AVal.map ValueSome (mapping i x))

/// <summary>
/// Adaptively maps every element of the list to an adaptive value, keeping
/// only the elements whose aval holds <c>Some</c>, passing the input
/// position to the mapping (FDA <c>AList.chooseiA</c> parity).
/// </summary>
let inline chooseiA
  (mapping: int -> 'T -> aval<'U option>)
  (list: alist<'T>)
  : alist<'U> =
  new ElementListNode<'T, 'U>(
    list,
    fun i x -> AVal.map Option.toValueOption (mapping i x)
  )

/// <summary>
/// The voption counterpart of <see cref="chooseiA"/>: the mapping returns
/// <c>aval&lt;'U voption&gt;</c> directly, without the option-to-voption
/// wrapper node per element (the no-allocation path).
/// </summary>
let inline chooseiAV
  (mapping: int -> 'T -> aval<'U voption>)
  (list: alist<'T>)
  : alist<'U> =
  new ElementListNode<'T, 'U>(list, fun i x -> mapping i x)

/// <summary>
/// Adaptively keeps the elements whose predicate aval holds <c>true</c>,
/// passing the input position to the predicate (FDA <c>AList.filteriA</c>
/// parity).
/// </summary>
let inline filteriA
  (predicate: int -> 'T -> aval<bool>)
  (list: alist<'T>)
  : alist<'T> =
  new ElementListNode<'T, 'T>(
    list,
    fun i x ->
      AVal.map (fun b -> if b then ValueSome x else ValueNone) (predicate i x)
  )

/// <summary>The concatenation of two lists (FDA <c>AList.append</c> parity).</summary>
let inline append (left: alist<'T>) (right: alist<'T>) : alist<'T> =
  new AppendListNode<'T>(left, right)

/// <summary>
/// Returns a transient view of the current state. Valid only until the next
/// write; do not retain or mutate it. Use <see cref="force"/> to
/// materialize a snapshot that is safe to retain.
/// </summary>
let inline getValue(list: alist<'T>) = list.GetValue()

/// <summary>
/// Materializes the current state as a fresh array. This is the only list
/// operation that allocates; the result is safe to retain and the library
/// never touches it again. Runs the pending delta processing (drain) first.
/// </summary>
let inline force(list: alist<'T>) : 'T[] = Seq.toArray(list.GetValue())

/// <summary>Materializes the F# <c>list</c> counterpart.</summary>
let inline toList(list: alist<'T>) : 'T list = List.ofSeq(list.GetValue())

/// <summary>Materializes the array counterpart.</summary>
let inline toArray(list: alist<'T>) : 'T[] = Seq.toArray(list.GetValue())

/// <summary>Adaptively gets the number of elements.</summary>
let inline count(list: alist<'T>) : aval<int> =
  new ListCountNode<'T, int>(list, id)

/// <summary>
/// Adaptively tests if the list is empty. Incremental: only a change that
/// crosses the empty/non-empty boundary re-evaluates this value or its
/// dependents.
/// </summary>
let inline isEmpty(list: alist<'T>) : aval<bool> =
  new ListCountNode<'T, bool>(list, fun c -> c = 0)

/// <summary>
/// Adaptively reduces the list with the given <see cref="AdaptiveReduction"/>
/// (FDA <c>AList.reduce</c> parity). The reduction state is maintained per
/// delta; a reduction that cannot invert a removal recomputes (e.g.
/// <see cref="AdaptiveReduction.fold"/>). Order-sensitive reductions are
/// the caller's contract (the add/sub must be delta-consistent).
/// </summary>
let inline reduce
  (reduction: AdaptiveReduction<'a, 's, 'v>)
  (list: alist<'a>)
  : aval<'v> =
  new ListReduceNode<'a, 'a, 's, 'v>(list, (fun v -> v), reduction)

/// <summary>
/// Maps every element, then reduces the mapped values with the given
/// <see cref="AdaptiveReduction"/> (FDA <c>AList.reduceBy</c> parity). The
/// mapping runs per delta entry.
/// </summary>
let inline reduceBy
  (reduction: AdaptiveReduction<'b, 's, 'v>)
  (mapping: 'a -> 'b)
  (list: alist<'a>)
  : aval<'v> =
  new ListReduceNode<'a, 'b, 's, 'v>(list, mapping, reduction)

/// <summary>
/// Adaptively folds the list with <c>add</c>; every removal recomputes the
/// whole fold (FDA <c>AList.fold</c> parity).
/// </summary>
let inline fold (add: 's -> 'a -> 's) (zero: 's) (list: alist<'a>) : aval<'s> =
  reduceBy (AdaptiveReduction.fold zero add) (fun v -> v) list

/// <summary>
/// Adaptively folds the list with an invertible <c>subtract</c>: removals
/// update the state without a recompute (FDA <c>AList.foldGroup</c>
/// parity; the add/sub must be delta-consistent, see <see cref="reduce"/>).
/// </summary>
let inline foldGroup
  (add: 's -> 'a -> 's)
  (subtract: 's -> 'a -> 's)
  (zero: 's)
  (list: alist<'a>)
  : aval<'s> =
  reduceBy (AdaptiveReduction.group zero add subtract) (fun v -> v) list

/// <summary>
/// Adaptively folds the list with a partially invertible
/// <c>trySubtract</c>: removals that cannot be inverted recompute the
/// whole fold (FDA <c>AList.foldHalfGroup</c> parity).
/// </summary>
let inline foldHalfGroup
  (add: 's -> 'a -> 's)
  (trySubtract: 's -> 'a -> 's voption)
  (zero: 's)
  (list: alist<'a>)
  : aval<'s> =
  reduceBy (AdaptiveReduction.halfGroup zero add trySubtract) (fun v -> v) list

/// <summary>Adaptively tests if any element satisfies the predicate (FDA <c>AList.exists</c> parity).</summary>
let inline exists (predicate: 'T -> bool) (list: alist<'T>) : aval<bool> =
  let reduction =
    AdaptiveReduction.countPositive |> AdaptiveReduction.mapOut(fun c -> c <> 0)

  new ListReduceNode<'T, bool, int, bool>(list, predicate, reduction)

/// <summary>Adaptively tests if every element satisfies the predicate (FDA <c>AList.forall</c> parity).</summary>
let inline forall (predicate: 'T -> bool) (list: alist<'T>) : aval<bool> =
  new ListReduceNode<'T, bool, int, bool>(
    list,
    predicate,
    AdaptiveReduction.countNegative |> AdaptiveReduction.mapOut(fun c -> c = 0)
  )

/// <summary>Adaptively counts the elements that satisfy the predicate (FDA <c>AList.countBy</c> parity).</summary>
let inline countBy (predicate: 'T -> bool) (list: alist<'T>) : aval<int> =
  new ListReduceNode<'T, bool, int, int>(
    list,
    predicate,
    AdaptiveReduction.countPositive
  )

/// <summary>Adaptively gets the minimum element, or <c>ValueNone</c> when empty (FDA <c>AList.tryMin</c> parity).</summary>
let inline tryMin(list: alist<'T>) : aval<'T voption> =
  reduce (AdaptiveReduction.tryMin()) list

/// <summary>Adaptively gets the maximum element, or <c>ValueNone</c> when empty (FDA <c>AList.tryMax</c> parity).</summary>
let inline tryMax(list: alist<'T>) : aval<'T voption> =
  reduce (AdaptiveReduction.tryMax()) list

/// <summary>Adaptively sums the elements (FDA <c>AList.sum</c> parity; needs an additive numeric type).</summary>
let inline sum(list: alist<'T>) : aval<'T> =
  reduce (AdaptiveReduction.sum()) list

/// <summary>Adaptively sums the mapped elements (FDA <c>AList.sumBy</c> parity).</summary>
let inline sumBy (mapping: 'T -> 'U) (list: alist<'T>) : aval<'U> =
  reduceBy (AdaptiveReduction.sum()) mapping list

/// <summary>
/// Adaptively averages the elements (needs a numeric type with
/// <c>DivideByInt</c>, e.g. <c>float</c>; FDA <c>AList.average</c> parity).
/// </summary>
let inline average(list: alist< ^T >) : aval< ^T > =
  AVal.map2
    (fun total c -> LanguagePrimitives.DivideByInt total c)
    (sum list)
    (count list)

/// <summary>
/// Adaptively averages the mapped elements (needs a numeric type with
/// <c>DivideByInt</c>, e.g. <c>float</c>; FDA <c>AList.averageBy</c> parity).
/// </summary>
let inline averageBy (mapping: 'T -> ^U) (list: alist<'T>) : aval< ^U > =
  AVal.map2
    (fun total c -> LanguagePrimitives.DivideByInt total c)
    (sumBy mapping list)
    (count list)

// =========================================================================
// The *A reductions: composition over mapA/filterA + the existing
// reduction nodes. Value-only mapping ('T -> aval<'U>), FDA parity: the
// mapped value follows the ELEMENT, not the position. No new node types.
// FDA argument order: reduction, mapping, list.
// =========================================================================

/// <summary>
/// Adaptively reduces the list after mapping every element to an adaptive
/// value (the AList counterpart of <c>ASet.reduceByA</c>). The mapped
/// values keep their multiplicity (a list has no deduplication).
/// </summary>
let inline reduceByA
  (reduction: AdaptiveReduction<'U, 's, 'v>)
  (mapping: 'T -> aval<'U>)
  (list: alist<'T>)
  : aval<'v> =
  list |> mapA mapping |> reduceBy reduction id

/// <summary>Adaptively counts the elements whose predicate aval holds <c>true</c> (the AList counterpart of <c>ASet.countByA</c>).</summary>
let inline countByA
  (predicate: 'T -> aval<bool>)
  (list: alist<'T>)
  : aval<int> =
  list |> filterA predicate |> count

/// <summary>Adaptively tests if any element's predicate aval holds <c>true</c> (the AList counterpart of <c>ASet.existsA</c>).</summary>
let inline existsA
  (predicate: 'T -> aval<bool>)
  (list: alist<'T>)
  : aval<bool> =
  list |> countByA predicate |> AVal.map(fun c -> c <> 0)

/// <summary>Adaptively tests if every element's predicate aval holds <c>true</c> (the AList counterpart of <c>ASet.forallA</c>).</summary>
let inline forallA
  (predicate: 'T -> aval<bool>)
  (list: alist<'T>)
  : aval<bool> =
  list
  |> filterA(fun x -> AVal.map not (predicate x))
  |> count
  |> AVal.map(fun c -> c = 0)

/// <summary>Adaptively sums the avals mapped from the elements (the AList counterpart of <c>ASet.sumByA</c>).</summary>
let inline sumByA (mapping: 'T -> aval<'U>) (list: alist<'T>) : aval<'U> =
  reduceByA (AdaptiveReduction.sum()) mapping list

/// <summary>
/// Adaptively averages the avals mapped from the elements (needs a
/// numeric type with <c>DivideByInt</c>, e.g. <c>float</c>; the AList
/// counterpart of <c>ASet.averageByA</c>).
/// </summary>
let inline averageByA (mapping: 'T -> aval<'U>) (list: alist<'T>) : aval<'U> =
  AVal.map2
    (fun total count -> LanguagePrimitives.DivideByInt total count)
    (reduceByA (AdaptiveReduction.sum()) mapping list)
    (count list)

/// <summary>
/// Adaptively gets the minimum of the avals mapped from the elements, or
/// <c>ValueNone</c> when empty (the voption counterpart of the <c>*A</c>
/// family, mirroring <see cref="tryMin"/>).
/// </summary>
let inline tryMinA
  (mapping: 'T -> aval<'U>)
  (list: alist<'T>)
  : aval<'U voption> =
  reduceByA (AdaptiveReduction.tryMin()) mapping list

/// <summary>
/// Adaptively gets the maximum of the avals mapped from the elements, or
/// <c>ValueNone</c> when empty (the voption counterpart of the <c>*A</c>
/// family, mirroring <see cref="tryMax"/>).
/// </summary>
let inline tryMaxA
  (mapping: 'T -> aval<'U>)
  (list: alist<'T>)
  : aval<'U voption> =
  reduceByA (AdaptiveReduction.tryMax()) mapping list

/// <summary>
/// An adaptive list over an adaptive value of a sequence (FDA
/// <c>AList.ofAVal</c> parity). Every change of the value replaces the
/// whole state and emits the positional diff as the delta.
/// </summary>
let inline ofAVal<'T, 'S when 'S :> seq<'T>>(value: aval<'S>) : alist<'T> =
  new OfAvalListNode<'T, 'S>(value)

/// <summary>
/// An adaptive list generated from a count and a generator (FDA
/// <c>AList.init</c> parity). The list is rebuilt when the count changes.
/// </summary>
let inline init (f: int -> 'T) (count: aval<int>) : alist<'T> =
  ofAVal(AVal.map (fun c -> Array.init c f) count)

/// <summary>
/// An adaptive numeric range as a list (FDA <c>AList.range</c> parity).
/// The list is rebuilt when either bound changes; the bounds are inclusive.
/// </summary>
let inline range (min: aval< ^T >) (max: aval< ^T >) : alist< ^T > =
  ofAVal(AVal.map2 (fun lo hi -> seq { lo..hi }) min max)

/// <summary>
/// Adaptively looks up the element at the given position (FDA
/// <c>AList.tryAt</c> parity; the position is the <c>int</c> input
/// position, the positional deviation). Per-position precise (O(1) on
/// read) for a direct changeable source: an op that does not touch the
/// position (an insert or remove after it, an update elsewhere) does not
/// re-evaluate this value or its dependents. On a derived source the
/// branch re-evaluates at most once per upstream change (pull-lazy: the
/// gate runs at the next read's drain).
/// </summary>
let inline tryAt (index: int) (list: alist<'T>) : aval<'T voption> =
  new ListLookupNode<'T>(list, index)

/// <summary>Alias of <see cref="tryAt"/> (FDA parity name; both take the <c>int</c> position).</summary>
let inline tryGet (index: int) (list: alist<'T>) : aval<'T voption> =
  tryAt index list

/// <summary>
/// Adaptively gets the first element, or <c>ValueNone</c> when empty (FDA
/// <c>AList.tryFirst</c> parity). Per-position precise, like
/// <see cref="tryAt"/>.
/// </summary>
let inline tryFirst(list: alist<'T>) : aval<'T voption> =
  new ListLookupNode<'T>(list, 0)

/// <summary>
/// Adaptively gets the last element, or <c>ValueNone</c> when empty (FDA
/// <c>AList.tryLast</c> parity). Per-position precise: only an op that
/// changes the last element (append, remove or update of the last
/// element) re-evaluates this value or its dependents.
/// </summary>
let inline tryLast(list: alist<'T>) : aval<'T voption> =
  new ListLastNode<'T>(list)

/// <summary>
/// Materializes the list as an adaptive value. Every change materializes a
/// new array (the retain boundary, like <see cref="force"/>); the value
/// is safe to retain (FDA <c>AList.toAVal</c> parity, as <c>aval&lt;'T[]&gt;</c>,
/// the positional deviation).
/// </summary>
let inline toAVal(list: alist<'T>) : aval<'T[]> =
  new AdaptiveNode<'T[]>(fun () -> Seq.toArray(list.GetValue()))

/// <summary>
/// An adaptive set of the list's elements, deduplicated (FDA
/// <c>AList.toASet</c> parity). An element leaves the output only when its
/// last occurrence leaves.
/// </summary>
let inline toASet(list: alist<'T>) : aset<'T> = new ToSetListNode<'T>(list)

/// <summary>
/// An adaptive set of the elements paired with their input positions
/// (FDA <c>AList.toIndexedASet</c> parity; struct pairs, the library
/// convention).
/// </summary>
let inline toIndexedASet(list: alist<'T>) : aset<struct (int * 'T)> =
  list |> indexed |> toASet

/// <summary>
/// An adaptive list of a set's elements (FDA <c>AList.ofASet</c> parity,
/// poll node). The order is the set's iteration order, stable while the
/// set does not change.
/// </summary>
let inline ofASet(set: aset<'T>) : alist<'T> = new SetToListNode<'T>(set)

/// <summary>
/// Reverses the list (FDA <c>AList.rev</c> parity, poll node).
/// </summary>
let inline rev(list: alist<'T>) : alist<'T> =
  new PollListSourceNode<'T, 'T>(
    list,
    fun view ->
      let view = Collections.asResizeList view
      let next = ResizeArray<'T>(view.Count)

      for i in view.Count - 1 .. -1 .. 0 do
        next.Add view[i]

      next
  )

/// <summary>
/// Adaptively maps over the given value and returns the resulting list
/// (FDA <c>AList.bind</c> parity). When the value changes, <c>mapping</c>
/// selects the new inner list; the output is rebuilt on any change (the
/// value's or the inner list's), emitting the positional diff.
/// </summary>
let inline bind (mapping: 'T -> alist<'U>) (value: aval<'T>) : alist<'U> =
  new BindListNode<'T, 'U>(value, mapping)

/// <summary>
/// Adaptively maps over the two values and returns the resulting list
/// (FDA <c>AList.bind2</c> parity). Composed as one bind over the mapped
/// pair (the ASet lesson: nested binds miss the inner bind's swap, which
/// signals by version only, not by delta).
/// </summary>
let inline bind2
  (mapping: 'A -> 'B -> alist<'C>)
  (a: aval<'A>)
  (b: aval<'B>)
  : alist<'C> =
  bind (fun (av, bv) -> mapping av bv) (AVal.map2 (fun av bv -> (av, bv)) a b)

/// <summary>
/// Adaptively maps over the three values and returns the resulting list
/// (FDA <c>AList.bind3</c> parity).
/// </summary>
let inline bind3
  (mapping: 'A -> 'B -> 'C -> alist<'D>)
  (a: aval<'A>)
  (b: aval<'B>)
  (c: aval<'C>)
  : alist<'D> =
  bind
    (fun (av, bv, cv) -> mapping av bv cv)
    (AVal.map3 (fun av bv cv -> (av, bv, cv)) a b c)

/// <summary>
/// Concatenates a fixed sequence of lists (FDA <c>AList.concat</c> parity,
/// poll node; generalizes <see cref="append"/>).
/// </summary>
let inline concat(lists: #seq<alist<'T>>) : alist<'T> =
  new ConcatListNode<'T>(Seq.toArray lists)

/// <summary>
/// The window <c>[offset, offset + count)</c> of the list (FDA
/// <c>AList.subA</c> parity, poll node; the bounds are adaptive).
/// </summary>
let inline subA
  (offset: aval<int>)
  (count: aval<int>)
  (list: alist<'T>)
  : alist<'T> =
  new SubListNode<'T>(list, offset, count)

/// <summary>The window <c>[offset, offset + count)</c> of the list (FDA <c>AList.sub</c> parity).</summary>
let inline sub (offset: int) (count: int) (list: alist<'T>) : alist<'T> =
  subA (AVal.constant offset) (AVal.constant count) list

/// <summary>The first <c>count</c> elements (FDA <c>AList.takeA</c> parity).</summary>
let inline takeA (count: aval<int>) (list: alist<'T>) : alist<'T> =
  subA (AVal.constant 0) count list

/// <summary>The first <c>count</c> elements (FDA <c>AList.take</c> parity).</summary>
let inline take (count: int) (list: alist<'T>) : alist<'T> =
  takeA (AVal.constant count) list

/// <summary>All elements after the first <c>count</c> (FDA <c>AList.skipA</c> parity).</summary>
let inline skipA (count: aval<int>) (list: alist<'T>) : alist<'T> =
  subA count (AVal.constant System.Int32.MaxValue) list

/// <summary>All elements after the first <c>count</c> (FDA <c>AList.skip</c> parity).</summary>
let inline skip (count: int) (list: alist<'T>) : alist<'T> =
  skipA (AVal.constant count) list

/// <summary>
/// Sorts the list with the given comparison (FDA <c>AList.sortWith</c>
/// parity, stable, poll node).
/// </summary>
let inline sortWith (comparer: 'T -> 'T -> int) (list: alist<'T>) : alist<'T> =
  new SortListNode<'T, 'T>(list, (fun _ v -> v), comparer)

/// <summary>Sorts the list ascending (FDA <c>AList.sort</c> parity, stable, poll node).</summary>
let inline sort(list: alist<'T>) : alist<'T> = sortWith compare list

/// <summary>Sorts the list descending (FDA <c>AList.sortDescending</c> parity, stable, poll node).</summary>
let inline sortDescending(list: alist<'T>) : alist<'T> =
  sortWith (fun a b -> compare b a) list

/// <summary>
/// Sorts the list by the keys given by the projection (FDA
/// <c>AList.sortBy</c> parity, stable, poll node).
/// </summary>
let inline sortBy (f: 'T -> 'K) (list: alist<'T>) : alist<'T> =
  new SortListNode<'T, 'K>(list, (fun _ v -> f v), compare)

/// <summary>
/// Sorts the list by the keys given by the projection, passing the input
/// position to the projection (FDA <c>AList.sortByi</c> parity, stable,
/// poll node; the index is the <c>int</c> input position).
/// </summary>
let inline sortByi (f: int -> 'T -> 'K) (list: alist<'T>) : alist<'T> =
  new SortListNode<'T, 'K>(list, f, compare)

/// <summary>Sorts the list by the keys given by the projection, descending (FDA <c>AList.sortByDescending</c> parity).</summary>
let inline sortByDescending (f: 'T -> 'K) (list: alist<'T>) : alist<'T> =
  new SortListNode<'T, 'K>(list, (fun _ v -> f v), fun a b -> compare b a)

/// <summary>Sorts the list by the keys given by the projection, descending, index-aware (FDA <c>AList.sortByDescendingi</c> parity).</summary>
let inline sortByDescendingi
  (f: int -> 'T -> 'K)
  (list: alist<'T>)
  : alist<'T> =
  new SortListNode<'T, 'K>(list, f, fun a b -> compare b a)

/// <summary>
/// An adaptive list of adjacent pairs (FDA <c>AList.pairwise</c> parity,
/// poll node; struct pairs, the library convention).
/// </summary>
let inline pairwise(list: alist<'T>) : alist<struct ('T * 'T)> =
  new PollListSourceNode<'T, struct ('T * 'T)>(
    list,
    fun view ->
      let view = Collections.asResizeList view
      let next = ResizeArray<struct ('T * 'T)>(max 0 (view.Count - 1))

      for i in 0 .. view.Count - 2 do
        next.Add(struct (view[i], view[i + 1]))

      next
  )

/// <summary>
/// An adaptive list of adjacent pairs, with the last element paired with
/// the first (FDA <c>AList.pairwiseCyclic</c> parity, poll node).
/// </summary>
let inline pairwiseCyclic(list: alist<'T>) : alist<struct ('T * 'T)> =
  new PollListSourceNode<'T, struct ('T * 'T)>(
    list,
    fun view ->
      let view = Collections.asResizeList view
      let next = ResizeArray<struct ('T * 'T)>(view.Count)

      for i in 0 .. view.Count - 1 do
        next.Add(struct (view[i], view[(i + 1) % view.Count]))

      next
  )

/// <summary>
/// Maps every element, disposing the mapped value when the element leaves
/// its position (FDA <c>AList.mapUse</c> parity). The output is 1:1 with
/// the input. Disposing the returned disposable disposes all live mapped
/// values and clears the output.
/// </summary>
let inline mapUse
  (mapping: 'T -> 'W)
  (list: alist<'T>)
  : IDisposable * alist<'W> =
  let node = new MapUseListNode<'T, 'W>(list, fun _ v -> mapping v)
  (node :> IDisposable, node :> alist<'W>)

/// <summary>
/// Maps every element, passing the input position to the mapping and
/// disposing the mapped value when the element leaves its position (FDA
/// <c>AList.mapUsei</c> parity; the index is the <c>int</c> input
/// position, the positional deviation).
/// </summary>
let inline mapUsei
  (mapping: int -> 'T -> 'W)
  (list: alist<'T>)
  : IDisposable * alist<'W> =
  let node = new MapUseListNode<'T, 'W>(list, mapping)
  (node :> IDisposable, node :> alist<'W>)

/// <summary>
/// Creates an adaptive list from an external snapshot function and an
/// invalidate handle (FDA <c>AList.ofExternal</c> parity). The snapshot
/// runs at most once per invalidate, on the next read, and is diffed
/// against the previous snapshot positionally (prefix/suffix, the
/// <c>ChangeableList.ApplyDiff</c> algorithm); not invalidated → reads
/// are O(1) and allocate nothing. The handle is O(1) to call.
/// </summary>
let inline ofExternal
  (snapshot: unit -> IReadOnlyList<'T>)
  : alist<'T> * (unit -> unit) =
  let node = new ExternalListNode<'T>(snapshot)
  (node :> alist<'T>, fun () -> node.Invalidate())

/// <summary>
/// An adaptive list driven by a compute function (FDA <c>AList.custom</c>
/// parity, pull model). The compute receives the current view and a delta
/// builder; it appends the operations that describe the change since the
/// previous call (for example, consuming its own event queue). The
/// operations are positional and applied in order.
/// </summary>
let inline custom
  (compute: IReadOnlyList<'T> -> ListDeltaBuilder<'T> -> unit)
  : alist<'T> =
  new CustomListNode<'T>(compute)
