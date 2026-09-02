module Mibo.Layout3D.LayeredLayout3D

open Mibo.Layout3D.LayeredGrid3D

val inline layer:
  index: int ->
  [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
  grid: LayeredGrid3D<'T> ->
    LayeredGrid3D<'T>
