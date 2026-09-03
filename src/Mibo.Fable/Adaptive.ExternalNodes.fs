namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// =============================================================================
// External sources (MAPA-DESIGN §1.1): ofExternal — snapshot function +
// invalidate handle. Web port of ExternalNodes.fs.
//
// Semantics: the user provides a snapshot function and receives an invalidate
// handle. The snapshot is re-read at most once per invalidate, on the next
// read, and diffed against the previous snapshot; the diff is delivered
// through the normal delta machinery. Not invalidated → zero cost: no re-read,
// no diff, no allocation.
//
// The invalidate handle is O(1) at call time and deferred (no evaluation
// during the write). Single thread: invalidation is always direct (the
// .NET post-ring branch for foreign threads has no counterpart).
// =============================================================================

/// <summary>
/// An adaptive set whose content is supplied by an external snapshot function,
/// re-read only when invalidated via the handle returned by
/// <c>ASet.ofExternal</c> (FDA <c>ASet.ofExternal</c> parity). The snapshot is
/// materialized into a reused scratch set (the diff helpers require the
/// concrete <see cref="HashSet&lt;'T&gt;"/>); the scratch is refilled only on
/// invalidated polls.
/// </summary>
type ExternalSetNode<'T when 'T: equality>(snapshot: unit -> IReadOnlySet<'T>) =
  // The graph this node belongs to, captured at creation (the ambient graph
  // of the creating thread; on JS the single ambient context).
  let ctx = GraphContext.Current
  let mutable state = SetNodeState.create 0
  let scratch = HashSet<'T>()
  let mutable dirty = true
  let mutable disposed = false

  /// <summary>
  /// The invalidate handle implementation (returned by <c>ASet.ofExternal</c>
  /// as a function). Call this when the external source changed; the re-read
  /// happens on the next read. Not for direct use.
  /// </summary>
  member this.Invalidate() : unit =
    dirty <- true
    ctx.BumpWriteGeneration()

  member private this.Poll() =
    if dirty && not disposed then
      dirty <- false
      scratch.Clear()

      for x in snapshot() do
        scratch.Add x |> ignore

      if Collections.rebuildSetDiff scratch state then
        state.Version <- state.Version + 1L
        Collections.pushAndBumpSet ctx state.Out state.Sinks
        state.Out.Clear()

  interface IPostSource with
    member this.ApplyPosted() = this.Invalidate()

  interface IAdaptiveSet<'T> with
    member this.GetValue() =
      ctx.ClaimOwner()

      try
        if disposed then
          invalidOp "This adaptive set has been disposed."

        this.Poll()
        AdaptiveRuntime.addDependency (this :> IAdaptiveObject) state.Version
        state.Set.Data :> IReadOnlySet<'T>
      finally
        ctx.ReleaseOwner()

    member this.Version =
      this.Poll()
      state.Version

  interface IDisposable with
    member this.Dispose() =
      if not disposed then
        disposed <- true
        Collections.clearSinks state.Sinks

  interface ISetSinkRegistry with
    member this.AddSetSink(sink) = Collections.addSink state.Sinks sink

    member this.RemoveSetSink(sink) = Collections.removeSink state.Sinks sink

/// <summary>
/// An adaptive map whose content is supplied by an external snapshot function,
/// re-read only when invalidated via the handle returned by
/// <c>AMap.ofExternal</c> (FDA <c>AMap.ofExternal</c> parity). The snapshot is
/// materialized into a reused scratch dictionary; the scratch is refilled
/// only on invalidated polls.
/// </summary>
type ExternalMapNode<'K, 'V when 'K: equality>
  (snapshot: unit -> IReadOnlyDictionary<'K, 'V>) =
  let ctx = GraphContext.Current
  let mutable state = MapNodeState.create 0
  let scratch = Dictionary<'K, 'V>()
  let mutable dirty = true
  let mutable disposed = false

  /// <summary>
  /// The invalidate handle implementation (returned by <c>AMap.ofExternal</c>
  /// as a function). Not for direct use.
  /// </summary>
  member this.Invalidate() : unit =
    dirty <- true
    ctx.BumpWriteGeneration()

  member private this.Poll() =
    if dirty && not disposed then
      dirty <- false
      scratch.Clear()

      let view = snapshot()
      let mutable e = view.GetEnumerator()

      while e.MoveNext() do
        scratch[e.Current.Key] <- e.Current.Value

      if Collections.rebuildMapDiff scratch state then
        state.Version <- state.Version + 1L
        Collections.pushAndBumpMap ctx state.Out state.Sinks
        state.Out.Clear()

  interface IPostSource with
    member this.ApplyPosted() = this.Invalidate()

  interface IAdaptiveMap<'K, 'V> with
    member this.GetValue() =
      ctx.ClaimOwner()

      try
        if disposed then
          invalidOp "This adaptive map has been disposed."

        this.Poll()
        AdaptiveRuntime.addDependency (this :> IAdaptiveObject) state.Version
        state.Data :> IReadOnlyDictionary<'K, 'V>
      finally
        ctx.ReleaseOwner()

    member this.Version =
      this.Poll()
      state.Version

  interface IDisposable with
    member this.Dispose() =
      if not disposed then
        disposed <- true
        Collections.clearSinks state.Sinks

  interface IMapSinkRegistry with
    member this.AddMapSink(sink) = Collections.addSink state.Sinks sink

    member this.RemoveMapSink(sink) = Collections.removeSink state.Sinks sink

/// <summary>
/// An adaptive list whose content is supplied by an external snapshot function,
/// re-read only when invalidated via the handle returned by
/// <c>AList.ofExternal</c> (FDA <c>AList.ofExternal</c> parity). The re-read
/// is diffed against the previous snapshot positionally (prefix/suffix, the
/// <c>ChangeableList.ApplyDiff</c> algorithm); the diff is delivered as a
/// <see cref="ListDelta&lt;'T&gt;"/> through the normal delta machinery.
/// </summary>
type ExternalListNode<'T when 'T: equality>(snapshot: unit -> IReadOnlyList<'T>)
  =
  let ctx = GraphContext.Current
  let mutable data = ResizeArray<'T>()
  let out = ListDelta.create()
  let mutable version = 0L
  let sinks = SinkList.create()
  let mutable dirty = true
  let mutable disposed = false

  /// <summary>
  /// The invalidate handle implementation (returned by <c>AList.ofExternal</c>
  /// as a function). Not for direct use.
  /// </summary>
  member this.Invalidate() : unit =
    dirty <- true
    ctx.BumpWriteGeneration()

  member private this.Poll() =
    if dirty && not disposed then
      dirty <- false
      let next = Collections.asResizeList(snapshot())

      if Collections.rebuildListDiff next data out then
        version <- version + 1L
        Collections.pushAndBumpList ctx out sinks
        out.Clear()

  interface IPostSource with
    member this.ApplyPosted() = this.Invalidate()

  interface IAdaptiveList<'T> with
    member this.GetValue() =
      ctx.ClaimOwner()

      try
        if disposed then
          invalidOp "This adaptive list has been disposed."

        this.Poll()
        AdaptiveRuntime.addDependency (this :> IAdaptiveObject) version
        data :> IReadOnlyList<'T>
      finally
        ctx.ReleaseOwner()

    member this.Version =
      this.Poll()
      version

  interface IDisposable with
    member this.Dispose() =
      if not disposed then
        disposed <- true
        Collections.clearSinks sinks

  interface IListSinkRegistry with
    member this.AddListSink(sink) = Collections.addSink sinks sink

    member this.RemoveListSink(sink) = Collections.removeSink sinks sink
