/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.DirectionalLight3D"/>.</summary>
module Mibo.Elmish.Graphics3D.DirectionalLight3D

open Mibo.Vectors
open Mibo

/// <summary>Creates a directional light. Defaults: Color=White, Intensity=1, CastsShadows=true.</summary>
val create: direction: Vector3 -> DirectionalLight3D

val inline withColor: v: Color -> l: DirectionalLight3D -> DirectionalLight3D

val inline withIntensity:
  v: float32 -> l: DirectionalLight3D -> DirectionalLight3D

val inline withCastsShadows:
  v: bool -> l: DirectionalLight3D -> DirectionalLight3D
