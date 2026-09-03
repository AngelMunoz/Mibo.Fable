namespace Mibo.Layout3D

open CellGrid3D

[<Struct>]
type GridSection3D<'T> = {
  BackingGrid: CellGrid3D<'T>
  OffsetX: int
  OffsetY: int
  OffsetZ: int
  Width: int
  Height: int
  Depth: int
}

[<AutoOpen>]
module Layout3DHelpers =
  let inline createSection(grid: CellGrid3D<'T>) : GridSection3D<'T> = {
    BackingGrid = grid
    OffsetX = 0
    OffsetY = 0
    OffsetZ = 0
    Width = grid.Width
    Height = grid.Height
    Depth = grid.Depth
  }

  let inline setLocal
    (lx: int)
    (ly: int)
    (lz: int)
    (content: 'T)
    (section: GridSection3D<'T>)
    : unit =
    let gx = section.OffsetX + lx
    let gy = section.OffsetY + ly
    let gz = section.OffsetZ + lz

    if
      gx >= 0
      && gx < section.BackingGrid.Width
      && gy >= 0
      && gy < section.BackingGrid.Height
      && gz >= 0
      && gz < section.BackingGrid.Depth
    then
      set gx gy gz content section.BackingGrid

  let inline clearLocal
    (lx: int)
    (ly: int)
    (lz: int)
    (section: GridSection3D<'T>)
    : unit =
    let gx = section.OffsetX + lx
    let gy = section.OffsetY + ly
    let gz = section.OffsetZ + lz

    if
      gx >= 0
      && gx < section.BackingGrid.Width
      && gy >= 0
      && gy < section.BackingGrid.Height
      && gz >= 0
      && gz < section.BackingGrid.Depth
    then
      clear gx gy gz section.BackingGrid
