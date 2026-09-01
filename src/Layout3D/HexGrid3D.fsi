namespace Mibo.Layout3D

open Mibo.Vectors
open Mibo.Layout

[<Struct>]
type HexGrid3D<'T> = {
  Origin: Vector3
  HexSize: float32
  LayerHeight: float32
  Orientation: HexOrientation
  Width: int
  Height: int
  Depth: int
  Cells: 'T voption[]
}

module HexGrid3D =
  val inline toIndex:
    col: int -> row: int -> layer: int -> width: int -> depth: int -> int

  val inline hexDimensions:
    size: float32 -> orientation: HexOrientation -> struct (float32 * float32)

  val create:
    width: int ->
    height: int ->
    depth: int ->
    hexSize: float32 ->
    layerHeight: float32 ->
    origin: Vector3 ->
    orientation: HexOrientation ->
      HexGrid3D<'T>

  val inline get:
    col: int -> row: int -> layer: int -> grid: HexGrid3D<'T> -> 'T voption

  val inline set:
    col: int ->
    row: int ->
    layer: int ->
    content: 'T ->
    grid: HexGrid3D<'T> ->
      unit

  val inline clear:
    col: int -> row: int -> layer: int -> grid: HexGrid3D<'T> -> unit

  val inline getWorldPos:
    col: int -> row: int -> layer: int -> grid: HexGrid3D<'T> -> Vector3

  val inline iter:
    action: (int -> int -> int -> 'T -> unit) -> grid: HexGrid3D<'T> -> unit

  val inline iterVolume:
    bounds: BoundingBox ->
    action: (int -> int -> int -> 'T -> unit) ->
    grid: HexGrid3D<'T> ->
      unit
