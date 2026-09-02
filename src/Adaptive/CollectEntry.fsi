namespace Mibo.Fable.Adaptive

open System.Collections.Generic

/// <summary>
/// Internal. One source element's contribution to a collect node: the inner
/// adaptive set, its last-seen version, the current content, the pending
/// journal, and the registered sink.
/// </summary>
type internal CollectEntry<'U when 'U: equality> = internal {
  mutable Node: IAdaptiveSet<'U>
  mutable Version: int64
  mutable Content: HashSet<'U>
  mutable Journal: SetDelta<'U>
  mutable Sink: obj
}

module internal CollectEntry =
  val inline create<'U> :
    node: IAdaptiveSet<'U> -> CollectEntry<'U> when 'U: equality
