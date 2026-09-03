namespace Mibo.Fable.Adaptive

/// <summary>
/// A map delta: upserted entries and removed keys since the previous
/// delivery. The buffers are transient: valid only during the delivery that
/// received the delta.
/// </summary>
type MapDelta<'K, 'V> = internal {
  mutable Sets: DeltaBuffer<struct ('K * 'V)>
  mutable Rems: DeltaBuffer<'K>
  /// Shared in-drain flag; see SetDelta.InDrain.
  mutable InDrain: int[]
} with

  /// <summary>Gets whether this delta contains no operations.</summary>
  member IsEmpty: bool
  member internal Clear: unit -> unit
  /// <summary>The entries set (added or updated). Transient: valid during the callback only.</summary>
  member SetEntries: struct ('K * 'V)[]
  /// <summary>The number of entries set.</summary>
  member SetCount: int
  /// <summary>The keys removed. Transient: valid during the callback only.</summary>
  member RemovedKeys: 'K[]
  /// <summary>The number of keys removed.</summary>
  member RemovedCount: int
  /// <summary>Appends an upsert operation. For <see cref="AMap.custom"/> computes.</summary>
  member Set: key: 'K * value: 'V -> unit
  /// <summary>Appends a remove operation. For <see cref="AMap.custom"/> computes.</summary>
  member Remove: key: 'K -> unit

module internal MapDelta =
  val inline create<'K, 'V> : unit -> MapDelta<'K, 'V>
  /// Point-in-time copy: shares the buffers, copies the counts.
  val inline copy<'K, 'V> : source: MapDelta<'K, 'V> -> MapDelta<'K, 'V>

/// <summary>
/// A mutable delta builder for <see cref="AMap.custom"/> computes. See
/// <see cref="SetDeltaBuilder&lt;'T&gt;"/> for the protocol.
/// </summary>
type MapDeltaBuilder<'K, 'V> =
  new: unit -> MapDeltaBuilder<'K, 'V>
  /// <summary>Appends an upsert operation.</summary>
  member Set: key: 'K * value: 'V -> unit
  /// <summary>Appends a remove operation.</summary>
  member Remove: key: 'K -> unit
  member internal IsEmpty: bool
  member internal Clear: unit -> unit
  member internal Sets: DeltaBuffer<struct ('K * 'V)>
  member internal Rems: DeltaBuffer<'K>
  /// Point-in-time copy: shares the buffers, copies the counts.
  member internal Snapshot: unit -> MapDelta<'K, 'V>
