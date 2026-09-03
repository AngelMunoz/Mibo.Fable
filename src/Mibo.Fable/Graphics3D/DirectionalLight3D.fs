[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Elmish.Graphics3D.DirectionalLight3D

open Mibo.Vectors
open Mibo

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
