/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.PointLight3D"/>.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Elmish.Graphics3D.PointLight3D

open Mibo.Vectors
open Mibo

/// <summary>Creates a point light. Defaults: Color=White, Intensity=1, Falloff=2, CastsShadows=false, ShadowBias=None.</summary>
val create: position: Vector3 * radius: float32 -> PointLight3D

val inline withColor: v: Color -> l: PointLight3D -> PointLight3D

val inline withIntensity: v: float32 -> l: PointLight3D -> PointLight3D

val inline withFalloff: v: float32 -> l: PointLight3D -> PointLight3D

val inline withCastsShadows: v: bool -> l: PointLight3D -> PointLight3D

val inline withShadowDirection: v: Vector3 -> l: PointLight3D -> PointLight3D

val inline withShadowBias: v: float32 -> l: PointLight3D -> PointLight3D
