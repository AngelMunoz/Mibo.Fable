module Mibo.Color

open Mibo.Vectors

/// <summary>Create a color from RGBA byte values.</summary>
let inline create (r: byte) (g: byte) (b: byte) (a: byte) : Color = {
  R = r
  G = g
  B = b
  A = a
}

/// <summary>Create an opaque color from RGB byte values (alpha = 255).</summary>
let inline rgb (r: byte) (g: byte) (b: byte) : Color = create r g b 255uy

let White: Color = rgb 255uy 255uy 255uy

/// <summary>Black (0, 0, 0, 255).</summary>
let Black: Color = rgb 0uy 0uy 0uy

/// <summary>Red (255, 0, 0, 255).</summary>
let Red: Color = rgb 255uy 0uy 0uy

/// <summary>Green (0, 255, 0, 255).</summary>
let Green: Color = rgb 0uy 255uy 0uy

/// <summary>Blue (0, 0, 255, 255).</summary>
let Blue: Color = rgb 0uy 0uy 255uy

/// <summary>Transparent (0, 0, 0, 0).</summary>
let Transparent: Color = create 0uy 0uy 0uy 0uy

/// <summary>Convert a color to a normalized Vector3 (RGB in [0,1], alpha dropped).</summary>
let inline toVector3(c: Color) : Vector3 =
  Vector3(float32 c.R / 255.0f, float32 c.G / 255.0f, float32 c.B / 255.0f)

/// <summary>Convert a color to a normalized Vector4 (RGBA in [0,1]).</summary>
let inline toVector4(c: Color) : Vector4 =
  Vector4(
    float32 c.R / 255.0f,
    float32 c.G / 255.0f,
    float32 c.B / 255.0f,
    float32 c.A / 255.0f
  )
