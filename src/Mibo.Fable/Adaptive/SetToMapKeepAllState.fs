namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>Internal. State of a keep-all set-to-map node (per-key value sets).</summary>
type internal SetToMapKeepAllState<'K, 'V, 'T when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Data: Dictionary<'K, HashSet<'V>>
  mutable Journal: SetDelta<'T>
  mutable Out: MapDelta<'K, HashSet<'V>>
}

module internal SetToMapKeepAllState =
  let inline create<'K, 'V, 'T when 'K: equality>
    (depCount: int)
    : SetToMapKeepAllState<'K, 'V, 'T> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Data = Dictionary<'K, HashSet<'V>>()
      Journal = SetDelta.create()
      Out = MapDelta.create()
    }
