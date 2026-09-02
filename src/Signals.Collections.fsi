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

/// A mutable builder the <c>custom</c> computes of a set use to report the
/// change since their previous run: the compute appends the operations (for
/// example, by consuming its own event queue) and the node applies them.
type SetDeltaBuilder<'T> =

  new: unit -> SetDeltaBuilder<'T>
  /// Appends an add operation.
  member Add: item: 'T -> unit
  /// Appends a remove operation.
  member Remove: item: 'T -> unit
  member internal Adds: 'T array
  member internal Removes: 'T array

/// A mutable builder the <c>custom</c> computes of a map use to report the
/// change since their previous run. See <see cref="SetDeltaBuilder"/>.
type MapDeltaBuilder<'K, 'V> =

  new: unit -> MapDeltaBuilder<'K, 'V>
  /// Appends an upsert operation.
  member Set: key: 'K * value: 'V -> unit
  /// Appends a remove operation.
  member Remove: key: 'K -> unit
  member internal Sets: struct ('K * 'V) array
  member internal Removes: 'K array

/// A mutable builder the <c>custom</c> computes of a list use to report the
/// change since their previous run. Positions refer to the state as of the
/// previous operation. See <see cref="SetDeltaBuilder"/>.
type ListDeltaBuilder<'T> =

  new: unit -> ListDeltaBuilder<'T>
  /// Appends an insert operation.
  member Insert: position: int * value: 'T -> unit
  /// Appends a remove operation.
  member Remove: position: int -> unit
  /// Appends an update operation.
  member Update: position: int * value: 'T -> unit
  member internal Operations: ListOp<'T> array

/// One adaptive map: a memoized view of the structural version and the
/// per-key cells. Value cells hold <c>voption</c> so derived nodes can
/// express absence (filter/choose drops) with the same cell type.
type AMap<'K, 'V when 'K: comparison> =

  internal new:
    state: AVal<struct (int64 * Map<'K, AVal<'V voption>>)> -> AMap<'K, 'V>

  /// The structural version and the per-key cells.
  member internal State: AVal<struct (int64 * Map<'K, AVal<'V voption>>)>

  /// A hook run before any direct read; changeable owners flush their
  /// posted boundary intents here.
  member internal OnRead: (unit -> unit) with get, set

/// One adaptive set: structure only, no per-element value cells.
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
type ListCells<'T> =

  internal new:
    order: int array * byId: Map<int, AVal<'T voption>> -> ListCells<'T>

  /// The element order as internal ids.
  member Order: int array
  /// One value cell per id.
  member ById: Map<int, AVal<'T voption>>

/// One adaptive list: a memoized view of the structural version and the
/// stable-id cells.
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
