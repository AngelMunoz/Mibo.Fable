namespace Mibo.Elmish.Graphics3D

open Mibo.Vectors
open Mibo

// ─────────────────────────────────────────────────────────────────────────────
// Backend-neutral 3D light definitions.
//
// These structs use Mibo.Color (backend-neutral byte RGBA) and System.Numerics
// vectors. Both raylib and MonoGame previously had byte-for-byte identical
// copies of this file, differing only in which Color/Vector3 namespace they
// opened. This is the single shared implementation.
//
// Backends convert Mibo.Color → native Color or normalized Vector3/4 at the
// pipeline upload boundary (inlineable, zero cost).
// ─────────────────────────────────────────────────────────────────────────────

[<Struct>]
type AmbientLight3D = { Color: Color; Intensity: float32 }

[<Struct>]
type DirectionalLight3D = {
  Direction: Vector3
  Color: Color
  Intensity: float32
  CastsShadows: bool
}

[<Struct>]
type PointLight3D = {
  Position: Vector3
  Color: Color
  Intensity: float32
  Radius: float32
  Falloff: float32
  CastsShadows: bool
  ShadowDirection: Vector3 voption
  ShadowBias: float32 voption
}

[<Struct>]
type SpotLight3D = {
  Position: Vector3
  Direction: Vector3
  Color: Color
  Intensity: float32
  Radius: float32
  InnerCutoff: float32
  OuterCutoff: float32
  CastsShadows: bool
  ShadowBias: float32 voption
}
