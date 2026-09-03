/// <summary>Service accessors for the registered <see cref="T:Mibo.Input.InputMapper.IInputMapper`1"/>.</summary>
module Mibo.Input.InputMapper

open Mibo.Elmish

/// <summary>
/// Service interface for input mapping. The contract lives in Core; each backend
/// supplies an implementation that polls its native API to evaluate whether each
/// Trigger is held.
/// </summary>
type IInputMapper<'Action when 'Action: comparison> =
  abstract CurrentState: ActionState.ActionState<'Action>
  abstract Update: unit -> unit

/// <summary>Attempts to get the registered IInputMapper service.</summary>
val inline tryGetService:
  ctx: GameContext -> IInputMapper<'Action> voption when 'Action: comparison

/// <summary>Gets the registered IInputMapper service.</summary>
/// <exception cref="T:System.Exception">Thrown when no IInputMapper is registered (use Program.withInputMapper).</exception>
val inline getService:
  ctx: GameContext -> IInputMapper<'Action> when 'Action: comparison
