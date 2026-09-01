namespace Mibo.Layout3D

open System.Collections.Generic
open Mibo.Vectors
open Mibo.Layout

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

module LayeredHexGrid3D =
  val create:
    width: int ->
    height: int ->
    depth: int ->
    hexSize: float32 ->
    layerHeight: float32 ->
    origin: Vector3 ->
    orientation: HexOrientation ->
      LayeredHexGrid3D<'T>

  val getOrAddLayer:
    index: int ->
    grid: LayeredHexGrid3D<'T> ->
      struct (HexGrid3D<'T> * LayeredHexGrid3D<'T>)

module LayeredHexLayout3D =
  val inline layer:
    index: int ->
    [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    grid: LayeredHexGrid3D<'T> ->
      LayeredHexGrid3D<'T>
