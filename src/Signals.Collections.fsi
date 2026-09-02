namespace Mibo.Signals

// ─────────────────────────────────────────────────────────────────────────────
// Signals.Collections — shared machinery for the adaptive collections
// (AMap, ASet, AList) built on @preact/signals-core.
//
// Representation: per-key/per-element signal cells over an F# Map snapshot,
// one structural version channel per collection. Value writes touch one cell;
// structural writes replace the cell snapshot and bump the version. Batching
// is one module-level depth counter; delta sinks flush through preact
// effects, so a write burst inside a batch delivers exactly one net delta.
// ─────────────────────────────────────────────────────────────────────────────

/// The net change of a map since the last flush: the entries written
/// (adds and value updates) and the keys removed.
type MapDelta<'K, 'V> = {
  Sets: ('K * 'V) array
  Removes: 'K array
}

/// The net change of a set since the last flush.
type SetDelta<'T> = { Adds: 'T array; Removes: 'T array }

/// The kind of a list delta operation.
type ListOpKind =
  /// An element was inserted before the given position.
  | ListInsert
  /// The element at the given position was removed.
  | ListRemove
  /// The element at the given position was replaced.
  | ListUpdate

/// One positional list operation.
type ListOp<'T> = {
  Kind: ListOpKind
  Position: int
  Value: 'T
}

/// The net change of a list since the last flush: positional operations,
/// applied in order.
type ListDelta<'T> = { Operations: ListOp<'T> array }

/// A disposable handle.
type Unsubscriber =
  new: wrapper: (unit -> unit) -> Unsubscriber
  interface System.IDisposable

/// One adaptive map: a memoized view of the structural version and the
/// per-key cells. Value cells hold <c>voption</c> so derived nodes can
/// express absence (filter/choose drops) with the same cell type.
[<Class>]
type AMap<'K, 'V when 'K: comparison> =

  internal new:
    state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)> -> AMap<'K, 'V>

  /// The structural version and the per-key cells.
  member internal State: AVal<struct (int64 * Map<'K, AVal<'V voption>>)>

  /// A hook run before any direct read; changeable owners flush their
  /// posted boundary intents here.
  member internal OnRead: (unit -> unit) with get, set

/// One adaptive set: structure only, no per-element value cells.
[<Class>]
type ASet<'T when 'T: comparison> =

  internal new: state: AVal<struct (int64 * Set<'T>)> -> ASet<'T>

  /// The structural version and the current element set.
  member internal State: AVal<struct (int64 * Set<'T>)>

  /// A hook run before any direct read; changeable owners flush their
  /// posted boundary intents here.
  member internal OnRead: (unit -> unit) with get, set

/// The stable-id cells of an adaptive list: the element order as internal
/// ids plus one value cell per id. Moves and inserts reorder
/// <see cref="Order"/> without touching the cells, so element values never
/// recompute on position changes.
[<Class>]
type ListCells<'T> =

  internal new:
    order: int array * byId: Map<int, AVal<'T voption>> -> ListCells<'T>

  /// The element order as internal ids.
  member Order: int array
  /// One value cell per id.
  member ById: Map<int, AVal<'T voption>>

/// One adaptive list: a memoized view of the structural version and the
/// stable-id cells.
[<Class>]
type AList<'T> =

  internal new: state: AVal<struct (int64 * ListCells<'T>)> -> AList<'T>

  /// The structural version and the stable-id cells.
  member internal State: AVal<struct (int64 * ListCells<'T>)>

  /// A hook run before any direct read; changeable owners flush their
  /// posted boundary intents here.
  member internal OnRead: (unit -> unit) with get, set

/// <summary>Shared batching, pack-guard, and delta-sink machinery.</summary>
[<RequireQualifiedAccess>]
module Collections =

  /// <summary>True while inside a <see cref="batch"/>.</summary>
  val inBatch: unit -> bool

  /// <summary>
  /// Runs <paramref name="work"/> with delta delivery postponed until it
  /// returns. Nested batches collapse into the outermost one.
  /// </summary>
  val batch: work: (unit -> unit) -> unit

  /// <summary>
  /// Raises when called inside a batch: materializing a collection into a
  /// frame pack is a pack-once-per-step contract.
  /// </summary>
  val guardPack: operation: string -> unit
