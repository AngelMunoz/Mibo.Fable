/// <summary>Tests for the ThreeJS foundation conversions.</summary>
module ThreeConversions.Tests

open Mibo
open Mibo.Vectors
open Mibo.Testing.QUnit
open Mibo.Fable.ThreeJS.Conversions

QUnit.``module`` "ThreeJS conversions"

QUnit.test(
  "color to hex packs RGB channels",
  fun assert' ->
    assert'.strictEqual(
      colorToHexRgb Color.Black,
      0x000000,
      "black packs to zero"
    )

    assert'.strictEqual(
      colorToHexRgb Color.White,
      0xffffff,
      "white packs to max"
    )

    assert'.strictEqual(
      colorToHexRgb Color.Red,
      0xff0000,
      "red keeps red channel"
    )

    assert'.strictEqual(
      colorToHexRgb Color.Green,
      0x00ff00,
      "green keeps green channel"
    )

    assert'.strictEqual(
      colorToHexRgb Color.Blue,
      0x0000ff,
      "blue keeps blue channel"
    )
)

QUnit.test(
  "color alpha maps to zero one range",
  fun assert' ->
    assert'.strictEqual(
      colorAlpha01 Color.Transparent,
      0.0,
      "transparent is zero"
    )

    assert'.strictEqual(colorAlpha01 Color.White, 1.0, "opaque is one")
)

QUnit.test(
  "vector triple round trips through Mibo vectors",
  fun assert' ->
    let v = Vector3(1.0f, -2.0f, 0.5f)
    let triple = vector3ToTriple v
    assert'.deepEqual(triple, (1.0, -2.0, 0.5), "triple holds float values")
    let back = tripleToVector3 triple
    assert'.deepEqual(back, v, "round trip keeps vector")

    assert'.deepEqual(
      vector2ToPair(Vector2(3.0f, 4.0f)),
      (3.0, 4.0),
      "pair holds float values"
    )
)
