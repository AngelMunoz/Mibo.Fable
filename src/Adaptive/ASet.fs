module Mibo.Fable.Adaptive.ASet

open System
open System.Collections.Generic

/// <summary>An empty adaptive set (FDA <c>ASet.empty</c> parity).</summary>
let empty<'T> : aset<'T> = new ConstantSet<'T>(fun () -> Seq.empty)

/// <summary>An adaptive set over fixed, immutable items.</summary>
let inline ofSeq(items: seq<'T>) : aset<'T> =
  new ConstantSet<'T>(fun () -> items)

/// <summary>An adaptive set over a fixed array.</summary>
let inline ofArray(items: 'T[]) : aset<'T> =
  new ConstantSet<'T>(fun () -> items)

/// <summary>An adaptive set over a fixed list.</summary>
let inline ofList(items: 'T list) : aset<'T> =
  new ConstantSet<'T>(fun () -> items)

/// <summary>An adaptive set over a fixed HashSet.</summary>
let inline ofHashSet(items: HashSet<'T>) : aset<'T> =
  new ConstantSet<'T>(fun () -> items)

/// <summary>
/// An adaptive set whose content is fixed but computed lazily, once, at
/// first read. Enables self-referential definitions (FDA parity: the
/// create function runs at most once).
/// </summary>
let inline constant(create: unit -> HashSet<'T>) : aset<'T> =
  new ConstantSet<'T>(fun () -> create() :> seq<'T>)

/// <summary>Alias of <see cref="constant"/> (FDA parity: delay is constant).</summary>
let inline delay(create: unit -> HashSet<'T>) : aset<'T> = constant create

/// <summary>Maps every element of the set.</summary>
let inline map (f: 'T -> 'U) (set: aset<'T>) : aset<'U> =
  new MapSetNode<'T, 'U>(set, fun x -> ValueSome(f x))

/// <summary>Maps every element, keeping only the ones the mapping returns a value for.</summary>
let inline choose (f: 'T -> 'U option) (set: aset<'T>) : aset<'U> =
  new MapSetNode<'T, 'U>(set, f >> Option.toValueOption)

/// <summary>Maps every element, keeping only the ones the mapping returns a value for.</summary>
let inline chooseV (f: 'T -> 'U voption) (set: aset<'T>) : aset<'U> =
  new MapSetNode<'T, 'U>(set, f)

/// <summary>Keeps the elements that satisfy the predicate.</summary>
let inline filter (predicate: 'T -> bool) (set: aset<'T>) : aset<'T> =
  new FilterSetNode<'T>(set, predicate)

/// <summary>
/// Adaptively maps every element of the set to an adaptive value (FDA
/// <c>ASet.mapA</c> parity). The output follows the aval returned for
/// each element; writes to the avals deliver targeted deltas.
/// </summary>
let inline mapA (mapping: 'T -> aval<'U>) (set: aset<'T>) : aset<'U> =
  new ElementSetNode<'T, 'U>(set, fun x -> AVal.map ValueSome (mapping x))

/// <summary>
/// Adaptively maps every element of the set to an adaptive value, keeping
/// only the elements whose aval holds <c>Some</c> (FDA
/// <c>ASet.chooseA</c> parity).
/// </summary>
let inline chooseA (mapping: 'T -> aval<'U option>) (set: aset<'T>) : aset<'U> =
  new ElementSetNode<'T, 'U>(
    set,
    fun x -> AVal.map Option.toValueOption (mapping x)
  )

/// <summary>
/// The voption counterpart of <see cref="chooseA"/>: the mapping returns
/// <c>aval&lt;'U voption&gt;</c> directly, without the option-to-voption
/// wrapper node per element (the no-allocation path).
/// </summary>
let inline chooseAV
  (mapping: 'T -> aval<'U voption>)
  (set: aset<'T>)
  : aset<'U> =
  new ElementSetNode<'T, 'U>(set, mapping)

/// <summary>
/// Adaptively keeps the elements whose predicate aval holds <c>true</c>
/// (FDA <c>ASet.filterA</c> parity).
/// </summary>
let inline filterA (predicate: 'T -> aval<bool>) (set: aset<'T>) : aset<'T> =
  new ElementSetNode<'T, 'T>(
    set,
    fun x ->
      AVal.map (fun b -> if b then ValueSome x else ValueNone) (predicate x)
  )

/// <summary>The union of two sets.</summary>
let inline union (left: aset<'T>) (right: aset<'T>) : aset<'T> =
  new UnionSetNode<'T>(left, right)

/// <summary>
/// The union of all given sets. Deviation from FDA: FDA's
/// <c>unionMany</c> takes an adaptive set of sets (the dynamic form lands
/// with <c>bind</c>/<c>collect</c>); this overload takes a static sequence
/// and folds <see cref="union"/>.
/// </summary>
let unionMany(sets: seq<aset<'T>>) : aset<'T> =
  let mutable acc = Unchecked.defaultof<aset<'T>>
  let mutable first = true

  for s in sets do
    if first then
      acc <- s
      first <- false
    else
      acc <- new UnionSetNode<'T>(acc, s)

  if first then
    new ConstantSet<'T>(fun () -> Seq.empty) :> aset<_>
  else
    acc

/// <summary>
/// Adaptively maps over the given set and unions all resulting sets (FDA
/// <c>ASet.collect</c> parity). The output is the refcounted union: an
/// element contributed by several inner sets disappears only when the last
/// contributor drops it. This is also the dynamic <c>unionMany</c>:
/// <c>ASet.collect id</c> over <c>IAdaptiveSet&lt;IAdaptiveSet&lt;'T&gt;&gt;</c>.
/// </summary>
let inline collect (mapping: 'T -> aset<'U>) (set: aset<'T>) : aset<'U> =
  new CollectSetNode<'T, 'U>(set, mapping)

/// <summary>
/// Flattens the set by statically expanding each element to a sequence
/// (FDA <c>ASet.collect'</c> parity). The expansion runs per source
/// element change; the inner sequences are constant.
/// </summary>
let inline collect' (mapping: 'T -> seq<'U>) (set: aset<'T>) : aset<'U> =
  collect (mapping >> ofSeq) set

/// <summary>
/// Maps every element, disposing the mapped value when its last source
/// occurrence leaves (FDA <c>ASet.mapUse</c> parity). The mapped values
/// are stable (the mapping runs once per source element). Disposing the
/// returned disposable disposes all live mapped values and clears the
/// output set.
/// </summary>
let inline mapUse (mapping: 'A -> 'B) (set: aset<'A>) : IDisposable * aset<'B> =
  let node = new MapUseSetNode<'A, 'B>(set, mapping)
  (node :> IDisposable, node :> aset<'B>)

/// <summary>The elements of the left set that are not in the right set.</summary>
let inline difference (left: aset<'T>) (right: aset<'T>) : aset<'T> =
  new TwoSourceSetNode<'T>(TwoSetOp.Difference, left, right)

/// <summary>The elements present in both sets.</summary>
let inline intersect (left: aset<'T>) (right: aset<'T>) : aset<'T> =
  new TwoSourceSetNode<'T>(TwoSetOp.Intersect, left, right)

/// <summary>The symmetric difference: elements present in exactly one set.</summary>
let inline xor (left: aset<'T>) (right: aset<'T>) : aset<'T> =
  new TwoSourceSetNode<'T>(TwoSetOp.Xor, left, right)

/// <summary>
/// An adaptive set over an adaptive value of a sequence. Every change of
/// the value replaces the whole state and emits the diff as the delta
/// (FDA <c>ASet.ofAVal</c> parity; the value carries no deltas).
/// </summary>
let inline ofAVal<'T, 'S when 'T: equality and 'S :> seq<'T>>
  (value: aval<'S>)
  : aset<'T> =
  new OfAvalSetNode<'T, 'S>(value)

/// <summary>
/// Adaptively maps over the given value and returns the resulting set (FDA
/// <c>ASet.bind</c> parity). When the value changes, the whole inner set is
/// swapped: the old content is removed, the inner sink is unregistered
/// eagerly, and <c>mapping</c> selects the new inner set. The inner set's
/// own changes propagate while it is bound.
/// </summary>
let inline bind (mapping: 'T -> aset<'U>) (value: aval<'T>) : aset<'U> =
  new BindSetNode<'T, 'U>(value, mapping)

/// <summary>
/// An adaptive numeric range (FDA <c>ASet.range</c> parity). The set is
/// rebuilt when either bound changes; the bounds are inclusive.
/// </summary>
let inline range (min: aval< ^T >) (max: aval< ^T >) : aset< ^T > =
  ofAVal(AVal.map2 (fun lo hi -> seq { lo..hi }) min max)

/// <summary>
/// Adaptively maps over the two values and returns the resulting set (FDA
/// <c>ASet.bind2</c> parity). When either value changes, the whole inner
/// set is swapped (the bind semantics). Composed as one bind over the
/// mapped pair (the FDA approach: nested binds would miss the inner
/// bind's swap, which signals by version only, not by delta).
/// </summary>
let inline bind2
  (mapping: 'A -> 'B -> aset<'C>)
  (a: aval<'A>)
  (b: aval<'B>)
  : aset<'C> =
  bind (fun (av, bv) -> mapping av bv) (AVal.map2 (fun av bv -> (av, bv)) a b)

/// <summary>
/// Adaptively maps over the three values and returns the resulting set
/// (FDA <c>ASet.bind3</c> parity). When any value changes, the whole
/// inner set is swapped (the bind semantics).
/// </summary>
let inline bind3
  (mapping: 'A -> 'B -> 'C -> aset<'D>)
  (a: aval<'A>)
  (b: aval<'B>)
  (c: aval<'C>)
  : aset<'D> =
  bind
    (fun (av, bv, cv) -> mapping av bv cv)
    (AVal.map3 (fun av bv cv -> (av, bv, cv)) a b c)

/// <summary>
/// An adaptive set over an external reader function. The reader is called
/// on every read (poll); the node diffs the result against its state and
/// emits the diff as the delta. Pull-based: nothing tells this node when
/// the underlying data changes, so consumers must re-read it (FDA
/// <c>ASet.ofReader</c> is pull-based too).
/// </summary>
let inline ofReader(reader: unit -> HashSet<'T>) : aset<'T> =
  new ReaderSetNode<'T>(reader)

/// <summary>
/// An adaptive set driven by a compute function (FDA <c>ASet.custom</c>
/// parity, pull model). The compute receives the current view and a delta
/// builder; it appends the operations that describe the change since the
/// previous call (for example, consuming its own event queue).
/// </summary>
let inline custom
  (compute: HashSet<'T> -> SetDeltaBuilder<'T> -> unit)
  : aset<'T> =
  new CustomSetNode<'T>(compute)

/// <summary>
/// Creates an adaptive set from an external snapshot function and an
/// invalidate handle (FDA <c>ASet.ofExternal</c> parity). The snapshot
/// runs at most once per invalidate, on the next read, and is diffed
/// against the previous snapshot; not invalidated → reads are O(1) and
/// allocate nothing. The handle is O(1) to call.
/// </summary>
let inline ofExternal
  (snapshot: unit -> IReadOnlySet<'T>)
  : aset<'T> * (unit -> unit) =
  let node = new ExternalSetNode<'T>(snapshot)
  (node :> aset<'T>, fun () -> node.Invalidate())

/// <summary>
/// Adaptively reduces the set with the given <see cref="AdaptiveReduction"/>.
/// The state is updated incrementally from deltas: added elements apply
/// <c>add</c>; removed elements apply <c>sub</c> (or recompute the whole
/// state when <c>sub</c> returns <c>ValueNone</c>).
/// </summary>
let inline reduce
  (reduction: AdaptiveReduction<'a, 's, 'v>)
  (set: aset<'a>)
  : aval<'v> =
  new SetReduceNode<'a, 'a, 's, 'v>(set, id, reduction)

/// <summary>
/// Maps every element, then reduces the mapped values with the given
/// <see cref="AdaptiveReduction"/>. The mapping runs per delta element.
/// </summary>
let inline reduceBy
  (reduction: AdaptiveReduction<'b, 's, 'v>)
  (mapping: 'a -> 'b)
  (set: aset<'a>)
  : aval<'v> =
  new SetReduceNode<'a, 'b, 's, 'v>(set, mapping, reduction)

/// <summary>
/// Adaptively folds the set with <c>add</c>; every removal recomputes the
/// whole fold (the fold operation is not invertible in general). Use
/// <see cref="foldGroup"/> when the operation has an inverse.
/// </summary>
let inline fold (add: 's -> 'a -> 's) (zero: 's) (set: aset<'a>) : aval<'s> =
  reduce (AdaptiveReduction.fold zero add) set

/// <summary>
/// Adaptively folds the set with an invertible <c>subtract</c>: removals
/// update the state without a recompute.
/// </summary>
let inline foldGroup
  (add: 's -> 'a -> 's)
  (subtract: 's -> 'a -> 's)
  (zero: 's)
  (set: aset<'a>)
  : aval<'s> =
  reduce (AdaptiveReduction.group zero add subtract) set

/// <summary>
/// Adaptively folds the set; a removal applies <c>trySubtract</c> when it
/// returns a value, otherwise the whole fold recomputes.
/// </summary>
let inline foldHalfGroup
  (add: 's -> 'a -> 's)
  (trySubtract: 's -> 'a -> 's voption)
  (zero: 's)
  (set: aset<'a>)
  : aval<'s> =
  reduce (AdaptiveReduction.halfGroup zero add trySubtract) set

/// <summary>
/// Adaptively gets the number of elements. Incremental: maintained per
/// delta, no full rescan.
/// </summary>
let inline count(set: aset<'T>) : aval<int> = new SetCountNode<'T, int>(set, id)

/// <summary>
/// Adaptively tests if the set is empty. Incremental: only a change that
/// crosses the empty/non-empty boundary re-evaluates this value or its
/// dependents.
/// </summary>
let inline isEmpty(set: aset<'T>) : aval<bool> =
  new SetCountNode<'T, bool>(set, fun c -> c = 0)

/// <summary>
/// Adaptively tests if the set contains the given element. Per-element
/// precise (O(1) on read) for a direct changeable source: a write to an
/// unrelated element does not re-evaluate this value or its dependents.
/// On a derived source the branch re-evaluates at most once per upstream
/// change (pull-lazy: the per-element gate runs at the next read's
/// drain).
/// </summary>
let inline contains (value: 'T) (set: aset<'T>) : aval<bool> =
  new SetContainsNode<'T>(set, value)

/// <summary>Adaptively tests if any element satisfies the predicate.</summary>
let inline exists (predicate: 'T -> bool) (set: aset<'T>) : aval<bool> =
  let reduction =
    AdaptiveReduction.countPositive |> AdaptiveReduction.mapOut(fun c -> c <> 0)

  new SetReduceNode<'T, bool, int, bool>(set, predicate, reduction)

/// <summary>Adaptively tests if every element satisfies the predicate.</summary>
let inline forall (predicate: 'T -> bool) (set: aset<'T>) : aval<bool> =
  new SetReduceNode<'T, bool, int, bool>(
    set,
    predicate,
    AdaptiveReduction.countNegative |> AdaptiveReduction.mapOut(fun c -> c = 0)
  )

/// <summary>Adaptively counts the elements that satisfy the predicate.</summary>
let inline countBy (predicate: 'T -> bool) (set: aset<'T>) : aval<int> =
  new SetReduceNode<'T, bool, int, int>(
    set,
    predicate,
    AdaptiveReduction.countPositive
  )

// =========================================================================
// The *A reductions: composition over mapA + the existing reduction nodes.
// No new node types. FDA argument order: reduction, mapping, set.
// =========================================================================

/// <summary>
/// Adaptively reduces the set after mapping every element to an adaptive
/// value (FDA <c>ASet.reduceByA</c> parity). The mapping produces distinct
/// pairs <c>struct (x, v)</c>, so duplicate mapped values keep their
/// multiplicity (a plain mapA would deduplicate them); the reduction
/// projects the value side.
/// </summary>
let inline reduceByA
  (reduction: AdaptiveReduction<'U, 's, 'v>)
  (mapping: 'T -> aval<'U>)
  (set: aset<'T>)
  : aval<'v> =
  set
  |> mapA(fun x -> AVal.map (fun v -> struct (x, v)) (mapping x))
  |> reduceBy reduction (fun struct (_, v) -> v)

/// <summary>
/// Adaptively counts the elements whose predicate aval holds <c>true</c>
/// (FDA <c>ASet.countByA</c> parity). Composition: filterA + count
/// (element-preserving, unlike a bool-mapped reduce).
/// </summary>
let inline countByA (predicate: 'T -> aval<bool>) (set: aset<'T>) : aval<int> =
  set |> filterA predicate |> count

/// <summary>Adaptively tests if any element's predicate aval holds <c>true</c> (FDA <c>ASet.existsA</c> parity).</summary>
let inline existsA (predicate: 'T -> aval<bool>) (set: aset<'T>) : aval<bool> =
  set |> countByA predicate |> AVal.map(fun c -> c <> 0)

/// <summary>Adaptively tests if every element's predicate aval holds <c>true</c> (FDA <c>ASet.forallA</c> parity).</summary>
let inline forallA (predicate: 'T -> aval<bool>) (set: aset<'T>) : aval<bool> =
  set
  |> filterA(fun x -> AVal.map not (predicate x))
  |> count
  |> AVal.map(fun c -> c = 0)

/// <summary>Adaptively sums the avals mapped from the elements (FDA <c>ASet.sumByA</c> parity).</summary>
let inline sumByA (mapping: 'T -> aval<'U>) (set: aset<'T>) : aval<'U> =
  reduceByA (AdaptiveReduction.sum()) mapping set

/// <summary>
/// Adaptively averages the avals mapped from the elements (FDA
/// <c>ASet.averageByA</c> parity; needs a numeric type with
/// <c>DivideByInt</c>, e.g. <c>float</c>).
/// </summary>
let inline averageByA (mapping: 'T -> aval<'U>) (set: aset<'T>) : aval<'U> =
  AVal.map2
    (fun total count -> LanguagePrimitives.DivideByInt total count)
    (reduceByA (AdaptiveReduction.sum()) mapping set)
    (count set)

/// <summary>Adaptively sums the elements.</summary>
let inline sum(set: aset<'T>) : aval<'T> = reduce (AdaptiveReduction.sum()) set

/// <summary>Adaptively sums the mapped elements.</summary>
let inline sumBy (mapping: 'T -> 'U) (set: aset<'T>) : aval<'U> =
  reduceBy (AdaptiveReduction.sum()) mapping set

/// <summary>
/// Adaptively averages the elements (needs a numeric type with
/// <c>DivideByInt</c>, e.g. <c>float</c>). The average is sum/count.
/// </summary>
let inline average(set: aset< ^T >) : aval< ^T > =
  AVal.map2
    (fun total count -> LanguagePrimitives.DivideByInt total count)
    (sum set)
    (count set)

/// <summary>
/// Adaptively averages the mapped elements (needs a numeric type with
/// <c>DivideByInt</c>, e.g. <c>float</c>).
/// </summary>
let inline averageBy (mapping: 'T -> ^U) (set: aset<'T>) : aval< ^U > =
  average(map mapping set)

/// <summary>Adaptively gets the minimum element, or <c>ValueNone</c> when empty.</summary>
let inline tryMin(set: aset<'T>) : aval<'T voption> =
  reduce (AdaptiveReduction.tryMin()) set

/// <summary>Adaptively gets the maximum element, or <c>ValueNone</c> when empty.</summary>
let inline tryMax(set: aset<'T>) : aval<'T voption> =
  reduce (AdaptiveReduction.tryMax()) set

/// <summary>
/// Adaptively gets the minimum of the avals mapped from the elements, or
/// <c>ValueNone</c> when empty (the voption counterpart of the <c>*A</c>
/// family, mirroring <see cref="tryMin"/>).
/// </summary>
let inline tryMinA
  (mapping: 'T -> aval<'U>)
  (set: aset<'T>)
  : aval<'U voption> =
  reduceByA (AdaptiveReduction.tryMin()) mapping set

/// <summary>
/// Adaptively gets the maximum of the avals mapped from the elements, or
/// <c>ValueNone</c> when empty (the voption counterpart of the <c>*A</c>
/// family, mirroring <see cref="tryMax"/>).
/// </summary>
let inline tryMaxA
  (mapping: 'T -> aval<'U>)
  (set: aset<'T>)
  : aval<'U voption> =
  reduceByA (AdaptiveReduction.tryMax()) mapping set

/// <summary>A constant set with a single element.</summary>
let inline single(value: 'T) : aset<'T> =
  new ConstantSet<'T>(fun () -> Seq.singleton value)

/// <summary>
/// Materializes the set as an adaptive value. Every change materializes a
/// new immutable copy (the retain boundary, like <see cref="force"/>); the
/// value is safe to retain.
/// </summary>
let inline toAVal(set: aset<'T>) : aval<HashSet<'T>> =
  new AdaptiveNode<HashSet<'T>>(fun () -> HashSet(set.GetValue()))

/// <summary>
/// Returns a transient view of the current state. Valid only until the next
/// write; do not retain or mutate it. Use <see cref="force"/> to
/// materialize a snapshot that is safe to retain.
/// </summary>
let inline getValue(set: aset<'T>) = set.GetValue()

/// <summary>
/// Materializes the current state as an immutable copy. This is the only
/// collection operation that allocates; the result is safe to retain and
/// the library never touches it again. Runs the pending delta processing
/// (drain) first.
/// </summary>
let inline force(set: aset<'T>) : HashSet<'T> = HashSet(set.GetValue())

/// <summary>
/// The sorted set as a list, using the given comparison (stable, poll
/// node; a sorted set is a list by construction).
/// </summary>
let inline sortWith (comparer: 'T -> 'T -> int) (set: aset<'T>) : alist<'T> =
  new SortListNode<'T, 'T>(new SetToListNode<'T>(set), (fun _ v -> v), comparer)

/// <summary>The sorted set as a list, ascending (stable).</summary>
let inline sort(set: aset<'T>) : alist<'T> = sortWith compare set

/// <summary>The sorted set as a list, descending (stable).</summary>
let inline sortDescending(set: aset<'T>) : alist<'T> =
  sortWith (fun a b -> compare b a) set

/// <summary>
/// The set sorted by the keys given by the projection, as a list (stable).
/// </summary>
let inline sortBy (f: 'T -> 'K) (set: aset<'T>) : alist<'T> =
  new SortListNode<'T, 'K>(
    new SetToListNode<'T>(set),
    (fun _ v -> f v),
    compare
  )

/// <summary>The set sorted by the keys given by the projection, descending (stable).</summary>
let inline sortByDescending (f: 'T -> 'K) (set: aset<'T>) : alist<'T> =
  new SortListNode<'T, 'K>(
    new SetToListNode<'T>(set),
    (fun _ v -> f v),
    fun a b -> compare b a
  )

/// <summary>Materializes the F# <c>Set</c> counterpart (sorted, structural equality).</summary>
let inline toSet(set: aset<'T>) : Set<'T> = Set.ofSeq(set.GetValue())
