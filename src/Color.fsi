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
