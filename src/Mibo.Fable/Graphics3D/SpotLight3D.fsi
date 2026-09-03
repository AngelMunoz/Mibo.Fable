/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.SpotLight3D"/>.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Elmish.Graphics3D.SpotLight3D

open Mibo.Vectors
open Mibo

/// <summary>Creates a spot light. Defaults: Color=White, Intensity=1, InnerCutoff=0.5, OuterCutoff=0.7, CastsShadows=false, ShadowBias=None.</summary>
val create:
  position: Vector3 * direction: Vector3 * radius: float32 -> SpotLight3D

val inline withColor: v: Color -> l: SpotLight3D -> SpotLight3D

val inline withIntensity: v: float32 -> l: SpotLight3D -> SpotLight3D

val inline withCutoff:
  inner: float32 -> outer: float32 -> l: SpotLight3D -> SpotLight3D

val inline withCastsShadows: v: bool -> l: SpotLight3D -> SpotLight3D

val inline withShadowBias: v: float32 -> l: SpotLight3D -> SpotLight3D
