/// <summary>Functions for updating and transforming <see cref="T:Mibo.Input.ActionState`1"/> values.</summary>
module Mibo.Input.ActionState

open Mibo.Input.InputMap

/// <summary>
/// Runtime state tracking which actions are currently active.
/// </summary>
/// <remarks>
/// ActionState is the "output" of the input mapping system. It tells you
/// which actions are held, just started, or just released.
/// </remarks>
/// <example>
/// <code>
/// if actionState.Started.Contains Jump then
///     // Player just pressed jump this frame
///
/// if actionState.Held.Contains MoveLeft then
///     // Player is holding left
/// </code>
/// </example>
type ActionState<'Action when 'Action: comparison> = {
  Held: Set<'Action>
  Started: Set<'Action>
  Released: Set<'Action>
  Values: Map<'Action, float32>
  HeldTriggers: Set<Trigger>
}

/// <summary>An empty action state with nothing held, started, or released.</summary>
val empty: ActionState<'Action> when 'Action: comparison

/// <summary>
/// Pure state-update for a single trigger transition. Backend-agnostic: the
/// caller supplies <c>isDown</c>, so this function never touches a native API.
/// </summary>
val update:
  map: InputMap<'Action> ->
  isDown: bool ->
  trigger: Trigger ->
  state: ActionState<'Action> ->
    ActionState<'Action>
    when 'Action: comparison

/// <summary>Clears the per-frame edge sets (<c>Started</c>/<c>Released</c>) for the next frame.</summary>
val nextFrame:
  state: ActionState<'Action> -> ActionState<'Action> when 'Action: comparison

/// <summary>
/// Merges a freshly built state into the current one: the edge events
/// (<c>Started</c>/<c>Released</c>) UNION — several deltas arriving between two
/// consumptions must not drop the earlier deltas' edges (a mouse-move build has
/// empty edges and would otherwise wipe a key delta's events) — while
/// <c>Held</c>/<c>Values</c>/<c>HeldTriggers</c> stay last-wins: they are the current
/// truth, not events. Pairs with <see cref="M:Mibo.Input.ActionState.nextFrame"/>:
/// the adaptive input subscription clears the edges at the post drain after
/// <c>Update</c>, so the next merge starts from an empty edge set.
/// </summary>
/// <param name="current">The root's current state — its edges may already hold unread events.</param>
/// <param name="incoming">The freshly built state — its edges are the new delta's events.</param>
val mergeEdges:
  current: ActionState<'Action> ->
  incoming: ActionState<'Action> ->
    ActionState<'Action>
    when 'Action: comparison
