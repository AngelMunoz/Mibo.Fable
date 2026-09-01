namespace Mibo.Elmish

open System
open System.Collections.Generic

/// <summary>
/// Context passed to <c>init</c>, <c>update</c>, and <c>subscribe</c> functions, providing access
/// to game services and runtime information.
/// </summary>
/// <remarks>
/// This is the primary way to access game services (assets, input, etc.) from within
/// the Elmish architecture. Use <see cref="M:Mibo.Elmish.GameContext.getService"/> or the typed accessor
/// conveniences to retrieve registered services.
/// </remarks>
type GameContext =
  internal new: width: int * height: int -> GameContext
  /// The runtime service table. Public because the inline registry helpers
  /// reference it; treat as infrastructure.
  member Services: Dictionary<Type, obj>
  /// <summary>Current window width in pixels.</summary>
  member WindowWidth: int
  /// <summary>Current window height in pixels.</summary>
  member WindowHeight: int
  member internal UpdateDimensions: w: int * h: int -> unit

/// Functions for accessing and managing services in the GameContext.
module GameContext =
  val internal create: width: int * height: int -> GameContext
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

/// <summary>
/// Interface for renderers that draw the model state each frame.
/// </summary>
type IRenderer<'Model> =
  abstract Draw: GameContext * 'Model * GameTime -> unit

/// <summary>
/// A small, allocation-friendly buffer that stores render commands tagged with a sort key.
/// </summary>
/// <remarks>
/// This is the core data structure for deferred rendering. Commands are accumulated
/// during the view phase and then sorted/executed by the renderer.
/// </remarks>
/// <typeparam name="Key">The sort key type (e.g., <c>int&lt;RenderLayer&gt;</c> for 2D, <c>unit</c> for 3D)</typeparam>
/// <typeparam name="Cmd">The render command type</typeparam>
type RenderBuffer<'Key, 'Cmd when 'Key: comparison> =
  new:
    ?capacity: int * ?keyComparer: IComparer<'Key> -> RenderBuffer<'Key, 'Cmd>

  /// Clears all commands from the buffer without deallocating.
  member Clear: unit -> unit
  /// Adds a command with its sort key to the buffer.
  member Add: key: 'Key * cmd: 'Cmd -> unit
  /// Sorts the buffer by key. Call this before iterating if order matters.
  member Sort: unit -> unit
  /// The number of commands currently in the buffer.
  member Count: int
  /// Gets the command at the specified index as a (key, command) struct tuple.
  member Item: i: int -> struct ('Key * 'Cmd)
  /// <summary>
  /// Returns the rented backing array to <see cref="T:System.Buffers.ArrayPool`1"/>.
  /// Call when the buffer is no longer needed (typically from a renderer's
  /// <c>Dispose</c>). After disposal the buffer must not be used.
  /// </summary>
  member Dispose: unit -> unit
  interface IDisposable
