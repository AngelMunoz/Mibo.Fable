module Mibo.Layout3D.LayeredHexLayout3D

open Mibo.Layout3D.LayeredHexGrid3D

let inline layer
  index
  ([<InlineIfLambda>] f: HexGrid3DSection<'T> -> HexGrid3DSection<'T>)
  (grid: LayeredHexGrid3D<'T>)
  : LayeredHexGrid3D<'T> =
  let struct (targetGrid, updatedContainer) =
    LayeredHexGrid3D.getOrAddLayer index grid

  HexLayout3D.run f targetGrid |> ignore

  updatedContainer
