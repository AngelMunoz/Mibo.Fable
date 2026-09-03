namespace Mibo.Elmish

open System
open FSharp.UMX

/// <summary>
/// Subscription identifier used as the key for subscription diffing.
/// </summary>
/// <remarks>
/// The Elmish runtime uses SubIds to determine which subscriptions to start,
/// stop, or keep running across frames. Use stable, unique IDs for each subscription.
/// Keep this allocation-free in hot paths (avoid list-based IDs).
/// </remarks>
[<Measure>]
type subId

/// A typed string wrapper for subscription identifiers.
type SubId = string<subId>

/// Functions for creating and manipulating subscription identifiers.
module SubId =
  /// <summary>Wraps a raw string into a <see cref="T:Mibo.Elmish.SubId"/>.</summary>
  val inline ofString: value: string -> SubId
  /// <summary>Extracts the raw string value from a <see cref="T:Mibo.Elmish.SubId"/>.</summary>
  val inline value: id: SubId -> string

  /// <summary>
  /// Prefixes a SubId with a namespace for parent-child subscription composition.
  /// </summary>
  /// <example>
  /// <code>
  /// // Creates "Player/moveInput"
  /// SubId.prefix "Player" (SubId.ofString "moveInput")
  /// </code>
  /// </example>
  val inline prefix: prefix: string -> id: SubId -> SubId
