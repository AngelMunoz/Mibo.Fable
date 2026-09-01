namespace Mibo.Layout

open Mibo.Vectors

[<Struct>]
type HexOrientation =
  | PointyTop
  | FlatTop

[<Struct>]
type HexGrid<'T> = {
  Origin: Vector2
  Size: float32
  Orientation: HexOrientation
  Width: int
  Height: int
  Cells: 'T voption[]
}

module HexGrid =
  val inline toIndex: col: int -> row: int -> width: int -> int

  val inline hexDimensions:
    size: float32 -> orientation: HexOrientation -> struct (float32 * float32)

  val create:
    width: int ->
    height: int ->
    size: float32 ->
    origin: Vector2 ->
    orientation: HexOrientation ->
      HexGrid<'T>

  val inline set:
    col: int -> row: int -> content: 'T -> grid: HexGrid<'T> -> unit

  val inline get: col: int -> row: int -> grid: HexGrid<'T> -> 'T voption

  val inline clear: col: int -> row: int -> grid: HexGrid<'T> -> unit

  val inline getWorldPos: col: int -> row: int -> grid: HexGrid<'T> -> Vector2

  val inline iter:
    [<InlineIfLambda>] action: (int -> int -> 'T -> unit) ->
    grid: HexGrid<'T> ->
      unit

  val inline iterVisible:
    left: float32 ->
    top: float32 ->
    right: float32 ->
    bottom: float32 ->
    [<InlineIfLambda>] action: (int -> int -> 'T -> unit) ->
    grid: HexGrid<'T> ->
      unit
