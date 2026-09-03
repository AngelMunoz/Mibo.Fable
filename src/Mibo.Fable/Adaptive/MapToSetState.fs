namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>Internal. State of a map-to-set node (keys or distinct values).</summary>
type internal MapToSetState<'K, 'V, 'T when 'K: equality and 'T: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Mirror: Dictionary<'K, 'T>
  mutable Out: RefCountedSet<'T>
  mutable Journal: MapDelta<'K, 'V>
  mutable OutDelta: SetDelta<'T>
}

module internal MapToSetState =
  let inline create<'K, 'V, 'T when 'K: equality and 'T: equality>
    (depCount: int)
    : MapToSetState<'K, 'V, 'T> =
    {
      Version = 0L
      Sinks = SinkList.create()
      DepVersions = Array.zeroCreate depCount
      Mirror = Dictionary<'K, 'T>()
      Out = RefCountedSet.create()
      Journal = MapDelta.create()
      OutDelta = SetDelta.create()
    }
