namespace Mibo.Fable.Adaptive

/// <summary>
/// A set delta: elements added and removed since the previous delivery. The
/// buffers are transient: valid only during the delivery that received the
/// delta.
/// </summary>
type SetDelta<'T> = internal {
  mutable Adds: DeltaBuffer<'T>
  mutable Rems: DeltaBuffer<'T>
  /// Shared in-drain flag (a one-slot array). Nonzero while a drain
  /// is replaying this journal; the append-time cross-kind
  /// coalescing is suspended then.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member IsEmpty: bool
  member internal Clear: unit -> unit
  /// <summary>The elements added. Transient: valid during the callback only.</summary>
  member Added: 'T[]
  /// <summary>The number of added elements.</summary>
  member AddedCount: int
  /// <summary>The elements removed. Transient: valid during the callback only.</summary>
  member Removed: 'T[]
  /// <summary>The number of removed elements.</summary>
  member RemovedCount: int
  /// <summary>Appends an add operation. For <see cref="ASet.custom"/> computes.</summary>
  member Add: item: 'T -> unit
  /// <summary>Appends a remove operation. For <see cref="ASet.custom"/> computes.</summary>
  member Remove: item: 'T -> unit

module internal SetDelta =
  val inline create<'T> : unit -> SetDelta<'T>
  /// Point-in-time copy: shares the buffers, copies the counts.
  val inline copy<'T> : source: SetDelta<'T> -> SetDelta<'T>

/// <summary>
/// A mutable delta builder for <see cref="ASet.custom"/> computes. The compute
/// receives the current view and this builder, appends the operations that
/// describe the change since the previous call, and returns. The builder is a
/// class: appends mutate the node's pending delta directly.
/// </summary>
type SetDeltaBuilder<'T> =
  new: unit -> SetDeltaBuilder<'T>
  /// <summary>Appends an add operation.</summary>
  member Add: item: 'T -> unit
  /// <summary>Appends a remove operation.</summary>
  member Remove: item: 'T -> unit
  member internal IsEmpty: bool
  member internal Clear: unit -> unit
  member internal Adds: DeltaBuffer<'T>
  member internal Rems: DeltaBuffer<'T>
  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal Snapshot: unit -> SetDelta<'T>
