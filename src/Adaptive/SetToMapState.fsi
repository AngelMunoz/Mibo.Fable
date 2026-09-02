namespace Mibo.Fable.Adaptive

open System.Collections.Generic

/// <summary>Internal. State of a set-to-map node (one value per key).</summary>
type internal SetToMapState<'K, 'V, 'T when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Data: Dictionary<'K, 'V>
  mutable Journal: SetDelta<'T>
  mutable Out: MapDelta<'K, 'V>
}

module internal SetToMapState =
  val inline create<'K, 'V, 'T> :
    depCount: int -> SetToMapState<'K, 'V, 'T> when 'K: equality
