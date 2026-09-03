namespace Mibo.Layout

open Mibo.Vectors

type CellGrid2D<'T> = {
  Origin: Vector2
  CellSize: Vector2
  Width: int
  Height: int
  Cells: 'T voption[]
}

module CellGrid2D =
  val inline toIndex: x: int -> y: int -> width: int -> int

  val create:
    width: int ->
    height: int ->
    cellSize: Vector2 ->
    origin: Vector2 ->
      CellGrid2D<'T>

  val inline set:
    x: int -> y: int -> content: 'T -> grid: CellGrid2D<'T> -> unit

  val inline get: x: int -> y: int -> grid: CellGrid2D<'T> -> 'T voption

  val inline clear: x: int -> y: int -> grid: CellGrid2D<'T> -> unit

  val inline getWorldPos: x: int -> y: int -> grid: CellGrid2D<'T> -> Vector2

  val inline iter:
    [<InlineIfLambda>] action: (int -> int -> 'T -> unit) ->
    grid: CellGrid2D<'T> ->
      unit

  val inline iterVisible:
    left: int ->
    top: int ->
    right: int ->
    bottom: int ->
    [<InlineIfLambda>] action: (int -> int -> 'T -> unit) ->
    grid: CellGrid2D<'T> ->
      unit
