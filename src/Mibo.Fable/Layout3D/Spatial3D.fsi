namespace Mibo.Layout3D

open System
open System.Buffers
open Mibo.Vectors
open Mibo.Layout

module Grid3DSpatial =
  /// Internal helpers for A* pathfinding. Not intended for direct use.
  module Internal =
    [<Struct>]
    type AStarNode = {
      X: int
      Y: int
      Z: int
      Priority: float32
    }

    /// Min-heap priority queue for 3D A* pathfinding over a pooled backing array.
    [<Struct>]
    type MinHeap = {
      mutable Items: AStarNode[]
      mutable Count: int
    }

  /// Returns the 6 face-adjacent neighbors of (x, y, z).
  val inline neighbors6:
    x: int ->
    y: int ->
    z: int ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int)[]

  /// Returns the 26 surrounding neighbors (face + edge + corner).
  val inline neighbors26:
    x: int ->
    y: int ->
    z: int ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int)[]

  /// Manhattan distance in 3D.
  val inline distanceManhattan:
    x1: int -> y1: int -> z1: int -> x2: int -> y2: int -> z2: int -> int

  /// Chebyshev distance in 3D (diagonal = 1).
  val inline distanceChebyshev:
    x1: int -> y1: int -> z1: int -> x2: int -> y2: int -> z2: int -> int

  /// Euclidean distance in 3D.
  val inline distanceEuclidean:
    x1: int -> y1: int -> z1: int -> x2: int -> y2: int -> z2: int -> float32

  /// Converts a world position to the grid cell that contains it. Returns ValueNone
  /// if the position is outside the grid.
  val inline worldToCell:
    worldPos: Vector3 ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int) voption

  /// Returns all cells within Chebyshev distance `range` of (x, y, z).
  val inline inRange:
    x: int ->
    y: int ->
    z: int ->
    range: int ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int)[]

  /// Returns true if a 3D line from (x1,y1,z1) to (x2,y2,z2) is clear of
  /// blocked cells. Uses 3D Bresenham. Start not checked; goal IS checked.
  val inline lineOfSight:
    x1: int ->
    y1: int ->
    z1: int ->
    x2: int ->
    y2: int ->
    z2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> int -> bool) ->
    grid: CellGrid3D<'T> ->
      bool

  /// Returns the visible cells along a 3D line from (x1,y1,z1) toward
  /// (x2,y2,z2), stopping at the first blocked cell.
  val inline lineOfSightCells:
    x1: int ->
    y1: int ->
    z1: int ->
    x2: int ->
    y2: int ->
    z2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> int -> bool) ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int)[]

  /// Flood fill from (x, y, z) using BFS over 6-connected neighbors.
  val inline floodFill:
    x: int ->
    y: int ->
    z: int ->
    [<InlineIfLambda>] predicate: (int -> int -> int -> bool) ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int)[]

  /// A* pathfinding on a 3D voxel grid. Returns the shortest path as an
  /// array of coordinates, or ValueNone if no path exists.
  val inline findPath:
    startX: int ->
    startY: int ->
    startZ: int ->
    goalX: int ->
    goalY: int ->
    goalZ: int ->
    [<InlineIfLambda>] isPassable: (int -> int -> int -> bool) ->
    [<InlineIfLambda>] costFn:
      (int -> int -> int -> int -> int -> int -> float32) ->
    grid: CellGrid3D<'T> ->
      struct (int * int * int)[] voption

module Hex3DSpatial =

  /// Returns the 8 neighbors of a hex cell in 3D: 6 hex neighbors on the
  /// same layer plus the cells directly above and below.
  val inline neighbors:
    col: int ->
    row: int ->
    layer: int ->
    grid: HexGrid3D<'T> ->
      struct (int * int * int)[]

  /// Returns only the 6 hex neighbors on the same layer.
  val inline neighborsHex:
    col: int ->
    row: int ->
    layer: int ->
    grid: HexGrid3D<'T> ->
      struct (int * int * int)[]

  /// Hex distance in 3D: hex distance on the plane + layer difference.
  val inline distance:
    c1: int ->
    r1: int ->
    l1: int ->
    c2: int ->
    r2: int ->
    l2: int ->
    grid: HexGrid3D<'T> ->
      int

  /// Converts a 3D world position to the nearest hex3D cell.
  /// The hex (XZ) plane resolves to the nearest hex center; the Y axis resolves
  /// to the containing layer.
  val inline worldToCell:
    worldPos: Vector3 -> grid: HexGrid3D<'T> -> struct (int * int * int) voption

  /// Returns all hex3D cells within `range` steps (hex + vertical).
  val inline inRange:
    col: int ->
    row: int ->
    layer: int ->
    range: int ->
    grid: HexGrid3D<'T> ->
      struct (int * int * int)[]

  /// Returns true if a hex3D line is clear of blocked cells.
  val inline lineOfSight:
    c1: int ->
    r1: int ->
    l1: int ->
    c2: int ->
    r2: int ->
    l2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> int -> bool) ->
    grid: HexGrid3D<'T> ->
      bool

  /// Flood fill from (col, row, layer) using BFS over hex3D neighbors.
  val inline floodFill:
    col: int ->
    row: int ->
    layer: int ->
    [<InlineIfLambda>] predicate: (int -> int -> int -> bool) ->
    grid: HexGrid3D<'T> ->
      struct (int * int * int)[]

  /// A* pathfinding on a hex3D grid. Returns the shortest path or ValueNone.
  val inline findPath:
    startCol: int ->
    startRow: int ->
    startLayer: int ->
    goalCol: int ->
    goalRow: int ->
    goalLayer: int ->
    [<InlineIfLambda>] isPassable: (int -> int -> int -> bool) ->
    [<InlineIfLambda>] costFn:
      (int -> int -> int -> int -> int -> int -> float32) ->
    grid: HexGrid3D<'T> ->
      struct (int * int * int)[] voption
