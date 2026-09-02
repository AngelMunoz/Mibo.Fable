module Mibo.Layout3D.LayeredLayout3D

open Mibo.Layout3D.LayeredGrid3D

let inline layer
  index
  ([<InlineIfLambda>] f: GridSection3D<'T> -> GridSection3D<'T>)
  (grid: LayeredGrid3D<'T>)
  : LayeredGrid3D<'T> =
  let struct (targetGrid, updatedContainer) =
    LayeredGrid3D.getOrAddLayer index grid

  Layout3D.run f targetGrid |> ignore

  updatedContainer
