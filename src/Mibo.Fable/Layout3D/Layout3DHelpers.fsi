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
  val inline createSection: grid: CellGrid3D<'T> -> GridSection3D<'T>

  val inline setLocal:
    lx: int ->
    ly: int ->
    lz: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      unit

  val inline clearLocal:
    lx: int -> ly: int -> lz: int -> section: GridSection3D<'T> -> unit
