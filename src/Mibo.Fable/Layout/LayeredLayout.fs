module Mibo.Layout.LayeredLayout

open Mibo.Layout.LayeredGrid2D

let inline layer
  index
  ([<InlineIfLambda>] f: GridSection2D<'T> -> GridSection2D<'T>)
  (grid: LayeredGrid2D<'T>)
  : LayeredGrid2D<'T> =
  let struct (targetGrid, updatedContainer) =
    LayeredGrid2D.getOrAddLayer index grid

  Layout.run f targetGrid |> ignore

  updatedContainer
