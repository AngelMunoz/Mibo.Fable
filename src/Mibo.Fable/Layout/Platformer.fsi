namespace Mibo.Layout

module Platformer =
  [<Struct>]
  type Anchor =
    | Left
    | Right

  [<Struct>]
  type StairDirection =
    | UpRight
    | UpLeft
    | DownRight
    | DownLeft

  val inline box:
    width: int ->
    height: int ->
    border: 'T ->
    fill: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline platform:
    width: int -> tile: 'T -> section: GridSection2D<'T> -> GridSection2D<'T>

  val inline ledge:
    width: int ->
    anchor: Anchor ->
    tile: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline wall:
    height: int -> tile: 'T -> section: GridSection2D<'T> -> GridSection2D<'T>

  val inline pillar:
    height: int ->
    baseTile: 'T ->
    middleTile: 'T ->
    topTile: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline stairs:
    width: int ->
    tile: 'T ->
    direction: StairDirection ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline slope:
    width: int ->
    height: int ->
    tile: 'T ->
    direction: StairDirection ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline pit:
    width: int -> depth: int -> section: GridSection2D<'T> -> GridSection2D<'T>

  val inline gap:
    width: int -> height: int -> section: GridSection2D<'T> -> GridSection2D<'T>

  val inline scatterEdges:
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline weather:
    oldContent: 'T ->
    newContent: 'T ->
    probability: float32 ->
    seed: int ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>
      when 'T: equality
