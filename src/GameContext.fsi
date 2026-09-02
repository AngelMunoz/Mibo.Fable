/// Functions for accessing and managing services in the GameContext.
module Mibo.Elmish.GameContext

val internal create: width: int * height: int -> GameContext

/// <summary>Registers a service instance under its type.</summary>
/// <remarks>
/// The registry accessors must stay <c>inline</c>: Fable erases generics,
/// so <c>typeof&lt;'T&gt;</c> resolves only at inlined call sites.
/// </remarks>
val inline internal register: svc: 'T -> ctx: GameContext -> unit

/// <summary>Attempts to get a registered service by type.</summary>
/// <returns><c>ValueSome</c> if the service is registered, <c>ValueNone</c> otherwise.</returns>
/// <remarks>
/// Registration and lookup both key on <c>typeof&lt;'T&gt;</c>, so the stored value
/// always has the requested type and an unbox is safe. A <c>:? 'T</c> test is
/// deliberately avoided: on JS, Fable cannot test against interface types
/// (the test would always be false and lookups would silently fail).
/// </remarks>
val inline tryGetService: ctx: GameContext -> 'T voption

/// <summary>Gets a registered service by type.</summary>
/// <exception cref="T:System.InvalidOperationException">Thrown when the service is not registered.</exception>
val inline getService: ctx: GameContext -> 'T
