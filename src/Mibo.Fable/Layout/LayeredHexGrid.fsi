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

val create:
  width: int ->
  height: int ->
  size: float32 ->
  origin: Vector2 ->
  orientation: HexOrientation ->
    LayeredHexGrid<'T>

val getOrAddLayer:
  index: int ->
  grid: LayeredHexGrid<'T> ->
    struct (HexGrid<'T> * LayeredHexGrid<'T>)
