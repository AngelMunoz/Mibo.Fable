namespace Mibo.Layout

open CellGrid2D

[<Struct>]
type GridSection2D<'T> = {
  BackingGrid: CellGrid2D<'T>
  OffsetX: int
  OffsetY: int
  Width: int
  Height: int
}

[<AutoOpen>]
module LayoutHelpers =
  val createSection: grid: CellGrid2D<'T> -> GridSection2D<'T>

  val inline setLocal:
    lx: int -> ly: int -> content: 'T -> section: GridSection2D<'T> -> unit

module Layout =
  val inline run:
    [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
    grid: CellGrid2D<'T> ->
      CellGrid2D<'T>

  val set:
    x: int ->
    y: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val repeatX:
    x: int ->
    y: int ->
    count: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val repeatY:
    x: int ->
    y: int ->
    count: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val fill:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val rect:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    borderContent: 'T ->
    fillContent: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val scatterBorder:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val line:
    x1: int ->
    y1: int ->
    x2: int ->
    y2: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val clear:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val replaceScatter:
    oldContent: 'T ->
    newContent: 'T ->
    probability: float32 ->
    seed: int ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>
      when 'T: equality

  val inline section:
    x: int ->
    y: int ->
    [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
    parent: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline padding:
    n: int ->
    [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
    parent: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline paddingEx:
    left: int ->
    top: int ->
    right: int ->
    bottom: int ->
    [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
    parent: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline center:
    w: int ->
    h: int ->
    [<InlineIfLambda>] f: (GridSection2D<'T> -> GridSection2D<'T>) ->
    parent: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline flowX:
    step: int ->
    stamps: seq<GridSection2D<'T> -> GridSection2D<'T>> ->
    parent: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline flowY:
    step: int ->
    stamps: seq<GridSection2D<'T> -> GridSection2D<'T>> ->
    parent: GridSection2D<'T> ->
      GridSection2D<'T>

  val border:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val corners:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val scatterLine:
    x1: int ->
    y1: int ->
    x2: int ->
    y2: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val checkerBorder:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    odd: 'T ->
    even: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline generate:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline iter:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    [<InlineIfLambda>] action: (int -> int -> 'T voption -> unit) ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val inline map:
    x: int ->
    y: int ->
    width: int ->
    height: int ->
    [<InlineIfLambda>] mapping: ('T -> 'T) ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val circle:
    cx: int ->
    cy: int ->
    radius: int ->
    filled: bool ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val polygon:
    points: struct (int * int)[] ->
    filled: bool ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val checker:
    odd: 'T -> even: 'T -> section: GridSection2D<'T> -> GridSection2D<'T>

  val scatter:
    count: int ->
    seed: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>

  val replace:
    oldContent: 'T ->
    newContent: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>
      when 'T: equality

  val inline scatterStamp:
    count: int ->
    seed: int ->
    [<InlineIfLambda>] stamp: (GridSection2D<'T> -> GridSection2D<'T>) ->
    section': GridSection2D<'T> ->
      GridSection2D<'T>

  val setIfEmpty:
    x: int ->
    y: int ->
    content: 'T ->
    section: GridSection2D<'T> ->
      GridSection2D<'T>
