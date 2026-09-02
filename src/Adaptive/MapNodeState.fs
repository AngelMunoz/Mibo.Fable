namespace Mibo.Fable.Adaptive

open System
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
  let inline create<'K, 'V, 'U when 'K: equality>
    (depCount: int)
    : MapNodeState<'K, 'V, 'U> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Data = Dictionary<'K, 'U>()
      Journal = MapDelta.create()
      Out = MapDelta.create()
    }
