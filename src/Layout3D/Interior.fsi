namespace Mibo.Layout3D

module Interior =
  [<Struct>]
  type DoorSide =
    | North
    | South
    | East
    | West

  val inline room:
    width: int ->
    height: int ->
    depth: int ->
    floor: 'T ->
    wall: 'T ->
    ceiling: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline openRoom:
    width: int ->
    height: int ->
    depth: int ->
    floor: 'T ->
    wall: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline corridorX:
    length: int ->
    width: int ->
    height: int ->
    floor: 'T ->
    wall: 'T ->
    ceiling: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline corridorZ:
    length: int ->
    width: int ->
    height: int ->
    floor: 'T ->
    wall: 'T ->
    ceiling: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline doorway:
    side: DoorSide ->
    doorWidth: int ->
    doorHeight: int ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline stairs:
    width: int ->
    rise: int ->
    run: int ->
    step: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline shaft:
    width: int ->
    depth: int ->
    height: int ->
    wall: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline pillar:
    height: int ->
    baseTile: 'T ->
    middleTile: 'T ->
    topTile: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline window:
    side: DoorSide ->
    windowWidth: int ->
    windowHeight: int ->
    sillHeight: int ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatterWall:
    side: DoorSide ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatterEdges:
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline weather:
    oldContent: 'T ->
    newContent: 'T ->
    probability: float32 ->
    seed: int ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>
      when 'T: equality
