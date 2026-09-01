namespace Mibo.Layout3D

module Terrain =
  val inline ground:
    width: int ->
    depth: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline plateau:
    width: int ->
    depth: int ->
    height: int ->
    top: 'T ->
    side: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline pit:
    width: int ->
    depth: int ->
    dropHeight: int ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline rampX:
    width: int ->
    depth: int ->
    rise: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline rampZ:
    width: int ->
    depth: int ->
    rise: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline path:
    points: (int * int * int) list ->
    width: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatterAt:
    y: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatter:
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatterSurface:
    [<InlineIfLambda>] heightFn: (int -> int -> int) ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline checkerSurface:
    [<InlineIfLambda>] heightFn: (int -> int -> int) ->
    odd: 'T ->
    even: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline generateSurface:
    [<InlineIfLambda>] heightFn: (int -> int -> int) ->
    [<InlineIfLambda>] generator: (int -> int -> int -> 'T) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatterPath:
    points: (int * int * int) list ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline scatterStampAt:
    y: int ->
    count: int ->
    seed: int ->
    [<InlineIfLambda>] stamp: (GridSection3D<'T> -> GridSection3D<'T>) ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline heightmap:
    [<InlineIfLambda>] heightFn: (int -> int -> int) ->
    content: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>

  val inline layeredHeightmap:
    [<InlineIfLambda>] heightFn: (int -> int -> int) ->
    topLayer: 'T ->
    midLayer: 'T ->
    midDepth: int ->
    bottomLayer: 'T ->
    section: GridSection3D<'T> ->
      GridSection3D<'T>
