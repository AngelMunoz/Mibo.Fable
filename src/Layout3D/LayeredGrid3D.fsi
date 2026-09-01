namespace Mibo.Layout3D

open System.Collections.Generic
open Mibo.Vectors
open CellGrid3D

type LayeredGrid3D<'T> = {
  Width: int
  Height: int
  Depth: int
  CellSize: Vector3
  Origin: Vector3
  Layers: Dictionary<int, CellGrid3D<'T>>
}

module LayeredGrid3D =
  val create:
    width: int ->
    height: int ->
    depth: int ->
    cellSize: Vector3 ->
    origin: Vector3 ->
      LayeredGrid3D<'T>

  val getOrAddLayer:
    index: int ->
    grid: LayeredGrid3D<'T> ->
      struct (CellGrid3D<'T> * LayeredGrid3D<'T>)

module LayeredLayout3D =
  val inline layer:
    index: int ->
    [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
    grid: LayeredGrid3D<'T> ->
      LayeredGrid3D<'T>
