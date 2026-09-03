/// <summary>Pure conversions between Mibo value types and three.js JS values.</summary>
module Mibo.Fable.ThreeJS.Conversions

open Mibo
open Mibo.Vectors

/// <summary>Converts a Mibo color to a three.js hex int (0xRRGGBB).</summary>
val inline colorToHexRgb: c: Color -> int

/// <summary>Converts a Mibo color alpha byte to 0..1.</summary>
val inline colorAlpha01: c: Color -> float

/// <summary>Converts a Mibo Vector3 (float32) to a float triple for three.js.</summary>
val inline vector3ToTriple: v: Vector3 -> float * float * float

/// <summary>Converts a float triple from three.js to a Mibo Vector3.</summary>
val inline tripleToVector3: xyz: float * float * float -> Vector3

/// <summary>Converts a Mibo Vector2 (float32) to a float pair for three.js.</summary>
val inline vector2ToPair: v: Vector2 -> float * float
