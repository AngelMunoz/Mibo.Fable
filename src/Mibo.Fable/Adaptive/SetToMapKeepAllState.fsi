namespace Mibo.Fable.Adaptive

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
  val inline create<'K, 'V, 'T> :
    depCount: int -> SetToMapKeepAllState<'K, 'V, 'T> when 'K: equality
