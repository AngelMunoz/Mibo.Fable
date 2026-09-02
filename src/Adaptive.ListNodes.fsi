namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Web port of Mibo.Adaptive's Core/Collections/ListNodes.fs: the adaptive
// list nodes. Positional operations over ordered journals; a node applies
// journal operations sequentially to its internal ResizeArray and emits the
// translated output delta. Initial loads read the source view first and
// register the sink after.

/// <summary>
/// A constant list: the content is fixed but computed lazily, once, at first
/// read (FDA parity: the create function runs at most once).
/// </summary>
type ConstantList<'T> =
  new: create: (unit -> 'T[]) -> ConstantList<'T>
  interface IAdaptiveList<'T>
  interface IDisposable

/// <summary>
/// Maps every element of a list (or chooses/filters, when the mapping returns
/// <c>ValueNone</c> to drop an element). The output position of a surviving
/// input element is found by binary search over the sorted input positions.
/// </summary>
type FilterMapListNode<'T, 'U> =
  new:
    source: IAdaptiveList<'T> * mapping: (int -> 'T -> 'U voption) ->
      FilterMapListNode<'T, 'U>

  interface IListDeltaSink<'T>
  interface IAdaptiveList<'U>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// The concatenation of two lists. Ops from both sources share one journal in
/// arrival order with a source tag: cross-source order matters.
/// </summary>
type AppendListNode<'T> =
  new: left: IAdaptiveList<'T> * right: IAdaptiveList<'T> -> AppendListNode<'T>
  interface IListDeltaSink<'T>
  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// An adaptive list whose content is driven by a compute function (FDA
/// <c>AList.custom</c> parity). The compute receives the current view and a
/// delta builder; called on every read (poll).
/// </summary>
type CustomListNode<'T when 'T: equality> =
  new:
    compute: (IReadOnlyList<'T> -> ListDeltaBuilder<'T> -> unit) ->
      CustomListNode<'T>

  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// An adaptive set of a list's elements, deduplicated (FDA
/// <c>AList.toASet</c> parity). Per-value occurrence counts: an element
/// leaves the output only when its last occurrence leaves.
/// </summary>
type ToSetListNode<'T when 'T: equality> =
  new: source: IAdaptiveList<'T> -> ToSetListNode<'T>
  interface IListDeltaSink<'T>
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive list of a set's elements (FDA <c>AList.ofASet</c> parity,
/// poll node, version-gated rebuild).
/// </summary>
type SetToListNode<'T when 'T: equality> =
  new: source: IAdaptiveSet<'T> -> SetToListNode<'T>
  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// An adaptive list bound to a scalar value (FDA <c>AList.bind</c> parity):
/// <c>mapping value</c> selects the inner list; rebuild-on-change semantics
/// (full replace deltas).
/// </summary>
type BindListNode<'T, 'U> =
  new:
    value: IAdaptiveValue<'T> * mapping: ('T -> IAdaptiveList<'U>) ->
      BindListNode<'T, 'U>

  interface IAdaptiveList<'U>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// Concatenates a fixed sequence of lists (FDA <c>AList.concat</c> parity,
/// poll node, version-gated rebuild).
/// </summary>
type ConcatListNode<'T> =
  new: sources: IAdaptiveList<'T>[] -> ConcatListNode<'T>
  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// An adaptive list over an adaptive value of a sequence (FDA
/// <c>AList.ofAVal</c> parity). Poll model, version-gated positional diff.
/// </summary>
type OfAvalListNode<'T, 'S when 'S :> seq<'T>> =
  new: value: IAdaptiveValue<'S> -> OfAvalListNode<'T, 'S>
  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// A poll node that rebuilds its output from the source and emits the
/// positional diff (the gap-sheet poll-node strategy). Rebuild-on-every-read
/// is deliberate: the <c>build</c> function may read additional adaptive
/// inputs, so a source-version gate would be unsound.
/// </summary>
type PollListSourceNode<'T, 'U> =
  new:
    source: IAdaptiveList<'T> * build: (IReadOnlyList<'T> -> ResizeArray<'U>) ->
      PollListSourceNode<'T, 'U>

  interface IAdaptiveList<'U>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// The window <c>[offset, offset + count)</c> of a list with adaptive bounds
/// (FDA <c>AList.subA</c> parity, poll node, explicitly gated on all three
/// dependency versions).
/// </summary>
type SubListNode<'T> =
  new:
    source: IAdaptiveList<'T> *
    offset: IAdaptiveValue<int> *
    count: IAdaptiveValue<int> ->
      SubListNode<'T>

  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// A stable sort node (FDA <c>AList.sortWith</c> parity, poll model). The
/// keys are computed with their input positions; the sort is stable by
/// position.
/// </summary>
type SortListNode<'T, 'K> =
  new:
    source: IAdaptiveList<'T> *
    keyMapping: (int -> 'T -> 'K) *
    comparer: ('K -> 'K -> int) ->
      SortListNode<'T, 'K>

  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// Maps every element, disposing the mapped value when the element leaves its
/// position (FDA <c>AList.mapUsei</c> parity; the index is the input
/// position). The output is 1:1 with the input.
/// </summary>
type MapUseListNode<'T, 'W when 'W: equality and 'W :> IDisposable> =
  new:
    source: IAdaptiveList<'T> * mapping: (int -> 'T -> 'W) ->
      MapUseListNode<'T, 'W>

  interface IListDeltaSink<'T>
  interface IAdaptiveList<'W>
  interface IDisposable
  interface IListSinkRegistry

/// <summary>
/// A positional lookup over an adaptive list (the node behind
/// <c>AList.tryAt</c>/<c>AList.tryGet</c>/<c>AList.tryFirst</c>). Registers
/// nothing; the version advances only when the element at the watched
/// position actually changed.
/// </summary>
type ListLookupNode<'T> =
  new: source: IAdaptiveList<'T> * index: int -> ListLookupNode<'T>
  interface IAdaptiveValue<'T voption>
  interface IDisposable

/// <summary>
/// A last-element lookup over an adaptive list (the node behind
/// <c>AList.tryLast</c>). Registers nothing; the version advances only when
/// the last element actually changed.
/// </summary>
type ListLastNode<'T> =
  new: source: IAdaptiveList<'T> -> ListLastNode<'T>
  interface IAdaptiveValue<'T voption>
  interface IDisposable

/// <summary>
/// A count over an adaptive list, projected through <c>view</c> (the node
/// behind <c>AList.count</c> and <c>AList.isEmpty</c>). Registers nothing;
/// the version advances only when the projected output changed.
/// </summary>
type ListCountNode<'T, 'Out> =
  new:
    source: IAdaptiveList<'T> * view: (int -> 'Out) -> ListCountNode<'T, 'Out>

  interface IAdaptiveValue<'Out>
  interface IDisposable
