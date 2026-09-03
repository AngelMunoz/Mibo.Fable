namespace Mibo.Layout3D

open HexGrid3D

module HexLayout3D =
  val inline run:
    [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    grid: HexGrid3D<'T> ->
      HexGrid3D<'T>

  val inline section:
    col: int ->
    row: int ->
    layer: int ->
    [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline padding:
    n: int ->
    [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline paddingEx:
    left: int ->
    bottom: int ->
    back: int ->
    right: int ->
    top: int ->
    front: int ->
    [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline center:
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] f: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline set:
    col: int ->
    row: int ->
    layer: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val repeatX:
    col: int ->
    row: int ->
    layer: int ->
    count: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val repeatY:
    col: int ->
    row: int ->
    layer: int ->
    count: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val repeatZ:
    col: int ->
    row: int ->
    layer: int ->
    count: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline column:
    col: int ->
    row: int ->
    layer: int ->
    height: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val fill:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val clear:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val floorHex:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val wallXY:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val wallYZ:
    col: int ->
    row: int ->
    layer: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val shell:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val edges:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterEdges:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterLine:
    c1: int ->
    r1: int ->
    l1: int ->
    c2: int ->
    r2: int ->
    l2: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val line:
    c1: int ->
    r1: int ->
    l1: int ->
    c2: int ->
    r2: int ->
    l2: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val sphere:
    cc: int ->
    cr: int ->
    cl: int ->
    radius: int ->
    filled: bool ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val cylinder:
    cc: int ->
    cr: int ->
    layer: int ->
    radius: int ->
    height: int ->
    filled: bool ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline generate:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] generator: (int -> int -> int -> 'T) ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline generateHexLayer:
    layer: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline generateXY:
    row: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline generateYZ:
    col: int ->
    [<InlineIfLambda>] generator: (int -> int -> 'T) ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline iter:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] action: (int -> int -> int -> 'T voption -> unit) ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline map:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    [<InlineIfLambda>] mapping: ('T -> 'T) ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val replace:
    oldContent: 'T ->
    newContent: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>
      when 'T: equality

  val replaceScatter:
    oldContent: 'T ->
    newContent: 'T ->
    probability: float32 ->
    seed: int ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>
      when 'T: equality

  val inline scatterStamp:
    count: int ->
    seed: int ->
    [<InlineIfLambda>] stamp: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) ->
    section': HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val setIfEmpty:
    col: int ->
    row: int ->
    layer: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline flowX:
    step: int ->
    stamps: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) seq ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline flowY:
    step: int ->
    stamps: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) seq ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val inline flowZ:
    step: int ->
    stamps: (HexGrid3DSection<'T> -> HexGrid3DSection<'T>) seq ->
    parent: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatter3D:
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterHexLayer:
    layer: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterXY:
    row: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterYZ:
    col: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterShell:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val checker3D:
    odd: 'T -> even: 'T -> section: HexGrid3DSection<'T> -> HexGrid3DSection<'T>

  val checkerHexLayer:
    layer: int ->
    odd: 'T ->
    even: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val checkerXY:
    row: int ->
    odd: 'T ->
    even: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val checkerYZ:
    col: int ->
    odd: 'T ->
    even: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val checkerShell:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    odd: 'T ->
    even: 'T ->
    section': HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val border:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val rect:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    borderContent: 'T ->
    fillContent: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val corners:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val scatterBorder:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val checkerBorder:
    col: int ->
    row: int ->
    layer: int ->
    w: int ->
    h: int ->
    d: int ->
    odd: 'T ->
    even: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>

  val checker:
    odd: 'T -> even: 'T -> section: HexGrid3DSection<'T> -> HexGrid3DSection<'T>

  val scatter:
    count: int ->
    seed: int ->
    content: 'T ->
    section: HexGrid3DSection<'T> ->
      HexGrid3DSection<'T>
