module Mibo.Layout3D.LayeredHexGrid3D

open System.Collections.Generic
open Mibo.Vectors
open Mibo.Layout
open HexGrid3D

type LayeredHexGrid3D<'T> = {
  Width: int
  Height: int
  Depth: int
  HexSize: float32
  LayerHeight: float32
  Origin: Vector3
  Orientation: HexOrientation
  Layers: Dictionary<int, HexGrid3D<'T>>
}

let create
  width
  height
  depth
  (hexSize: float32)
  (layerHeight: float32)
  (origin: Vector3)
  (orientation: HexOrientation)
  : LayeredHexGrid3D<'T> =
  {
    Width = width
    Height = height
    Depth = depth
    HexSize = hexSize
    LayerHeight = layerHeight
    Origin = origin
    Orientation = orientation
    Layers = Dictionary()
  }

let getOrAddLayer
  index
  (grid: LayeredHexGrid3D<'T>)
  : struct (HexGrid3D<'T> * LayeredHexGrid3D<'T>) =
  let mutable existing = Unchecked.defaultof<HexGrid3D<'T>>

  if grid.Layers.TryGetValue(index, &existing) then
    struct (existing, grid)
  else
    let newGrid =
      HexGrid3D.create
        grid.Width
        grid.Height
        grid.Depth
        grid.HexSize
        grid.LayerHeight
        grid.Origin
        grid.Orientation

    grid.Layers.Add(index, newGrid)
    struct (newGrid, grid)
