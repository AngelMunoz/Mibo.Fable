namespace Mibo.Fable.Adaptive

open System
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
  let inline create<'K, 'V, 'T when 'K: equality>
    (depCount: int)
    : SetToMapState<'K, 'V, 'T> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Data = Dictionary<'K, 'V>()
      Journal = SetDelta.create()
      Out = MapDelta.create()
    }
