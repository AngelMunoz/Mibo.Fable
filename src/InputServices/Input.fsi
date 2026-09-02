/// <summary>
/// Service accessors and the concrete <c>IInput</c> factory hook.
/// </summary>
/// <remarks>
/// <c>create</c> stays backend-local (it polls the native API). The accessors
/// here let user and framework code retrieve the registered <see cref="T:Mibo.Input.IInput"/>
/// from a <see cref="T:Mibo.Elmish.GameContext"/> without referencing a backend.
/// </remarks>
module Mibo.Input.Input

open Mibo.Elmish

/// <summary>Attempts to get the registered <see cref="T:Mibo.Input.IInput"/> service.</summary>
val tryGetService: ctx: GameContext -> IInput voption

/// <summary>Gets the registered <see cref="T:Mibo.Input.IInput"/> service.</summary>
/// <exception cref="T:System.Exception">Thrown when no IInput is registered (use <see cref="M:Mibo.Elmish.Program.withInput"/>).</exception>
val getService: ctx: GameContext -> IInput
