module Mibo.Elmish.Graphics3D.PointLight3D

open Mibo.Vectors
open Mibo

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
