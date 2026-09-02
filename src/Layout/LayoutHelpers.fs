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
  let inline createSection(grid: CellGrid2D<'T>) : GridSection2D<'T> = {
    BackingGrid = grid
    OffsetX = 0
    OffsetY = 0
    Width = grid.Width
    Height = grid.Height
  }

  let inline setLocal
    (lx: int)
    (ly: int)
    (content: 'T)
    (section: GridSection2D<'T>)
    : unit =
    let gx = section.OffsetX + lx
    let gy = section.OffsetY + ly

    if
      gx >= 0
      && gx < section.BackingGrid.Width
      && gy >= 0
      && gy < section.BackingGrid.Height
    then
      set gx gy content section.BackingGrid
