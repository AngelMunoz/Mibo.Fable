namespace Mibo.Elmish

// ─────────────────────────────────────────────────────────────────────────────
// IAssetCache: backend-neutral generic asset cache contract.
//
// The typed loaders (Texture/Font/Sound/Model/...) are backend-specific because
// they return native GPU/resource handles. But the generic typed cache — used for
// custom game assets like loaded config, decoders, pooled buffers, etc. — is
// identical across backends. This contract captures that shareable surface so
// portable user code (and the Headless runner) can cache custom assets without
// referencing a backend.
//
// Each backend's IAssets extends IAssetCache, adding the typed loaders.
// ─────────────────────────────────────────────────────────────────────────────

type IAssetCache =
  abstract Get<'T> : key: string -> 'T voption

  abstract Create<'T> : key: string * factory: (unit -> 'T) -> 'T

  abstract GetOrCreate<'T> : key: string * factory: (unit -> 'T) -> 'T

  abstract Clear: unit -> unit

  abstract Dispose: unit -> unit
