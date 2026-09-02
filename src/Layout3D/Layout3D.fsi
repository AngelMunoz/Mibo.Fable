namespace Mibo.Layout3D

open CellGrid3D

[<Struct>]
type GridSection3D<'T> = {
  BackingGrid: CellGrid3D<'T>
  OffsetX: int
  OffsetY: int
  OffsetZ: int
  Width: int
  Height: int
  Depth: int
}

[<AutoOpen>]
module Layout3DHelpers =
  val inline createSection: grid: CellGrid3D<'T> -> GridSection3D<'T>

  val inline setLocal:
    lx: int ->
    ly: int ->
    lz: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      unit

  val inline clearLocal:
    lx: int -> ly: int -> lz: int -> section: GridSection3D<'T> -> unit

module Layout3D =
  val inline run:
    [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
    grid: CellGrid3D<'T> ->
      CellGrid3D<'T>

  val inline section:
    x: int ->
    y: int ->
    z: int ->
    [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline set:
    x: int ->
    y: int ->
    z: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val fill:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val clear:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val floorXZ:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    d: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val wallXY:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val wallYZ:
    x: int ->
    y: int ->
    z: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatterEdges:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatterLine:
    x1: int ->
    y1: int ->
    z1: int ->
    x2: int ->
    y2: int ->
    z2: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val line:
    x1: int ->
    y1: int ->
    z1: int ->
    x2: int ->
    y2: int ->
    z2: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatterXZ:
    y: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatterXY:
    z: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatterYZ:
    x: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val replaceScatter:
    oldContent: 'T ->
    newContent: 'T ->
    probability: float32 ->
    seed: int ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>
      when 'T: equality

  val inline padding:
    n: int ->
    [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline paddingEx:
    left: int ->
    bottom: int ->
    back: int ->
    right: int ->
    top: int ->
    front: int ->
    [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline center:
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] f: (GridSection3D<'T> -> GridSection3D<'T>) ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>

  val shell:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val edges:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val repeatX:
    x: int ->
    y: int ->
    z: int ->
    count: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val repeatY:
    x: int ->
    y: int ->
    z: int ->
    count: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val repeatZ:
    x: int ->
    y: int ->
    z: int ->
    count: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline column:
    x: int ->
    y: int ->
    z: int ->
    height: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val sphere:
    cx: int ->
    cy: int ->
    cz: int ->
    radius: int ->
    filled: bool ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val cylinder:
    cx: int ->
    cz: int ->
    y: int ->
    radius: int ->
    height: int ->
    filled: bool ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline generate:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] generator: (int -> int -> int -> 'T) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatter3D:
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val scatterShell:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val checker3D:
    odd: 'T -> even: 'T -> section: GridSection3D<'T> -> GridSection3D<'T>

  val checkerXZ:
    y: int ->
    odd: 'T ->
    even: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val checkerXY:
    z: int ->
    odd: 'T ->
    even: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val checkerYZ:
    x: int ->
    odd: 'T ->
    even: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val checkerShell:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    odd: 'T ->
    even: 'T ->
    section': GridSection3D<'T> ->
      GridSection3D<'T>

  val inline generateXZ:
    y: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline generateXY:
    z: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline generateYZ:
    x: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline iter:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] action: (int -> int -> int -> 'T voption -> unit) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline map:
    x: int ->
    y: int ->
    z: int ->
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] mapping: ('T -> 'T) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val replace:
    oldContent: 'T ->
    newContent: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>
      when 'T: equality

  val inline scatterStamp:
    count: int ->
    seed: int ->
    [<InlineIfLambda>] stamp: (GridSection3D<'T> -> GridSection3D<'T>) ->
    section': GridSection3D<'T> ->
      GridSection3D<'T>

  val setIfEmpty:
    x: int ->
    y: int ->
    z: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline flowX:
    step: int ->
    stamps: (GridSection3D<'T> -> GridSection3D<'T>) seq ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline flowY:
    step: int ->
    stamps: (GridSection3D<'T> -> GridSection3D<'T>) seq ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline flowZ:
    step: int ->
    stamps: (GridSection3D<'T> -> GridSection3D<'T>) seq ->
    parent: GridSection3D<'T> ->
      GridSection3D<'T>
