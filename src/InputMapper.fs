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

/// <summary>Attempts to get the registered <see cref="T:Mibo.Input.InputMapper.IInputMapper`1"/> service.</summary>
let inline tryGetService<'Action when 'Action: comparison>
  (ctx: GameContext)
  : IInputMapper<'Action> voption =
  GameContext.tryGetService<IInputMapper<'Action>> ctx

/// <summary>Gets the registered <see cref="T:Mibo.Input.InputMapper.IInputMapper`1"/> service.</summary>
/// <exception cref="T:System.Exception">Thrown when no IInputMapper is registered (use Program.withInputMapper).</exception>
let inline getService<'Action when 'Action: comparison>
  (ctx: GameContext)
  : IInputMapper<'Action> =
  match tryGetService<'Action> ctx with
  | ValueSome m -> m
  | ValueNone ->
    failwith
      "IInputMapper service not registered. Add RaylibProgram.withInputMapper or MonoGameProgram.withInputMapper to your program."
