namespace Mibo.Layout3D

open Mibo.Vectors

/// A simple axis-aligned bounding box struct for volume queries.
[<Struct>]
type BoundingBox = { Min: Vector3; Max: Vector3 }

[<Struct>]
type CellGrid3D<'T> = {
  Origin: Vector3
  CellSize: Vector3
  Width: int
  Height: int
  Depth: int
  Cells: 'T voption[]
}

module CellGrid3D =
  val inline toIndex:
    x: int -> y: int -> z: int -> width: int -> height: int -> int

  val create:
    width: int ->
    height: int ->
    depth: int ->
    cellSize: Vector3 ->
    origin: Vector3 ->
      CellGrid3D<'T>

  val inline set:
    x: int -> y: int -> z: int -> content: 'T -> grid: CellGrid3D<'T> -> unit

  val inline get:
    x: int -> y: int -> z: int -> grid: CellGrid3D<'T> -> 'T voption

  val inline clear: x: int -> y: int -> z: int -> grid: CellGrid3D<'T> -> unit

  val inline getWorldPos:
    x: int -> y: int -> z: int -> grid: CellGrid3D<'T> -> Vector3

  val inline iter:
    action: (int -> int -> int -> 'T -> unit) -> grid: CellGrid3D<'T> -> unit

  val inline iterVolume:
    bounds: BoundingBox ->
    action: (int -> int -> int -> 'T -> unit) ->
    grid: CellGrid3D<'T> ->
      unit
