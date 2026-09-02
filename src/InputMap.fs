module Mibo.Input.InputMap

open System
open Mibo.Elmish

// ─────────────────────────────────────────────────────────────────────────────
// Trigger: a physical hardware input that an action can be bound to.
//
// Uses the Core-neutral KeyCode / MouseButtonCode / GamepadButtonCode, so an
// InputMap can be authored and persisted without any backend reference. The
// backend's IInputMapper implementation translates "is this trigger held?" via
// its native API.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a physical hardware input trigger, expressed in backend-neutral
/// codes so an InputMap is portable across backends.
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
/// An InputMap is backend-neutral: it stores Trigger values rather than native
/// key/button types, so the same map works on every backend. Build maps with
/// this module's helpers.
/// </remarks>
type InputMap<'Action when 'Action: comparison> = {
  ActionToTriggers: Map<'Action, Trigger list>
  TriggerToActions: Map<Trigger, 'Action list>
}

let empty = {
  ActionToTriggers = Map.empty
  TriggerToActions = Map.empty
}

let bind (action: 'Action) (trigger: Trigger) (map: InputMap<'Action>) =
  let existingTriggers =
    map.ActionToTriggers |> Map.tryFind action |> Option.defaultValue []

  let existingActions =
    map.TriggerToActions |> Map.tryFind trigger |> Option.defaultValue []

  {
    ActionToTriggers =
      map.ActionToTriggers |> Map.add action (trigger :: existingTriggers)
    TriggerToActions =
      map.TriggerToActions |> Map.add trigger (action :: existingActions)
  }

let key (action: 'Action) (k: KeyCode) (map: InputMap<'Action>) =
  bind action (Key k) map

let keyCombo (action: 'Action) (keys: Set<KeyCode>) (map: InputMap<'Action>) =
  bind action (KeyCombo keys) map

let mouse (action: 'Action) (btn: MouseButtonCode) (map: InputMap<'Action>) =
  bind action (MouseButton btn) map

let gamepadButton
  (action: 'Action)
  (player: int)
  (btn: GamepadButtonCode)
  (map: InputMap<'Action>)
  =
  bind action (GamepadButton(player, btn)) map
