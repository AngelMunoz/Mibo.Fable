namespace Mibo.Fable.Adaptive

/// <summary>
/// Per-element cache entry of the <c>*A</c> nodes (mapA/chooseA/filterA).
/// Holds the element's aval, its version at the last force (the version read
/// BEFORE the force: a mid-force write then leaves the stored version stale,
/// so the next scan re-forces), and its last contribution to the output.
/// </summary>
type internal ElementEntry<'U> = internal {
  mutable Aval: aval<'U voption>
  mutable Version: int64
  mutable Last: 'U voption
}

module internal ElementEntry =
  val inline create<'U> :
    aval: aval<'U voption> ->
    version: int64 ->
    last: 'U voption ->
      ElementEntry<'U>
