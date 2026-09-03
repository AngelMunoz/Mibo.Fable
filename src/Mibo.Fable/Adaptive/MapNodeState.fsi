namespace Mibo.Fable.Adaptive

open System.Collections.Generic

/// <summary>
/// State of a derived map node (map over map, filter). The journal holds
/// input-coordinate deltas (source entries); the state and output deltas live
/// in output coordinates.
/// </summary>
type internal MapNodeState<'K, 'V, 'U when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Data: Dictionary<'K, 'U>
  mutable Journal: MapDelta<'K, 'V>
  mutable Out: MapDelta<'K, 'U>
}

module internal MapNodeState =
  val inline create<'K, 'V, 'U> :
    depCount: int -> MapNodeState<'K, 'V, 'U> when 'K: equality
