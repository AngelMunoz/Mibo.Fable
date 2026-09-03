/// <summary>Functions for building InputMap configurations.</summary>
module Mibo.Input.InputMap

open System

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
/// An InputMap is backend-neutral: it stores <see cref="T:Mibo.Input.InputMap.Trigger"/>
/// values rather than native key/button types, so the same map works on every
/// backend. Build maps with this module's helpers.
/// </remarks>
type InputMap<'Action when 'Action: comparison> = {
  ActionToTriggers: Map<'Action, Trigger list>
  TriggerToActions: Map<Trigger, 'Action list>
}

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
