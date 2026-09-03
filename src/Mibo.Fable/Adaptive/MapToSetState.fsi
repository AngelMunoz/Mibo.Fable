namespace Mibo.Fable.Adaptive

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
  val inline create<'K, 'V, 'T> :
    depCount: int -> MapToSetState<'K, 'V, 'T>
      when 'K: equality and 'T: equality
