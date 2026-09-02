module Mibo.Input.Input

open Mibo.Elmish

/// <summary>Attempts to get the registered <see cref="T:Mibo.Input.IInput"/> service.</summary>
let tryGetService(ctx: GameContext) : IInput voption =
  GameContext.tryGetService<IInput> ctx

/// <summary>Gets the registered <see cref="T:Mibo.Input.IInput"/> service.</summary>
/// <exception cref="T:System.Exception">Thrown when no IInput is registered (use <see cref="M:Mibo.Elmish.Program.withInput"/>).</exception>
let getService(ctx: GameContext) : IInput =
  match tryGetService ctx with
  | ValueSome i -> i
  | ValueNone ->
    failwith
      "IInput service not registered. Add Program.withInput to your program."
