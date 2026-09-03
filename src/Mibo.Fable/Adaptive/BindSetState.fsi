namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>
/// Internal. State of a bind node over a scalar value (PLAN.md Section 7.4):
/// one inner set, swapped when the value changes. The content set is the
/// output (a single contribution needs no refcounts).
/// </summary>
type internal BindSetState<'U when 'U: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Journal: SetDelta<'U>
  mutable Data: HashSet<'U>
  mutable OutDelta: SetDelta<'U>
}

module internal BindSetState =
  val inline create<'U> : depCount: int -> BindSetState<'U> when 'U: equality
