module Mibo.Layout.LayeredLayout

open Mibo.Layout.LayeredGrid2D

val inline layer:
  index: int ->
  [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
  grid: LayeredGrid2D<'T> ->
    LayeredGrid2D<'T>
