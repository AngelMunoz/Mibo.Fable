namespace Mibo.Layout

module TopDown =
  [<Struct>]
  type CorridorDirection =
    | Horizontal
    | Vertical
    | DiagonalDownRight
    | DiagonalDownLeft
    | DiagonalUpRight
    | DiagonalUpLeft

  val inline wallSegment:
    length: int -> wall: 'T -> section: GridSection2D<'T> -> GridSection2D<'T>

  val inline doorway:
    length: int -> wall: 'T -> section: GridSection2D<'T> -> GridSection2D<'T>

  val inline corridor:
    length: int ->
    width: int ->
    direction: CorridorDirection ->
    floor: 'T ->
    wall: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline room:
    width: int ->
    height: int ->
    floor: 'T ->
    wall: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

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
