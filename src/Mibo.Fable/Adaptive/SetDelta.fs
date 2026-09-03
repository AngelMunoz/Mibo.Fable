namespace Mibo.Fable.Adaptive

/// <summary>
/// A set delta: elements added and removed since the previous delivery. The
/// buffers are transient: valid only during the delivery that received the
/// delta.
/// </summary>
type SetDelta<'T> = internal {
  mutable Adds: DeltaBuffer<'T>
  mutable Rems: DeltaBuffer<'T>
  // Shared in-drain flag (a one-slot array: the drains capture the
  // buffer references and counts, so a plain field would not be
  // visible to reentrant appends). Nonzero while a drain is
  // replaying this journal; the append-time cross-kind coalescing
  // (journalAppendSet) is suspended then, so it can never remove an
  // entry the in-flight drain is about to process.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member this.IsEmpty = this.Adds.IsEmpty && this.Rems.IsEmpty

  member internal this.Clear() =
    this.Adds.Clear()
    this.Rems.Clear()

  /// <summary>The elements added. Transient: valid during the callback only.</summary>
  member this.Added: 'T[] = this.Adds.Items

  /// <summary>The number of added elements.</summary>
  member this.AddedCount: int = this.Adds.Count

  /// <summary>The elements removed. Transient: valid during the callback only.</summary>
  member this.Removed: 'T[] = this.Rems.Items

  /// <summary>The number of removed elements.</summary>
  member this.RemovedCount: int = this.Rems.Count

  /// <summary>Appends an add operation. For <see cref="ASet.custom"/> computes.</summary>
  member this.Add(item: 'T) = this.Adds.Append item

  /// <summary>Appends a remove operation. For <see cref="ASet.custom"/> computes.</summary>
  member this.Remove(item: 'T) = this.Rems.Append item

module internal SetDelta =
  let inline create<'T>() : SetDelta<'T> = {
    Adds = DeltaBuffer.create()
    Rems = DeltaBuffer.create()
    InDrain = [| 0 |]
  }

  /// Point-in-time copy: shares the buffers, copies the counts.
  let inline copy<'T>(source: SetDelta<'T>) : SetDelta<'T> = {
    Adds = DeltaBuffer.copy source.Adds
    Rems = DeltaBuffer.copy source.Rems
    InDrain = [| 0 |]
  }

/// <summary>
/// A mutable delta builder for <see cref="ASet.custom"/> computes. The compute
/// receives the current view and this builder, appends the operations that
/// describe the change since the previous call, and returns. The builder is a
/// class: appends mutate the node's pending delta directly.
/// </summary>
type SetDeltaBuilder<'T>() =
  let adds = DeltaBuffer.create()
  let rems = DeltaBuffer.create()

  /// <summary>Appends an add operation.</summary>
  member _.Add(item: 'T) = adds.Append item

  /// <summary>Appends a remove operation.</summary>
  member _.Remove(item: 'T) = rems.Append item

  member internal _.IsEmpty = adds.IsEmpty && rems.IsEmpty

  member internal _.Clear() =
    adds.Clear()
    rems.Clear()

  member internal _.Adds = adds
  member internal _.Rems = rems

  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal this.Snapshot() : SetDelta<'T> =
    SetDelta.copy {
      Adds = adds
      Rems = rems
      InDrain = [| 0 |]
    }
