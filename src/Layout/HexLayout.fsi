namespace Mibo.Layout

open HexGrid

[<Struct>]
type HexGridSection<'T> = {
  BackingGrid: HexGrid<'T>
  OffsetCol: int
  OffsetRow: int
  Width: int
  Height: int
}

[<AutoOpen>]
module HexLayoutHelpers =
  val createHexSection: grid: HexGrid<'T> -> HexGridSection<'T>

  val inline setHexLocal:
    lc: int -> lr: int -> content: 'T -> section: HexGridSection<'T> -> unit

module HexLayout =
  val inline run:
    [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
    grid: HexGrid<'T> ->
      HexGrid<'T>

  val inline section:
    col: int ->
    row: int ->
    [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
    parent: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline padding:
    n: int ->
    [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
    parent: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline paddingEx:
    left: int ->
    top: int ->
    right: int ->
    bottom: int ->
    [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
    parent: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline center:
    w: int ->
    h: int ->
    [<InlineIfLambda>] f: (HexGridSection<'T> -> HexGridSection<'T>) ->
    parent: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline flowX:
    step: int ->
    stamps: seq<HexGridSection<'T> -> HexGridSection<'T>> ->
    parent: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline flowY:
    step: int ->
    stamps: seq<HexGridSection<'T> -> HexGridSection<'T>> ->
    parent: HexGridSection<'T> ->
      HexGridSection<'T>

  val set:
    col: int ->
    row: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val repeatX:
    col: int ->
    row: int ->
    count: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val repeatY:
    col: int ->
    row: int ->
    count: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val fill:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val border:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val rect:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    borderContent: 'T ->
    fillContent: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val corners:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val scatterBorder:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val scatterLine:
    c1: int ->
    r1: int ->
    c2: int ->
    r2: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val checkerBorder:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    odd: 'T ->
    even: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline generate:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline iter:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    [<InlineIfLambda>] action: (int -> int -> 'T voption -> unit) ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val inline map:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    [<InlineIfLambda>] mapping: ('T -> 'T) ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val line:
    c1: int ->
    r1: int ->
    c2: int ->
    r2: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val circle:
    cc: int ->
    cr: int ->
    radius: int ->
    filled: bool ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val polygon:
    points: struct (int * int)[] ->
    filled: bool ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val checker:
    odd: 'T -> even: 'T -> section: HexGridSection<'T> -> HexGridSection<'T>

  val scatter:
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val clear:
    col: int ->
    row: int ->
    width: int ->
    height: int ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>

  val replace:
    oldContent: 'T ->
    newContent: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>
      when 'T: equality

  val replaceScatter:
    oldContent: 'T ->
    newContent: 'T ->
    probability: float32 ->
    seed: int ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>
      when 'T: equality

  val inline scatterStamp:
    count: int ->
    seed: int ->
    [<InlineIfLambda>] stamp: (HexGridSection<'T> -> HexGridSection<'T>) ->
    section': HexGridSection<'T> ->
      HexGridSection<'T>

  val setIfEmpty:
    col: int ->
    row: int ->
    content: 'T ->
    section: HexGridSection<'T> ->
      HexGridSection<'T>
