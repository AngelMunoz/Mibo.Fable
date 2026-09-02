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
  val createHexSection: grid: HexGrid<'T> -> HexGridSection<'T>

  val inline setHexLocal:
    lc: int -> lr: int -> content: 'T -> section: HexGridSection<'T> -> unit
