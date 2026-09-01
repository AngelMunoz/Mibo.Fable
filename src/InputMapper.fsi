namespace Mibo.Input

open System
open Mibo.Elmish

/// <summary>
/// Represents a physical hardware input trigger, expressed in backend-neutral
/// codes so an <see cref="T:Mibo.Input.InputMap`1"/> is portable across backends.
/// </summary>
[<Struct>]
type Trigger =
  | Key of keyCode: KeyCode
  | KeyCombo of keyCombo: Set<KeyCode>
  | MouseButton of mouseButton: MouseButtonCode
  | GamepadButton of player: int * gamepadButton: GamepadButtonCode

/// <summary>
/// Configuration mapping game actions to their trigger inputs.
/// </summary>
/// <remarks>
/// An InputMap is backend-neutral: it stores <see cref="T:Mibo.Input.Trigger"/>
/// values rather than native key/button types, so the same map works on every
/// backend. Build maps with the <see cref="M:Mibo.Input.InputMap"/> module helpers.
/// </remarks>
type InputMap<'Action when 'Action: comparison> = {
  ActionToTriggers: Map<'Action, Trigger list>
  TriggerToActions: Map<Trigger, 'Action list>
}

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

/// <summary>
/// Service interface for input mapping. The contract lives in Core; each backend
/// supplies an implementation that polls its native API to evaluate whether each
/// <see cref="T:Mibo.Input.Trigger"/> is held.
/// </summary>
type IInputMapper<'Action when 'Action: comparison> =
  abstract CurrentState: ActionState<'Action>
  abstract Update: unit -> unit

/// <summary>Functions for building InputMap configurations.</summary>
module InputMap =
  /// <summary>An empty input map with no bindings.</summary>
  val empty: InputMap<'Action> when 'Action: comparison

  /// <summary>Registers a trigger for an action.</summary>
  val bind:
    action: 'Action ->
    trigger: Trigger ->
    map: InputMap<'Action> ->
      InputMap<'Action>
      when 'Action: comparison

  /// <summary>Binds a keyboard key to an action.</summary>
  val key:
    action: 'Action -> k: KeyCode -> map: InputMap<'Action> -> InputMap<'Action>
      when 'Action: comparison

  /// <summary>Binds a keyboard key combination to an action.</summary>
  val keyCombo:
    action: 'Action ->
    keys: Set<KeyCode> ->
    map: InputMap<'Action> ->
      InputMap<'Action>
      when 'Action: comparison

  /// <summary>Binds a mouse button to an action.</summary>
  val mouse:
    action: 'Action ->
    btn: MouseButtonCode ->
    map: InputMap<'Action> ->
      InputMap<'Action>
      when 'Action: comparison

  /// <summary>Binds a gamepad button to an action.</summary>
  val gamepadButton:
    action: 'Action ->
    player: int ->
    btn: GamepadButtonCode ->
    map: InputMap<'Action> ->
      InputMap<'Action>
      when 'Action: comparison

/// <summary>Functions for updating and transforming <see cref="T:Mibo.Input.ActionState`1"/> values.</summary>
module ActionState =
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

/// <summary>Service accessors for the registered <see cref="T:Mibo.Input.IInputMapper`1"/>.</summary>
module InputMapper =
  /// <summary>Attempts to get the registered <see cref="T:Mibo.Input.IInputMapper`1"/> service.</summary>
  val inline tryGetService:
    ctx: GameContext -> IInputMapper<'Action> voption when 'Action: comparison

  /// <summary>Gets the registered <see cref="T:Mibo.Input.IInputMapper`1"/> service.</summary>
  /// <exception cref="T:System.Exception">Thrown when no IInputMapper is registered (use Program.withInputMapper).</exception>
  val inline getService:
    ctx: GameContext -> IInputMapper<'Action> when 'Action: comparison
