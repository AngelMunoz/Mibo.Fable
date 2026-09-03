module Mibo.Layout.LayeredHexGrid

open System.Collections.Generic
open Mibo.Vectors
open HexGrid

type LayeredHexGrid<'T> = {
  Width: int
  Height: int
  Size: float32
  Origin: Vector2
  Orientation: HexOrientation
  Layers: Dictionary<int, HexGrid<'T>>
}

let create
  width
  height
  (size: float32)
  (origin: Vector2)
  (orientation: HexOrientation)
  : LayeredHexGrid<'T> =
  {
    Width = width
    Height = height
    Size = size
    Origin = origin
    Orientation = orientation
    Layers = Dictionary()
  }

let getOrAddLayer
  index
  (grid: LayeredHexGrid<'T>)
  : struct (HexGrid<'T> * LayeredHexGrid<'T>) =
  let mutable existing = Unchecked.defaultof<HexGrid<'T>>

  if grid.Layers.TryGetValue(index, &existing) then
    struct (existing, grid)
  else
    let newGrid =
      HexGrid.create
        grid.Width
        grid.Height
        grid.Size
        grid.Origin
        grid.Orientation

    grid.Layers.Add(index, newGrid)
    struct (newGrid, grid)
