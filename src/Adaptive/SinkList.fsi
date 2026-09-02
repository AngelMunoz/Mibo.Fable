namespace Mibo.Fable.Adaptive

/// <summary>
/// The sink list of a source. Entries are weak (a JS WeakRef): a derived node
/// the user dropped (and that is not observed) is collected, and delivery
/// skips its dead entry. A live sink is strongly reachable through its owner
/// (the user, an observation, or a downstream node), so delivery always
/// resolves it.
/// </summary>
type internal SinkList = internal {
  mutable Sinks: obj[]
  mutable Count: int
} with

  member IsEmpty: bool

module internal SinkList =
  val inline create: unit -> SinkList
