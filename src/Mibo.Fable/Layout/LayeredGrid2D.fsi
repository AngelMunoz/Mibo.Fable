module Mibo.Layout.LayeredGrid2D

open System.Collections.Generic
open Mibo.Vectors
open CellGrid2D

type LayeredGrid2D<'T> = {
  Width: int
  Height: int
  CellSize: Vector2
  Origin: Vector2
  Layers: Dictionary<int, CellGrid2D<'T>>
}

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
