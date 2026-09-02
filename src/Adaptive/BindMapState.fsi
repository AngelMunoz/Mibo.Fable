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
  val inline create<'K, 'V> :
    depCount: int -> BindMapState<'K, 'V> when 'K: equality
