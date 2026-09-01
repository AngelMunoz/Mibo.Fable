namespace Mibo.Layout

open System.Collections.Generic
open Mibo.Vectors

type LayeredGrid2D<'T> = {
  Width: int
  Height: int
  CellSize: Vector2
  Origin: Vector2
  Layers: Dictionary<int, CellGrid2D<'T>>
}

module LayeredGrid2D =
  val create:
    width: int ->
    height: int ->
    cellSize: Vector2 ->
    origin: Vector2 ->
      LayeredGrid2D<'T>

  val getOrAddLayer:
    index: int ->
    grid: LayeredGrid2D<'T> ->
      struct (CellGrid2D<'T> * LayeredGrid2D<'T>)

module LayeredLayout =
  val inline layer:
    index: int ->
    [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
    grid: LayeredGrid2D<'T> ->
      LayeredGrid2D<'T>
