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

/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.AmbientLight3D"/>.</summary>
module AmbientLight3D =

  /// <summary>Creates an ambient light. Defaults: Intensity=1.</summary>
  let create(color: Color) : AmbientLight3D = {
    Color = color
    Intensity = 1.0f
  }

  let inline withIntensity (v: float32) (l: AmbientLight3D) = {
    l with
        Intensity = v
  }

[<Struct>]
type DirectionalLight3D = {
  Direction: Vector3
  Color: Color
  Intensity: float32
  CastsShadows: bool
}

/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.DirectionalLight3D"/>.</summary>
module DirectionalLight3D =

  /// <summary>Creates a directional light. Defaults: Color=White, Intensity=1, CastsShadows=true.</summary>
  let create(direction: Vector3) : DirectionalLight3D = {
    Direction = direction
    Color = Color.White
    Intensity = 1.0f
    CastsShadows = true
  }

  let inline withColor (v: Color) (l: DirectionalLight3D) = { l with Color = v }

  let inline withIntensity (v: float32) (l: DirectionalLight3D) = {
    l with
        Intensity = v
  }

  let inline withCastsShadows (v: bool) (l: DirectionalLight3D) = {
    l with
        CastsShadows = v
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

/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.PointLight3D"/>.</summary>
module PointLight3D =

  /// <summary>Creates a point light. Defaults: Color=White, Intensity=1, Falloff=2, CastsShadows=false, ShadowBias=None.</summary>
  let create(position: Vector3, radius: float32) : PointLight3D = {
    Position = position
    Color = Color.White
    Intensity = 1.0f
    Radius = radius
    Falloff = 2.0f
    CastsShadows = false
    ShadowDirection = ValueNone
    ShadowBias = ValueNone
  }

  let inline withColor (v: Color) (l: PointLight3D) = { l with Color = v }

  let inline withIntensity (v: float32) (l: PointLight3D) = {
    l with
        Intensity = v
  }

  let inline withFalloff (v: float32) (l: PointLight3D) = { l with Falloff = v }

  let inline withCastsShadows (v: bool) (l: PointLight3D) = {
    l with
        CastsShadows = v
  }

  let inline withShadowDirection (v: Vector3) (l: PointLight3D) = {
    l with
        ShadowDirection = ValueSome v
  }

  let inline withShadowBias (v: float32) (l: PointLight3D) = {
    l with
        ShadowBias = ValueSome v
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

/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.SpotLight3D"/>.</summary>
module SpotLight3D =

  /// <summary>Creates a spot light. Defaults: Color=White, Intensity=1, InnerCutoff=0.5, OuterCutoff=0.7, CastsShadows=false, ShadowBias=None.</summary>
  let create
    (position: Vector3, direction: Vector3, radius: float32)
    : SpotLight3D =
    {
      Position = position
      Direction = direction
      Color = Color.White
      Intensity = 1.0f
      Radius = radius
      InnerCutoff = 0.5f
      OuterCutoff = 0.7f
      CastsShadows = false
      ShadowBias = ValueNone
    }

  let inline withColor (v: Color) (l: SpotLight3D) = { l with Color = v }

  let inline withIntensity (v: float32) (l: SpotLight3D) = {
    l with
        Intensity = v
  }

  let inline withCutoff (inner: float32) (outer: float32) (l: SpotLight3D) = {
    l with
        InnerCutoff = inner
        OuterCutoff = outer
  }

  let inline withCastsShadows (v: bool) (l: SpotLight3D) = {
    l with
        CastsShadows = v
  }

  let inline withShadowBias (v: float32) (l: SpotLight3D) = {
    l with
        ShadowBias = ValueSome v
  }
