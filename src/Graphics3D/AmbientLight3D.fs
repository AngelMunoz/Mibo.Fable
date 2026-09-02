module Mibo.Elmish.Graphics3D.AmbientLight3D

open Mibo

/// <summary>Creates an ambient light. Defaults: Intensity=1.</summary>
let create(color: Color) : AmbientLight3D = { Color = color; Intensity = 1.0f }

let inline withIntensity (v: float32) (l: AmbientLight3D) = {
  l with
      Intensity = v
}
