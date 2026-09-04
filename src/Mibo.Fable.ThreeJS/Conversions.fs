module Mibo.Fable.ThreeJS.Conversions

open Mibo
open Mibo.Vectors

let inline colorToHexRgb(c: Color) : int =
  (int c.R <<< 16) ||| (int c.G <<< 8) ||| int c.B

let inline colorAlpha01(c: Color) : float = float c.A / 255.0

let inline vector3ToTriple(v: Vector3) : float * float * float =
  (float v.X, float v.Y, float v.Z)

let inline tripleToVector3((x, y, z): float * float * float) : Vector3 =
  Vector3(float32 x, float32 y, float32 z)

let inline vector2ToPair(v: Vector2) : float * float = (float v.X, float v.Y)
