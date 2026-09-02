namespace Mibo.Fable.Adaptive

/// <summary>
/// A map delta: upserted entries and removed keys since the previous
/// delivery. The buffers are transient: valid only during the delivery that
/// received the delta.
/// </summary>
type MapDelta<'K, 'V> = internal {
  mutable Sets: DeltaBuffer<struct ('K * 'V)>
  mutable Rems: DeltaBuffer<'K>
  // Shared in-drain flag; see SetDelta.InDrain.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member this.IsEmpty = this.Sets.IsEmpty && this.Rems.IsEmpty

  member internal this.Clear() =
    this.Sets.Clear()
    this.Rems.Clear()

  /// <summary>The entries set (added or updated). Transient: valid during the callback only.</summary>
  member this.SetEntries: struct ('K * 'V)[] = this.Sets.Items

  /// <summary>The number of entries set.</summary>
  member this.SetCount: int = this.Sets.Count

  /// <summary>The keys removed. Transient: valid during the callback only.</summary>
  member this.RemovedKeys: 'K[] = this.Rems.Items

  /// <summary>The number of keys removed.</summary>
  member this.RemovedCount: int = this.Rems.Count

  /// <summary>Appends an upsert operation. For <see cref="AMap.custom"/> computes.</summary>
  member this.Set(key: 'K, value: 'V) = this.Sets.Append(struct (key, value))

  /// <summary>Appends a remove operation. For <see cref="AMap.custom"/> computes.</summary>
  member this.Remove(key: 'K) = this.Rems.Append key

module internal MapDelta =
  let inline create<'K, 'V>() : MapDelta<'K, 'V> = {
    Sets = DeltaBuffer.create()
    Rems = DeltaBuffer.create()
    InDrain = [| 0 |]
  }

  /// Point-in-time copy: shares the buffers, copies the counts.
  let inline copy<'K, 'V>(source: MapDelta<'K, 'V>) : MapDelta<'K, 'V> = {
    Sets = DeltaBuffer.copy source.Sets
    Rems = DeltaBuffer.copy source.Rems
    InDrain = [| 0 |]
  }

/// <summary>
/// A mutable delta builder for <see cref="AMap.custom"/> computes. See
/// <see cref="SetDeltaBuilder&lt;'T&gt;"/> for the protocol.
/// </summary>
type MapDeltaBuilder<'K, 'V>() =
  let sets = DeltaBuffer.create()
  let rems = DeltaBuffer.create()

  /// <summary>Appends an upsert operation.</summary>
  member _.Set(key: 'K, value: 'V) = sets.Append(struct (key, value))

  /// <summary>Appends a remove operation.</summary>
  member _.Remove(key: 'K) = rems.Append key

  member internal _.IsEmpty = sets.IsEmpty && rems.IsEmpty

  member internal _.Clear() =
    sets.Clear()
    rems.Clear()

  member internal _.Sets = sets
  member internal _.Rems = rems

  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal this.Snapshot() : MapDelta<'K, 'V> =
    MapDelta.copy {
      Sets = sets
      Rems = rems
      InDrain = [| 0 |]
    }
