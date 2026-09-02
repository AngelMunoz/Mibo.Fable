module Mibo.Layout3D.LayeredHexLayout3D

open Mibo.Layout3D.LayeredHexGrid3D

val inline layer:
  index: int ->
  [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
  grid: LayeredHexGrid3D<'T> ->
    LayeredHexGrid3D<'T>
