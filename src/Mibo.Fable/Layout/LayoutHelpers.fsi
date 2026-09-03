namespace Mibo.Layout

open CellGrid2D

[<Struct>]
type GridSection2D<'T> = {
  BackingGrid: CellGrid2D<'T>
  OffsetX: int
  OffsetY: int
  Width: int
  Height: int
}

[<AutoOpen>]
module LayoutHelpers =
  val inline createSection: grid: CellGrid2D<'T> -> GridSection2D<'T>

  val inline setLocal:
    lx: int -> ly: int -> content: 'T -> section: GridSection2D<'T> -> unit
