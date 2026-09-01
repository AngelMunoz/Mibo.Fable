namespace Mibo

open Mibo.Vectors

/// <summary>
/// A 32-bit RGBA color (8 bits per channel). Backend-neutral — converts to
/// raylib <c>Color</c> or MonoGame <c>Color</c> at the boundary. Structural
/// equality and comparison are provided automatically by the F# record.
/// </summary>
[<Struct>]
type Color = {
  /// <summary>Red channel (0-255).</summary>
  R: byte
  /// <summary>Green channel (0-255).</summary>
  G: byte
  /// <summary>Blue channel (0-255).</summary>
  B: byte
  /// <summary>Alpha channel (0-255).</summary>
  A: byte
}

/// <summary>Convenience constructors and conversions for <see cref="T:Mibo.Color"/>.</summary>
module Color =
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
