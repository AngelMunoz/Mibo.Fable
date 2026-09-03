module Mibo.Input.ActionState

open Mibo.Input.InputMap

type ActionState<'Action when 'Action: comparison> = {
  Held: Set<'Action>
  Started: Set<'Action>
  Released: Set<'Action>
  Values: Map<'Action, float32>
  HeldTriggers: Set<Trigger>
}

let empty = {
  Held = Set.empty
  Started = Set.empty
  Released = Set.empty
  Values = Map.empty
  HeldTriggers = Set.empty
}

/// <summary>
/// Pure state-update for a single trigger transition. Backend-agnostic: the
/// caller supplies <c>isDown</c>, so this function never touches a native API.
/// </summary>
let update
  (map: InputMap<'Action>)
  (isDown: bool)
  (trigger: Trigger)
  (state: ActionState<'Action>)
  : ActionState<'Action> =
  let newHeldTriggers =
    if isDown then
      state.HeldTriggers |> Set.add trigger
    else
      state.HeldTriggers |> Set.remove trigger

  let actions =
    map.TriggerToActions |> Map.tryFind trigger |> Option.defaultValue []

  let mutable newHeld = state.Held
  let mutable newStarted = state.Started
  let mutable newReleased = state.Released
  let mutable newValues = state.Values

  for action in actions do
    let allTriggers =
      map.ActionToTriggers |> Map.tryFind action |> Option.defaultValue []

    let isActionHeld = allTriggers |> List.exists newHeldTriggers.Contains

    let wasHeld = state.Held.Contains action

    if isActionHeld && not wasHeld then
      newHeld <- newHeld |> Set.add action
      newStarted <- newStarted |> Set.add action
      newValues <- newValues |> Map.add action 1.0f
    elif not isActionHeld && wasHeld then
      newHeld <- newHeld |> Set.remove action
      newReleased <- newReleased |> Set.add action
      newValues <- newValues |> Map.remove action

  {
    Held = newHeld
    Started = newStarted
    Released = newReleased
    Values = newValues
    HeldTriggers = newHeldTriggers
  }

let nextFrame(state: ActionState<'Action>) = {
  state with
      Started = Set.empty
      Released = Set.empty
}

/// <summary>
/// Merges a freshly built state into the current one: the edge events
/// (<c>Started</c>/<c>Released</c>) UNION — several deltas arriving between two
/// consumptions must not drop the earlier deltas' edges (a mouse-move build has
/// empty edges and would otherwise wipe a key delta's events) — while
/// <c>Held</c>/<c>Values</c>/<c>HeldTriggers</c> stay last-wins: they are the current
/// truth, not events. Pairs with nextFrame: the adaptive input subscription
/// clears the edges at the post drain after Update, so the next merge starts
/// from an empty edge set.
/// </summary>
/// <param name="current">The root's current state — its edges may already hold unread events.</param>
/// <param name="incoming">The freshly built state — its edges are the new delta's events.</param>
let mergeEdges
  (current: ActionState<'Action>)
  (incoming: ActionState<'Action>)
  : ActionState<'Action> =
  {
    incoming with
        Started = Set.union current.Started incoming.Started
        Released = Set.union current.Released incoming.Released
  }
