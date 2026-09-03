namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>Internal. State of a choose2 map node.</summary>
type internal Choose2State<'K, 'V1, 'V2, 'V3 when 'K: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Sides: Dictionary<'K, struct ('V1 voption * 'V2 voption)>
  mutable Out: Dictionary<'K, 'V3>
  mutable JournalL: MapDelta<'K, 'V1>
  mutable JournalR: MapDelta<'K, 'V2>
  mutable OutDelta: MapDelta<'K, 'V3>
  /// Reused scratch for the net-delta post-pass (construction-time
  /// allocation only; zero steady-state allocation).
  mutable Scratch: HashSet<'K>
  mutable Scratch2: HashSet<'K>
}

module internal Choose2State =
  val inline create<'K, 'V1, 'V2, 'V3> :
    depCount: int -> Choose2State<'K, 'V1, 'V2, 'V3> when 'K: equality
