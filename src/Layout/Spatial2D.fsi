namespace Mibo.Layout

open System
open System.Buffers
open Mibo.Vectors

module Grid2DSpatial =
  /// Internal helpers for A* pathfinding. Not intended for direct use.
  module Internal =
    [<Struct>]
    type AStarNode = {
      Col: int
      Row: int
      Priority: float32
    }

    /// Min-heap priority queue for A* pathfinding over a pooled backing array.
    [<Struct>]
    type MinHeap = {
      mutable Items: AStarNode[]
      mutable Count: int
    }

    val inline internal create: capacity: int -> MinHeap

    val inline internal count: heap: MinHeap -> int

    val inline internal push: heap: byref<MinHeap> -> node: AStarNode -> unit

    val inline internal tryPop: heap: byref<MinHeap> -> AStarNode voption

    val inline internal dispose: heap: byref<MinHeap> -> unit

  /// Returns the 4 cardinal (N/S/E/W) neighbors of (x, y), filtered to grid bounds.
  val inline neighbors4:
    x: int -> y: int -> grid: CellGrid2D<'T> -> struct (int * int)[]

  /// Returns the 8 surrounding neighbors (cardinal + diagonal), filtered to grid bounds.
  val inline neighbors8:
    x: int -> y: int -> grid: CellGrid2D<'T> -> struct (int * int)[]

  /// Manhattan distance: cost of moving in 4 directions.
  val inline distanceManhattan: x1: int -> y1: int -> x2: int -> y2: int -> int

  /// Chebyshev distance: cost of moving in 8 directions (diagonal = 1).
  val inline distanceChebyshev: x1: int -> y1: int -> x2: int -> y2: int -> int

  /// Euclidean distance (straight-line).
  val inline distanceEuclidean:
    x1: int -> y1: int -> x2: int -> y2: int -> float32

  /// Converts a world position to the grid cell that contains it.
  /// Returns ValueNone if the position is outside the grid.
  val inline worldToCell:
    worldPos: Vector2 -> grid: CellGrid2D<'T> -> struct (int * int) voption

  /// Returns all grid cells within Chebyshev distance `range` of (x, y).
  /// Includes the origin cell when range >= 0.
  val inline inRange:
    x: int ->
    y: int ->
    range: int ->
    grid: CellGrid2D<'T> ->
      struct (int * int)[]

  /// Returns true if a straight line from (x1,y1) to (x2,y2) is clear of
  /// blocked cells. Uses Bresenham's algorithm. The start cell is not checked;
  /// the goal cell IS checked (a blocked goal means LOS is false).
  val inline lineOfSight:
    x1: int ->
    y1: int ->
    x2: int ->
    y2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> bool) ->
    grid: CellGrid2D<'T> ->
      bool

  /// Returns the visible cells along a line from (x1,y1) toward (x2,y2),
  /// stopping at the first blocked cell. The start cell is included if not blocked.
  val inline lineOfSightCells:
    x1: int ->
    y1: int ->
    x2: int ->
    y2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> bool) ->
    grid: CellGrid2D<'T> ->
      struct (int * int)[]

  /// Flood fill from (x, y) using BFS. Returns all reachable cells for which
  /// `predicate` returns true. Does not cross cells where predicate is false.
  val inline floodFill:
    x: int ->
    y: int ->
    [<InlineIfLambda>] predicate: (int -> int -> bool) ->
    grid: CellGrid2D<'T> ->
      struct (int * int)[]

  /// A* pathfinding on a square grid. Returns the shortest path from
  /// (startX, startY) to (goalX, goalY) as an array of coordinates, or
  /// ValueNone if no path exists.
  ///
  /// `isPassable` returns true for cells that can be walked through.
  /// `costFn` returns the movement cost between two adjacent cells.
  val inline findPath:
    startX: int ->
    startY: int ->
    goalX: int ->
    goalY: int ->
    [<InlineIfLambda>] isPassable: (int -> int -> bool) ->
    [<InlineIfLambda>] costFn: (int -> int -> int -> int -> float32) ->
    grid: CellGrid2D<'T> ->
      struct (int * int)[] voption

module Hex2DSpatial =
  /// Internal helpers for hex spatial operations. Not intended for direct use.
  module Internal =
    [<Struct>]
    type AStarNode = {
      Col: int
      Row: int
      Priority: float32
    }

    /// Min-heap priority queue for hex A* pathfinding over a pooled backing array.
    [<Struct>]
    type MinHeap = {
      mutable Items: AStarNode[]
      mutable Count: int
    }

    /// Iterates hex neighbors via callback. Zero allocation.
    val inline internal forEachNeighbor:
      col: int ->
      row: int ->
      w: int ->
      h: int ->
      orientation: HexOrientation ->
      [<InlineIfLambda>] action: (int -> int -> unit) ->
        unit

    // Hex neighbor offsets for PointyTop (offset coords)
    val pointyTopEvenRow: struct (int * int)[]

    val pointyTopOddRow: struct (int * int)[]

    // Hex neighbor offsets for FlatTop (offset coords)
    val flatTopEvenCol: struct (int * int)[]

    val flatTopOddCol: struct (int * int)[]

  /// Converts offset (col, row) to cube (q, r, s) coordinates.
  val inline offsetToCube:
    col: int ->
    row: int ->
    orientation: HexOrientation ->
      struct (int * int * int)

  /// Converts cube (q, r, s) to offset (col, row) coordinates.
  val inline cubeToOffset:
    q: int -> r: int -> orientation: HexOrientation -> struct (int * int)

  /// Rounds fractional cube coordinates to the nearest integer hex.
  val inline cubeRound:
    fq: float32 -> fr: float32 -> fs: float32 -> struct (int * int * int)

  /// Iterates the cells within `range` hex steps via callback. Zero allocation.
  val inline internal forEachInRange:
    col: int ->
    row: int ->
    range: int ->
    w: int ->
    h: int ->
    orientation: HexOrientation ->
    [<InlineIfLambda>] action: (int -> int -> unit) ->
      unit

  /// Returns the 6 hex neighbors of (col, row), filtered to grid bounds.
  val inline neighbors:
    col: int -> row: int -> grid: HexGrid<'T> -> struct (int * int)[]

  /// Hex distance using cube coordinates.
  val inline distance:
    c1: int -> r1: int -> c2: int -> r2: int -> grid: HexGrid<'T> -> int

  /// Converts a world position to the nearest hex cell coordinates.
  /// Returns ValueNone if outside the grid.
  val inline worldToCell:
    worldPos: Vector2 -> grid: HexGrid<'T> -> struct (int * int) voption

  /// Returns all hex cells within `range` hex steps of (col, row).
  val inline inRange:
    col: int ->
    row: int ->
    range: int ->
    grid: HexGrid<'T> ->
      struct (int * int)[]

  /// Returns all hex cells exactly `radius` hex steps from (col, row).
  val inline ring:
    col: int ->
    row: int ->
    radius: int ->
    grid: HexGrid<'T> ->
      struct (int * int)[]

  /// Returns all hex cells within `radius` hex steps, in spiral order
  /// (center first, then ring 1, ring 2, ...).
  val inline spiral:
    col: int ->
    row: int ->
    radius: int ->
    grid: HexGrid<'T> ->
      struct (int * int)[]

  /// Returns true if a hex line from (c1,r1) to (c2,r2) is clear of blocked cells.
  /// The start cell is not checked; the goal IS checked.
  val inline lineOfSight:
    c1: int ->
    r1: int ->
    c2: int ->
    r2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> bool) ->
    grid: HexGrid<'T> ->
      bool

  /// Returns the visible hex cells along a line from (c1,r1) toward (c2,r2),
  /// stopping at the first blocked cell.
  val inline lineOfSightCells:
    c1: int ->
    r1: int ->
    c2: int ->
    r2: int ->
    [<InlineIfLambda>] isBlocked: (int -> int -> bool) ->
    grid: HexGrid<'T> ->
      struct (int * int)[]

  /// Flood fill from (col, row) using BFS over hex neighbors.
  /// Returns all reachable hex cells for which `predicate` returns true.
  val inline floodFill:
    col: int ->
    row: int ->
    [<InlineIfLambda>] predicate: (int -> int -> bool) ->
    grid: HexGrid<'T> ->
      struct (int * int)[]

  /// A* pathfinding on a hex grid. Returns the shortest path from start to
  /// goal as an array of hex coordinates, or ValueNone if no path exists.
  val inline findPath:
    startCol: int ->
    startRow: int ->
    goalCol: int ->
    goalRow: int ->
    [<InlineIfLambda>] isPassable: (int -> int -> bool) ->
    [<InlineIfLambda>] costFn: (int -> int -> int -> int -> float32) ->
    grid: HexGrid<'T> ->
      struct (int * int)[] voption
