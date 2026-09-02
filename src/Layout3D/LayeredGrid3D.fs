module Mibo.Layout3D.LayeredGrid3D

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

let create
  width
  height
  depth
  (cellSize: Vector3)
  (origin: Vector3)
  : LayeredGrid3D<'T> =
  {
    Width = width
    Height = height
    Depth = depth
    CellSize = cellSize
    Origin = origin
    Layers = Dictionary()
  }

let getOrAddLayer
  index
  (grid: LayeredGrid3D<'T>)
  : struct (CellGrid3D<'T> * LayeredGrid3D<'T>) =
  let mutable existing = Unchecked.defaultof<CellGrid3D<'T>>

  if grid.Layers.TryGetValue(index, &existing) then
    struct (existing, grid)
  else
    let newGrid =
      CellGrid3D.create
        grid.Width
        grid.Height
        grid.Depth
        grid.CellSize
        grid.Origin

    grid.Layers.Add(index, newGrid)
    struct (newGrid, grid)
