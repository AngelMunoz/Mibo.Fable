module Mibo.Layout.LayeredHexLayout

open Mibo.Layout.LayeredHexGrid

val inline layer:
  index: int ->
  [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
  grid: LayeredHexGrid<'T> ->
    LayeredHexGrid<'T>
