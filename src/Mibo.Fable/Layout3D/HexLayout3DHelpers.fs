namespace Mibo.Layout3D

open HexGrid3D

[<Struct>]
type HexGrid3DSection<'T> = {
  BackingGrid: HexGrid3D<'T>
  OffsetCol: int
  OffsetRow: int
  OffsetLayer: int
  Width: int
  Height: int
  Depth: int
}

[<AutoOpen>]
module HexLayout3DHelpers =
  let inline createHex3DSection(grid: HexGrid3D<'T>) : HexGrid3DSection<'T> = {
    BackingGrid = grid
    OffsetCol = 0
    OffsetRow = 0
    OffsetLayer = 0
    Width = grid.Width
    Height = grid.Height
    Depth = grid.Depth
  }

  let inline setHex3DLocal
    (lc: int)
    (lr: int)
    (ll: int)
    (content: 'T)
    (section: HexGrid3DSection<'T>)
    : unit =
    let gc = section.OffsetCol + lc
    let gr = section.OffsetRow + lr
    let gl = section.OffsetLayer + ll

    if
      gc >= 0
      && gc < section.BackingGrid.Width
      && gr >= 0
      && gr < section.BackingGrid.Depth
      && gl >= 0
      && gl < section.BackingGrid.Height
    then
      set gc gr gl content section.BackingGrid

  let inline clearHex3DLocal
    (lc: int)
    (lr: int)
    (ll: int)
    (section: HexGrid3DSection<'T>)
    : unit =
    let gc = section.OffsetCol + lc
    let gr = section.OffsetRow + lr
    let gl = section.OffsetLayer + ll

    if
      gc >= 0
      && gc < section.BackingGrid.Width
      && gr >= 0
      && gr < section.BackingGrid.Depth
      && gl >= 0
      && gl < section.BackingGrid.Height
    then
      clear gc gr gl section.BackingGrid
