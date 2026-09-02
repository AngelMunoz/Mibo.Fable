namespace Mibo.Layout

open HexGrid

[<Struct>]
type HexGridSection<'T> = {
  BackingGrid: HexGrid<'T>
  OffsetCol: int
  OffsetRow: int
  Width: int
  Height: int
}

[<AutoOpen>]
module HexLayoutHelpers =
  let createHexSection(grid: HexGrid<'T>) : HexGridSection<'T> = {
    BackingGrid = grid
    OffsetCol = 0
    OffsetRow = 0
    Width = grid.Width
    Height = grid.Height
  }

  let inline setHexLocal
    (lc: int)
    (lr: int)
    (content: 'T)
    (section: HexGridSection<'T>)
    : unit =
    let gc = section.OffsetCol + lc
    let gr = section.OffsetRow + lr

    if
      gc >= 0
      && gc < section.BackingGrid.Width
      && gr >= 0
      && gr < section.BackingGrid.Height
    then
      set gc gr content section.BackingGrid
