namespace Mibo.Elmish

/// <summary>
/// Backend-neutral generic asset cache: stores arbitrary user-created assets by
/// string key, with create-or-get semantics.
/// </summary>
/// <remarks>
/// This is the shareable subset of asset caching. Backend-specific asset services
/// (e.g. raylib's <c>IAssets</c>) extend this interface to add typed loaders that
/// return native GPU/resource handles (<c>Texture2D</c>, <c>Font</c>, <c>Sound</c>,
/// etc.), which are inherently backend-specific.
/// </remarks>
/// <example>
/// <code>
/// /// Each backend registers its own IAssets (which inherits IAssetCache)
/// /// under the IAssets key, so resolve via the backend's IAssets type:
/// let cache = GameContext.getService&lt;IAssets&gt; ctx
/// let config = cache.GetOrCreate("gameConfig", fun () -> loadConfig())
/// </code>
/// </example>
type IAssetCache =
  /// <summary>Gets a previously created custom asset by key.</summary>
  abstract Get<'T> : key: string -> 'T voption

  /// <summary>Creates and caches a custom asset using the provided factory.</summary>
  abstract Create<'T> : key: string * factory: (unit -> 'T) -> 'T

  /// <summary>Gets a cached asset or creates it if not present.</summary>
  /// <remarks>This is the preferred method for custom assets - idempotent, ensures assets are created only once.</remarks>
  abstract GetOrCreate<'T> : key: string * factory: (unit -> 'T) -> 'T

  /// <summary>Clears all caches (does not dispose GPU resources).</summary>
  abstract Clear: unit -> unit

  /// <summary>Disposes all cached assets and clears caches.</summary>
  abstract Dispose: unit -> unit
