/// <summary>Convenience builders for <see cref="T:Mibo.Elmish.Graphics3D.AmbientLight3D"/>.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Elmish.Graphics3D.AmbientLight3D

open Mibo

/// <summary>Creates an ambient light. Defaults: Intensity=1.</summary>
val create: color: Color -> AmbientLight3D

val inline withIntensity: v: float32 -> l: AmbientLight3D -> AmbientLight3D
