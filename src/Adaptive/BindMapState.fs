namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>
/// Internal. State of a bind map node over a scalar value (PLAN.md Section
/// 7.4): one inner map, swapped when the value changes.
/// </summary>
type internal BindMapState<'K, 'V when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Journal: MapDelta<'K, 'V>
  mutable Data: Dictionary<'K, 'V>
  mutable OutDelta: MapDelta<'K, 'V>
}

module internal BindMapState =
  let inline create<'K, 'V when 'K: equality>
    (depCount: int)
    : BindMapState<'K, 'V> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Journal = MapDelta.create()
      Data = Dictionary<'K, 'V>()
      OutDelta = MapDelta.create()
    }
