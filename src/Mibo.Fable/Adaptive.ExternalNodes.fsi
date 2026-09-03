namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// External sources (MAPA-DESIGN §1.1): web port of ExternalNodes.fs. The
// snapshot is re-read at most once per invalidate, on the next read, and
// diffed against the previous snapshot; the diff is delivered through the
// normal delta machinery. Not invalidated → zero cost.

/// <summary>
/// An adaptive set whose content is supplied by an external snapshot function,
/// re-read only when invalidated via the handle returned by
/// <c>ASet.ofExternal</c> (FDA <c>ASet.ofExternal</c> parity).
/// </summary>
type ExternalSetNode<'T when 'T: equality> =
  new: snapshot: (unit -> IReadOnlySet<'T>) -> ExternalSetNode<'T>
  /// <summary>
  /// The invalidate handle implementation (returned by <c>ASet.ofExternal</c>
  /// as a function). Call this when the external source changed; the re-read
  /// happens on the next read. Not for direct use.
  /// </summary>
  member Invalidate: unit -> unit
  interface IPostSource
  interface IAdaptiveSet<'T>
  interface IDisposable
  interface ISetSinkRegistry

/// <summary>
/// An adaptive map whose content is supplied by an external snapshot function,
/// re-read only when invalidated via the handle returned by
/// <c>AMap.ofExternal</c> (FDA <c>AMap.ofExternal</c> parity).
/// </summary>
type ExternalMapNode<'K, 'V when 'K: equality> =
  new:
    snapshot: (unit -> IReadOnlyDictionary<'K, 'V>) -> ExternalMapNode<'K, 'V>

  /// <summary>
  /// The invalidate handle implementation (returned by <c>AMap.ofExternal</c>
  /// as a function). Not for direct use.
  /// </summary>
  member Invalidate: unit -> unit
  interface IPostSource
  interface IAdaptiveMap<'K, 'V>
  interface IDisposable
  interface IMapSinkRegistry

/// <summary>
/// An adaptive list whose content is supplied by an external snapshot function,
/// re-read only when invalidated via the handle returned by
/// <c>AList.ofExternal</c> (FDA <c>AList.ofExternal</c> parity).
/// </summary>
type ExternalListNode<'T when 'T: equality> =
  new: snapshot: (unit -> IReadOnlyList<'T>) -> ExternalListNode<'T>
  /// <summary>
  /// The invalidate handle implementation (returned by <c>AList.ofExternal</c>
  /// as a function). Not for direct use.
  /// </summary>
  member Invalidate: unit -> unit
  interface IPostSource
  interface IAdaptiveList<'T>
  interface IDisposable
  interface IListSinkRegistry
