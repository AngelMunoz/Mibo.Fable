namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>Internal. State of a two-source set node.</summary>
type internal TwoSetState<'T when 'T: equality> = internal {
  mutable Version: int64
  mutable Sinks: SinkList
  mutable DepVersions: int64[]
  mutable Left: RefCountedSet<'T>
  mutable Right: RefCountedSet<'T>
  mutable Out: HashSet<'T>
  mutable JournalL: SetDelta<'T>
  mutable JournalR: SetDelta<'T>
  mutable OutDelta: SetDelta<'T>
  // Reused scratch for the net-delta post-pass (construction-time
  // allocation only; zero steady-state allocation).
  mutable Scratch: HashSet<'T>
}

module internal TwoSetState =
  let inline create<'T when 'T: equality>(depCount: int) : TwoSetState<'T> = {
    Version = 0L
    Sinks = SinkList.create()
    DepVersions = Array.zeroCreate depCount
    Left = RefCountedSet.create()
    Right = RefCountedSet.create()
    Out = HashSet<'T>()
    JournalL = SetDelta.create()
    JournalR = SetDelta.create()
    OutDelta = SetDelta.create()
    Scratch = HashSet<'T>()
  }
