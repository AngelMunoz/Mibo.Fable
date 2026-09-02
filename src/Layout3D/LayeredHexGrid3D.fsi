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
