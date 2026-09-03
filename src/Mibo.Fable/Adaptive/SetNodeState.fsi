namespace Mibo.Fable.Adaptive

/// <summary>
/// State of a derived set node (map over set, filter, union). 'T is the input
/// element type (the journal holds input-coordinate deltas); 'U is the output
/// element type (the state and output deltas live in output coordinates).
/// </summary>
type internal SetNodeState<'T, 'U when 'U: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Set: RefCountedSet<'U>
  mutable Journal: SetDelta<'T>
  mutable Out: SetDelta<'U>
}

module internal SetNodeState =
  val inline create<'T, 'U> :
    depCount: int -> SetNodeState<'T, 'U> when 'U: equality
