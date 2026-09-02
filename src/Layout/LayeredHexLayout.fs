module Mibo.Layout.LayeredHexLayout

open Mibo.Layout.LayeredHexGrid

let inline layer
  index
  ([<InlineIfLambda>] f: HexGridSection<'T> -> HexGridSection<'T>)
  (grid: LayeredHexGrid<'T>)
  : LayeredHexGrid<'T> =
  let struct (targetGrid, updatedContainer) =
    LayeredHexGrid.getOrAddLayer index grid

  HexLayout.run f targetGrid |> ignore

  updatedContainer
