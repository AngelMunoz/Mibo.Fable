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
  val createHex3DSection: grid: HexGrid3D<'T> -> HexGrid3DSection<'T>

  val inline setHex3DLocal:
    lc: int ->
    lr: int ->
    ll: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      unit

  val inline clearHex3DLocal:
    lc: int -> lr: int -> ll: int -> section: HexGrid3DSection<'T> -> unit
