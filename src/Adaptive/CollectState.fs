namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>Internal. State of a collect node (PLAN.md Section 7.4).</summary>
type internal CollectState<'T, 'U when 'T: equality and 'U: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Journal: SetDelta<'T>
  mutable Inner: Dictionary<'T, CollectEntry<'U>>
  mutable Global: RefCountedSet<'U>
  mutable OutDelta: SetDelta<'U>
  // Reused scratch for the net-delta pass: prior presence of every
  // output element touched this batch (construction-time allocation
  // only; zero steady-state allocation).
  mutable Scratch: Dictionary<'U, bool>
}

module internal CollectState =
  let inline create<'T, 'U when 'T: equality and 'U: equality>
    (depCount: int)
    : CollectState<'T, 'U> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Journal = SetDelta.create()
      Inner = Dictionary<'T, CollectEntry<'U>>()
      Global = RefCountedSet.create()
      OutDelta = SetDelta.create()
      Scratch = Dictionary<'U, bool>()
    }
