namespace Mibo.Layout

open System.Collections.Generic
open Mibo.Vectors

type LayeredHexGrid<'T> = {
  Width: int
  Height: int
  Size: float32
  Origin: Vector2
  Orientation: HexOrientation
  Layers: Dictionary<int, HexGrid<'T>>
}

module LayeredHexGrid =
  val create:
    width: int ->
    height: int ->
    size: float32 ->
    origin: Vector2 ->
    orientation: HexOrientation ->
      LayeredHexGrid<'T>

  val getOrAddLayer:
    index: int ->
    grid: LayeredHexGrid<'T> ->
      struct (HexGrid<'T> * LayeredHexGrid<'T>)

module LayeredHexLayout =
  val inline layer:
    index: int ->
    [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
    grid: LayeredHexGrid<'T> ->
      LayeredHexGrid<'T>
