/// <summary>Convenience constructors and conversions for <see cref="T:Mibo.Color"/>.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Color

open Mibo.Vectors

/// Creates a color from RGBA byte values.
val inline create: r: byte -> g: byte -> b: byte -> a: byte -> Color

/// Creates an opaque color from RGB bytes (alpha = 255).
val inline rgb: r: byte -> g: byte -> b: byte -> Color

/// Converts to a Vector3 (R, G, B normalized to 0..1).
val inline toVector3: c: Color -> Vector3

/// Converts to a Vector4 (R, G, B, A normalized to 0..1).
val inline toVector4: c: Color -> Vector4

/// <summary>White (255, 255, 255, 255).</summary>
val White: Color

val Black: Color
val Red: Color
val Green: Color
val Blue: Color
val Transparent: Color
