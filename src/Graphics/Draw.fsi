namespace Mibo.Elmish.Graphics

open Mibo.Vectors
open System.Runtime.CompilerServices
open Mibo
open Mibo.Animation
open Mibo.Elmish.Graphics2D
open Mibo.Elmish.Graphics3D
open Mibo.Layout3D

/// <summary>Rectangle witnesses (fills, outlines, rounded, gradients).</summary>
type WithRects2D<'T
  when 'T: (member AddFillRect:
    float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddRectOutline:
    float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
      unit)
  and 'T: (member AddFillRectRounded:
    float32 *
    float32 *
    float32 *
    float32 *
    Color *
    float32 *
    int *
    int<RenderLayer> ->
      unit)
  and 'T: (member AddRectRoundedOutline:
    float32 *
    float32 *
    float32 *
    float32 *
    Color *
    float32 *
    int *
    float32 *
    int<RenderLayer> ->
      unit)
  and 'T: (member AddRectGradientV:
    int * int * int * int * Color * Color * int<RenderLayer> -> unit)
  and 'T: (member AddRectGradientH:
    int * int * int * int * Color * Color * int<RenderLayer> -> unit)
  and 'T: (member AddRectGradient:
    float32 *
    float32 *
    float32 *
    float32 *
    Color *
    Color *
    Color *
    Color *
    int<RenderLayer> ->
      unit)> = 'T

/// <summary>Circle, sector, and radial-gradient witnesses.</summary>
type WithCircles2D<'T
  when 'T: (member AddFillCircle:
    Vector2 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddCircleOutline:
    Vector2 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddCircleSector:
    Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
      unit)
  and 'T: (member AddCircleSectorOutline:
    Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
      unit)
  and 'T: (member AddCircleGradient:
    int * int * float32 * Color * Color * int<RenderLayer> -> unit)> = 'T

/// <summary>Ring / arc witnesses.</summary>
type WithRings2D<'T
  when 'T: (member AddFillRing:
    Vector2 *
    float32 *
    float32 *
    float32 *
    float32 *
    Color *
    int *
    int<RenderLayer> ->
      unit)
  and 'T: (member AddRingOutline:
    Vector2 *
    float32 *
    float32 *
    float32 *
    float32 *
    Color *
    int *
    int<RenderLayer> ->
      unit)> = 'T

/// <summary>Ellipse witnesses.</summary>
type WithEllipses2D<'T
  when 'T: (member AddFillEllipse:
    int * int * float32 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddEllipseOutline:
    int * int * float32 * float32 * Color * int<RenderLayer> -> unit)> = 'T

/// <summary>Line &amp; curve witnesses.</summary>
type WithLines2D<'T
  when 'T: (member AddLine: Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddLineThick:
    Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)
  and 'T: (member AddBezier:
    Vector2 * Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)> =
  'T

/// <summary>Triangle &amp; polygon witnesses.</summary>
type WithPolygons2D<'T
  when 'T: (member AddTriangle:
    Vector2 * Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddFillPoly:
    Vector2 * int * float32 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddPolyOutline:
    Vector2 * int * float32 * float32 * Color * float32 * int<RenderLayer> ->
      unit)> = 'T

/// <summary>All 2D shape witnesses, composed from the per-family aliases above.</summary>
type WithShapes2D<'T
  when WithRects2D<'T>
  and WithCircles2D<'T>
  and WithRings2D<'T>
  and WithEllipses2D<'T>
  and WithLines2D<'T>
  and WithPolygons2D<'T>> = 'T

/// <summary>
/// The unified fluent draw DSL. Chain members on the render buffer:
/// <code lang="fsharp">
/// buffer
///   .BeginCamera(camera)
///   .FillCircle(400f, 300f, 48f, Color.Blue)
///   .Sprite(playerSprite)
///   .EndCamera()
///   .Text(font, "HP 100", Vector2(10f, 10f), 20f, layer = 1001&lt;RenderLayer&gt;)
/// |&gt; ignore
/// </code>
/// </summary>
[<Class>]
[<Extension>]
type Draw =
  /// <summary>Draws a sprite from the backend's SpriteState record.</summary>
  [<Extension>]
  static member inline sprite< ^B, 'S
    when ^B: (member AddSpriteState: 'S -> unit)> : buffer: ^B * state: 'S -> ^B

  /// <summary>Draws text from the backend's TextState record.</summary>
  [<Extension>]
  static member inline text< ^B, 'S when ^B: (member AddTextState: 'S -> unit)> :
    buffer: ^B * state: 'S -> ^B

  /// <summary>
  /// Draws text from parts. <paramref name="size"/> maps to the backend's
  /// sizing model (raylib: font size in pixels; MonoGame: uniform scale).
  /// <paramref name="spacing"/> is used by raylib and ignored by MonoGame.
  /// </summary>
  [<Extension>]
  static member inline text< ^B, 'F
    when ^B: (member AddText:
      'F * string * Vector2 * float32 * float32 * Color * int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    font: 'F *
    text: string *
    position: Vector2 *
    size: float32 *
    [<OptionalArgument; Struct>] tint: Color voption *
    [<OptionalArgument; Struct>] spacing: float32 voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled rectangle. Coordinates are float pixels (truncated toward zero on MonoGame).</summary>
  [<Extension>]
  static member inline fillRect< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: float32 *
    y: float32 *
    w: float32 *
    h: float32 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Rectangle outline.</summary>
  [<Extension>]
  static member inline rectOutline< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: float32 *
    y: float32 *
    w: float32 *
    h: float32 *
    color: Color *
    [<OptionalArgument; Struct>] thickness: float32 voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled rounded rectangle.</summary>
  [<Extension>]
  static member inline fillRectRounded< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: float32 *
    y: float32 *
    w: float32 *
    h: float32 *
    color: Color *
    [<OptionalArgument; Struct>] roundness: float32 voption *
    [<OptionalArgument; Struct>] segments: int voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Rounded rectangle outline.</summary>
  [<Extension>]
  static member inline rectRoundedOutline< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: float32 *
    y: float32 *
    w: float32 *
    h: float32 *
    color: Color *
    [<OptionalArgument; Struct>] roundness: float32 voption *
    [<OptionalArgument; Struct>] segments: int voption *
    [<OptionalArgument; Struct>] thickness: float32 voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Vertical gradient rectangle (int pixel coords, matching the existing API).</summary>
  [<Extension>]
  static member inline rectGradientV< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: int *
    y: int *
    w: int *
    h: int *
    top: Color *
    bottom: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Horizontal gradient rectangle (int pixel coords, matching the existing API).</summary>
  [<Extension>]
  static member inline rectGradientH< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: int *
    y: int *
    w: int *
    h: int *
    left: Color *
    right: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>4-corner gradient rectangle.</summary>
  [<Extension>]
  static member inline rectGradient< ^B
    when ^B: (member AddFillRect:
      float32 * float32 * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectOutline:
      float32 * float32 * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)
    and ^B: (member AddFillRectRounded:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectRoundedOutline:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      float32 *
      int *
      float32 *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRectGradientV:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradientH:
      int * int * int * int * Color * Color * int<RenderLayer> -> unit)
    and ^B: (member AddRectGradient:
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      Color *
      Color *
      Color *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    x: float32 *
    y: float32 *
    w: float32 *
    h: float32 *
    topLeft: Color *
    bottomLeft: Color *
    topRight: Color *
    bottomRight: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled circle.</summary>
  [<Extension>]
  static member inline fillCircle< ^B
    when ^B: (member AddFillCircle:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleOutline:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleSector:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleSectorOutline:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleGradient:
      int * int * float32 * Color * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    center: Vector2 *
    radius: float32 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Circle outline.</summary>
  [<Extension>]
  static member inline circleOutline< ^B
    when ^B: (member AddFillCircle:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleOutline:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleSector:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleSectorOutline:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleGradient:
      int * int * float32 * Color * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    center: Vector2 *
    radius: float32 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled circle sector (pie slice).</summary>
  [<Extension>]
  static member inline circleSector< ^B
    when ^B: (member AddFillCircle:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleOutline:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleSector:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleSectorOutline:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleGradient:
      int * int * float32 * Color * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    center: Vector2 *
    radius: float32 *
    startAngle: float32 *
    endAngle: float32 *
    color: Color *
    [<OptionalArgument; Struct>] segments: int voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Circle sector outline.</summary>
  [<Extension>]
  static member inline circleSectorOutline< ^B
    when ^B: (member AddFillCircle:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleOutline:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleSector:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleSectorOutline:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleGradient:
      int * int * float32 * Color * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    center: Vector2 *
    radius: float32 *
    startAngle: float32 *
    endAngle: float32 *
    color: Color *
    [<OptionalArgument; Struct>] segments: int voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Radial gradient circle (int pixel center, matching the existing API).</summary>
  [<Extension>]
  static member inline circleGradient< ^B
    when ^B: (member AddFillCircle:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleOutline:
      Vector2 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddCircleSector:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleSectorOutline:
      Vector2 * float32 * float32 * float32 * Color * int * int<RenderLayer> ->
        unit)
    and ^B: (member AddCircleGradient:
      int * int * float32 * Color * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    centerX: int *
    centerY: int *
    radius: float32 *
    inner: Color *
    outer: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled ring / arc.</summary>
  [<Extension>]
  static member inline fillRing< ^B
    when ^B: (member AddFillRing:
      Vector2 *
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRingOutline:
      Vector2 *
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      int *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    center: Vector2 *
    innerRadius: float32 *
    outerRadius: float32 *
    startAngle: float32 *
    endAngle: float32 *
    color: Color *
    [<OptionalArgument; Struct>] segments: int voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Ring / arc outline.</summary>
  [<Extension>]
  static member inline ringOutline< ^B
    when ^B: (member AddFillRing:
      Vector2 *
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      int *
      int<RenderLayer> ->
        unit)
    and ^B: (member AddRingOutline:
      Vector2 *
      float32 *
      float32 *
      float32 *
      float32 *
      Color *
      int *
      int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    center: Vector2 *
    innerRadius: float32 *
    outerRadius: float32 *
    startAngle: float32 *
    endAngle: float32 *
    color: Color *
    [<OptionalArgument; Struct>] segments: int voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled ellipse (int pixel center, matching the existing API).</summary>
  [<Extension>]
  static member inline fillEllipse< ^B
    when ^B: (member AddFillEllipse:
      int * int * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddEllipseOutline:
      int * int * float32 * float32 * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    centerX: int *
    centerY: int *
    radiusH: float32 *
    radiusV: float32 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Ellipse outline (int pixel center, matching the existing API).</summary>
  [<Extension>]
  static member inline ellipseOutline< ^B
    when ^B: (member AddFillEllipse:
      int * int * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddEllipseOutline:
      int * int * float32 * float32 * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    centerX: int *
    centerY: int *
    radiusH: float32 *
    radiusV: float32 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>1-pixel line.</summary>
  [<Extension>]
  static member inline line< ^B
    when ^B: (member AddLine:
      Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddLineThick:
      Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)
    and ^B: (member AddBezier:
      Vector2 * Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)> :
    buffer: ^B *
    start: Vector2 *
    finish: Vector2 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Line with custom thickness.</summary>
  [<Extension>]
  static member inline lineThick< ^B
    when ^B: (member AddLine:
      Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddLineThick:
      Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)
    and ^B: (member AddBezier:
      Vector2 * Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)> :
    buffer: ^B *
    start: Vector2 *
    finish: Vector2 *
    color: Color *
    [<OptionalArgument; Struct>] thickness: float32 voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Connected line segments. The point array is a backend handle
  /// (System.Numerics on raylib, XNA on MonoGame) — no per-frame conversion.
  /// Single-point members take System.Numerics and convert for free.</summary>
  [<Extension>]
  static member inline lineStrip< ^B, 'P
    when ^B: (member AddLineStrip: 'P array * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    points: 'P array *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Quadratic bezier curve.</summary>
  [<Extension>]
  static member inline bezier< ^B
    when ^B: (member AddLine:
      Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddLineThick:
      Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)
    and ^B: (member AddBezier:
      Vector2 * Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)> :
    buffer: ^B *
    start: Vector2 *
    control: Vector2 *
    finish: Vector2 *
    color: Color *
    [<OptionalArgument; Struct>] thickness: float32 voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled triangle from 3 vertices, in any winding order.</summary>
  [<Extension>]
  static member inline triangle< ^B
    when ^B: (member AddTriangle:
      Vector2 * Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddFillPoly:
      Vector2 * int * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddPolyOutline:
      Vector2 * int * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    v1: Vector2 *
    v2: Vector2 *
    v3: Vector2 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>
  /// Filled triangle fan, in any winding order; points[0] is the shared
  /// center. The rim auto-closes: the last rim vertex is connected back
  /// to points[1], so a full convex rim fills its polygon on every
  /// backend. Points array is a backend handle (see LineStrip).
  /// </summary>
  [<Extension>]
  static member inline triangleFan< ^B, 'P
    when ^B: (member AddTriangleFan: 'P array * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    points: 'P array *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>
  /// Filled triangle strip from consecutive point pairs, in any winding
  /// order. Points array is a backend handle (see LineStrip).
  /// </summary>
  [<Extension>]
  static member inline triangleStrip< ^B, 'P
    when ^B: (member AddTriangleStrip:
      'P array * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    points: 'P array *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Filled regular polygon.</summary>
  [<Extension>]
  static member inline fillPoly< ^B
    when ^B: (member AddTriangle:
      Vector2 * Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddFillPoly:
      Vector2 * int * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddPolyOutline:
      Vector2 * int * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    center: Vector2 *
    sides: int *
    radius: float32 *
    rotation: float32 *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Regular polygon outline.</summary>
  [<Extension>]
  static member inline polyOutline< ^B
    when ^B: (member AddTriangle:
      Vector2 * Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddFillPoly:
      Vector2 * int * float32 * float32 * Color * int<RenderLayer> -> unit)
    and ^B: (member AddPolyOutline:
      Vector2 * int * float32 * float32 * Color * float32 * int<RenderLayer> ->
        unit)> :
    buffer: ^B *
    center: Vector2 *
    sides: int *
    radius: float32 *
    rotation: float32 *
    color: Color *
    [<OptionalArgument; Struct>] thickness: float32 voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Begins a camera transform (2D or 3D — the buffer selects the witness).</summary>
  [<Extension>]
  static member inline beginCamera< ^B, 'C
    when ^B: (member AddBeginCamera: 'C * int<RenderLayer> -> unit)> :
    buffer: ^B *
    camera: 'C *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Begins a camera with explicit rendering config (viewport, clear).</summary>
  [<Extension>]
  static member inline beginCameraWith< ^B, 'C
    when ^B: (member AddBeginCameraConfig: 'C * int<RenderLayer> -> unit)> :
    buffer: ^B *
    config: 'C *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Ends the current camera transform.</summary>
  [<Extension>]
  static member inline endCamera< ^B
    when ^B: (member AddEndCamera: int<RenderLayer> -> unit)> :
    buffer: ^B * [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Begins a 2D shader/effect block.</summary>
  [<Extension>]
  static member inline beginShader< ^B, 'S
    when ^B: (member AddBeginShader: 'S * int<RenderLayer> -> unit)> :
    buffer: ^B *
    shader: 'S *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Ends the current 2D shader block.</summary>
  [<Extension>]
  static member inline endShader< ^B
    when ^B: (member AddEndShader: int<RenderLayer> -> unit)> :
    buffer: ^B * [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Begins rendering to a render target.</summary>
  [<Extension>]
  static member inline beginTarget< ^B, 'T
    when ^B: (member AddBeginTarget: 'T * int<RenderLayer> -> unit)> :
    buffer: ^B *
    target: 'T *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Ends rendering to a render target.</summary>
  [<Extension>]
  static member inline endTarget< ^B
    when ^B: (member AddEndTarget: int<RenderLayer> -> unit)> :
    buffer: ^B * [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Sets the blending mode (backend handle: raylib BlendMode enum or MonoGame BlendMode DU).</summary>
  [<Extension>]
  static member inline setBlend< ^B, 'M
    when ^B: (member AddSetBlend: 'M * int<RenderLayer> -> unit)> :
    buffer: ^B *
    mode: 'M *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>
  /// Sets the sampler state (e.g. SamplerState.PointClamp) — stops tile-atlas
  /// bleeding. <b>MonoGame only</b>: the raylib buffer has no witness, so
  /// calling this there is a compile error.
  /// </summary>
  [<Extension>]
  static member inline setSamplerState< ^B, 'S
    when ^B: (member AddSamplerState: 'S * int<RenderLayer> -> unit)> :
    buffer: ^B *
    sampler: 'S *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Enables scissor testing.</summary>
  [<Extension>]
  static member inline setScissor< ^B
    when ^B: (member AddSetScissor:
      int * int * int * int * int<RenderLayer> -> unit)> :
    buffer: ^B *
    x: int *
    y: int *
    w: int *
    h: int *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Disables scissor testing.</summary>
  [<Extension>]
  static member inline clearScissor< ^B
    when ^B: (member AddClearScissor: int<RenderLayer> -> unit)> :
    buffer: ^B * [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Sets the line width for subsequent line draws.</summary>
  [<Extension>]
  static member inline setLineWidth< ^B
    when ^B: (member AddSetLineWidth: float32 * int<RenderLayer> -> unit)> :
    buffer: ^B *
    width: float32 *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Sets the viewport rectangle.</summary>
  [<Extension>]
  static member inline setViewport< ^B
    when ^B: (member AddSetViewport:
      int * int * int * int * int<RenderLayer> -> unit)> :
    buffer: ^B *
    x: int *
    y: int *
    w: int *
    h: int *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>
  /// Runs a fully-custom draw. 2D: action is <c>unit -&gt; unit</c> (batch is
  /// flushed, state restored). 3D: action receives the frame's SceneContext.
  /// The 3D witness ignores <paramref name="layer"/> (3D has no layers).
  /// </summary>
  [<Extension>]
  static member inline drawImmediate< ^B, 'Ctx
    when ^B: (member AddDrawImmediate: ('Ctx -> unit) * int<RenderLayer> -> unit)> :
    buffer: ^B *
    [<InlineIfLambda>] action: ('Ctx -> unit) *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Clears the current framebuffer to the given color.</summary>
  [<Extension>]
  static member inline clear< ^B
    when ^B: (member AddClear: Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>
  /// Enqueues a post-process pass. The action receives the backend's
  /// post-process context (2D or 3D) and runs once after the scene renders.
  /// </summary>
  [<Extension>]
  static member inline postProcess< ^B, 'Ctx
    when ^B: (member AddPostProcess: ('Ctx -> unit) -> unit)> :
    buffer: ^B * [<InlineIfLambda>] action: ('Ctx -> unit) -> ^B

  /// <summary>
  /// Enqueues a post-process pass that needs camera-POV scene depth.
  /// <b>3D only</b> — the 2D buffer has no witness.
  /// </summary>
  [<Extension>]
  static member inline postProcessWithDepth< ^B, 'Ctx
    when ^B: (member AddPostProcessWithDepth: ('Ctx -> unit) -> unit)> :
    buffer: ^B * [<InlineIfLambda>] action: ('Ctx -> unit) -> ^B

  /// <summary>Adds a batched particle render command.</summary>
  [<Extension>]
  static member inline particles< ^B, 'T, 'P
    when ^B: (member AddParticles:
      'T * 'P array * int * int<RenderLayer> -> unit)> :
    buffer: ^B *
    texture: 'T *
    particles: 'P array *
    count: int *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Sets the ambient light color for this frame.</summary>
  [<Extension>]
  static member inline setAmbient< ^B, 'C
    when ^B: (member AddSetAmbient: 'C * Color * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    color: Color *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Adds a 2D point light for this frame.</summary>
  [<Extension>]
  static member inline addPointLight< ^B, 'C, 'L
    when ^B: (member AddPointLight: 'C * 'L * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    light: 'L *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Adds a 2D directional light from the backend's light record.</summary>
  [<Extension>]
  static member inline addDirectionalLight< ^B, 'C, 'L
    when ^B: (member AddDirectionalLightState:
      'C * 'L * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    light: 'L *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Adds a 2D directional light from parts.</summary>
  [<Extension>]
  static member inline addDirectionalLight< ^B, 'C
    when ^B: (member AddDirectionalLight:
      'C * Vector2 * Color * float32 * bool * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    direction: Vector2 *
    color: Color *
    [<OptionalArgument; Struct>] intensity: float32 voption *
    [<OptionalArgument; Struct>] castsShadows: bool voption *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Adds a shadow-casting occluder segment for this frame.</summary>
  [<Extension>]
  static member inline addOccluder< ^B, 'C, 'O
    when ^B: (member AddOccluder: 'C * 'O * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    occluder: 'O *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Draws a sprite with the current lighting state.</summary>
  [<Extension>]
  static member inline litSprite< ^B, 'C, 'S
    when ^B: (member AddLitSprite: 'C * 'S -> unit)> :
    buffer: ^B * lightCtx: 'C * sprite: 'S -> ^B

  /// <summary>Draws an animated sprite with the current lighting state.</summary>
  [<Extension>]
  static member inline litAnimatedSprite< ^B, 'C, 'R, 'A
    when ^B: (member AddLitAnimatedSprite:
      'C * 'R * 'A * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    dest: 'R *
    animSprite: 'A *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Ends the current lighting pass; sprites after this point are unlit.</summary>
  [<Extension>]
  static member inline endLighting< ^B, 'C
    when ^B: (member AddEndLighting: 'C * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Enables 2D shadow casting for subsequent draws.</summary>
  [<Extension>]
  static member inline enableShadows< ^B, 'C
    when ^B: (member AddEnableShadows: 'C * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Disables 2D shadow casting for subsequent draws.</summary>
  [<Extension>]
  static member inline disableShadows< ^B, 'C
    when ^B: (member AddDisableShadows: 'C * int<RenderLayer> -> unit)> :
    buffer: ^B *
    lightCtx: 'C *
    [<OptionalArgument; Struct>] layer: int<RenderLayer> voption ->
      ^B

  /// <summary>Draws a mesh (raylib Mesh / MonoGame PrimitiveMesh) with a material.</summary>
  [<Extension>]
  static member inline mesh< ^B, 'M, 'X, 'Mat
    when ^B: (member AddDrawMesh: 'M * 'X * 'Mat -> unit)> :
    buffer: ^B * mesh: 'M * transform: 'X * material: 'Mat -> ^B

  /// <summary>
  /// Draws a slice of a mesh within shared vertex/index buffers (MonoGame
  /// content-pipeline parts). <c>vertexOffset</c>/<c>startIndex</c> default to 0 —
  /// self-contained buffers (procedural primitives, raylib meshes) draw the whole
  /// mesh with the defaults.
  /// </summary>
  /// <remarks>
  /// <b>MonoGame:</b> when the mesh record wraps one part of a shared buffer,
  /// <c>PrimitiveCount</c> must hold that part's triangle count and
  /// <c>Bounds</c> that part's local-space bounding sphere — the draw is sized
  /// by <c>PrimitiveCount</c> and the shadow pass culls by <c>Bounds</c>, both
  /// taken from the record, never from the shared buffer.
  /// </remarks>
  [<Extension>]
  static member inline meshSlice< ^B, 'M, 'X, 'Mat
    when ^B: (member AddDrawMeshSlice: 'M * 'X * 'Mat * int * int -> unit)> :
    buffer: ^B *
    mesh: 'M *
    transform: 'X *
    material: 'Mat *
    [<OptionalArgument; Struct>] vertexOffset: int voption *
    [<OptionalArgument; Struct>] startIndex: int voption ->
      ^B

  /// <summary>Draws many instances of the same mesh. Prefer over repeated Mesh calls.
  /// <paramref name="colors"/> tints each instance (albedo × color.rgb, alpha × color.a);
  /// <b>MonoGame only</b> — the raylib backend raises <see cref="T:System.NotSupportedException"/>
  /// when colors are supplied.</summary>
  [<Extension>]
  static member inline instanced< ^B, 'M, 'X, 'Mat, 'C
    when ^B: (member AddDrawInstanced:
      'M * 'X array * 'Mat * int * 'C array voption -> unit)> :
    buffer: ^B *
    mesh: 'M *
    transforms: 'X array *
    material: 'Mat *
    instanceCount: int *
    [<OptionalArgument; Struct>] colors: 'C array voption ->
      ^B

  /// <summary>
  /// Draws many instances of a slice of a mesh within shared vertex/index buffers
  /// (MonoGame content-pipeline parts). <c>vertexOffset</c>/<c>startIndex</c> default
  /// to 0 — self-contained buffers (procedural primitives, raylib meshes) draw the
  /// whole mesh with the defaults. <paramref name="colors"/> tints each instance
  /// (MonoGame only, see <c>instanced</c>).
  /// </summary>
  /// <remarks>
  /// <b>MonoGame:</b> when the mesh record wraps one part of a shared buffer,
  /// <c>PrimitiveCount</c> must hold that part's triangle count and
  /// <c>Bounds</c> that part's local-space bounding sphere — the draw is sized
  /// by <c>PrimitiveCount</c> and the shadow pass culls by <c>Bounds</c>, both
  /// taken from the record, never from the shared buffer.
  /// </remarks>
  [<Extension>]
  static member inline instancedSlice< ^B, 'M, 'X, 'Mat, 'C
    when ^B: (member AddDrawInstancedSlice:
      'M * 'X array * 'Mat * int * 'C array voption * int * int -> unit)> :
    buffer: ^B *
    mesh: 'M *
    transforms: 'X array *
    material: 'Mat *
    instanceCount: int *
    [<OptionalArgument; Struct>] colors: 'C array voption *
    [<OptionalArgument; Struct>] vertexOffset: int voption *
    [<OptionalArgument; Struct>] startIndex: int voption ->
      ^B

  /// <summary>Draws a model with a world transform and its authored materials.</summary>
  [<Extension>]
  static member inline model< ^B, 'M, 'X
    when ^B: (member AddDrawModel: 'M * 'X -> unit)> :
    buffer: ^B * model: 'M * transform: 'X -> ^B

  /// <summary>Draws a model with a whole-model material override.</summary>
  [<Extension>]
  static member inline modelWith< ^B, 'M, 'X, 'Mat
    when ^B: (member AddDrawModelWith: 'M * 'X * 'Mat -> unit)> :
    buffer: ^B * model: 'M * transform: 'X * material: 'Mat -> ^B

  /// <summary>Draws a model with a per-sub-mesh material resolver.</summary>
  [<Extension>]
  static member inline modelWithPerMesh< ^B, 'M, 'X, 'Mat
    when ^B: (member AddDrawModelWithPerMesh: 'M * 'X * (int -> 'Mat) -> unit)> :
    buffer: ^B *
    model: 'M *
    transform: 'X *
    [<InlineIfLambda>] resolver: (int -> 'Mat) ->
      ^B

  /// <summary>
  /// Draws an animated (skinned) model from the backend's animation state
  /// record. The witness derives the bone palette (MonoGame: from the state;
  /// raylib: applies it to the model).
  /// <paramref name="pose"/> lets the caller share one pose evaluation between
  /// this draw and any number of bone queries / attachment draws
  /// (see <c>attachedMesh</c>). On raylib it is honored by the
  /// <c>AnimatedModel</c> witness (GPU skinning path) and ignored by the legacy
  /// <c>Animation3DState</c> witness (mutating path). When omitted, the witness
  /// computes the pose internally exactly as before.
  /// </summary>
  [<Extension>]
  static member inline animatedModel< ^B, 'A, 'X, 'Pose
    when ^B: (member AddAnimatedModel: 'A * 'X * 'Pose voption -> unit)> :
    buffer: ^B *
    animModel: 'A *
    transform: 'X *
    [<OptionalArgument; Struct>] pose: 'Pose voption ->
      ^B

  /// <summary>Draws an animated model with a whole-model material override.
  /// <paramref name="pose"/> shares one pose evaluation with bone queries and
  /// attachment draws — see <c>animatedModel</c>.</summary>
  [<Extension>]
  static member inline animatedModelWith< ^B, 'A, 'X, 'Mat, 'Pose
    when ^B: (member AddAnimatedModelWith:
      'A * 'X * 'Mat * 'Pose voption -> unit)> :
    buffer: ^B *
    animModel: 'A *
    transform: 'X *
    material: 'Mat *
    [<OptionalArgument; Struct>] pose: 'Pose voption ->
      ^B

  /// <summary>Draws an animated model with a per-sub-mesh material resolver.
  /// <paramref name="pose"/> shares one pose evaluation with bone queries and
  /// attachment draws — see <c>animatedModel</c>.</summary>
  [<Extension>]
  static member inline animatedModelWithPerMesh< ^B, 'A, 'X, 'Mat, 'Pose
    when ^B: (member AddAnimatedModelWithPerMesh:
      'A * 'X * (int -> 'Mat) * 'Pose voption -> unit)> :
    buffer: ^B *
    animModel: 'A *
    transform: 'X *
    [<InlineIfLambda>] resolver: (int -> 'Mat) *
    [<OptionalArgument; Struct>] pose: 'Pose voption ->
      ^B

  /// <summary>
  /// Skinned + instanced: draws <c>min(transforms.Length, poses.Length)</c>
  /// instances of the same animated model in one draw call (per sub-mesh),
  /// each instance with its own world transform and pose.
  /// <paramref name="poses"/> carries one caller-evaluated <c>BonePose</c> per
  /// instance — compute the poses once per frame and share them with bone
  /// queries / attachment draws (see <c>animatedModel</c>).
  /// <paramref name="material"/> overrides the authored materials
  /// (<c>MaterialOverride.All</c> for a whole-model override,
  /// <c>MaterialOverride.PerMesh</c> for a per-sub-mesh resolver);
  /// <paramref name="colors"/> tints each instance (<b>MonoGame only</b> — the
  /// raylib backend raises <see cref="T:System.NotSupportedException"/>).
  /// On the MonoGame OpenGL backend the draw falls back to per-instance
  /// skinned draws (no vertex texture fetch on that shader profile).
  /// </summary>
  [<Extension>]
  static member inline animatedModelInstanced< ^B, 'A, 'X, 'Pose, 'O, 'C
    when ^B: (member AddAnimatedModelInstanced:
      'A * 'X array * 'Pose array * 'O voption * 'C array voption -> unit)> :
    buffer: ^B *
    animModel: 'A *
    transforms: 'X array *
    poses: 'Pose array *
    [<OptionalArgument; Struct>] material: 'O voption *
    [<OptionalArgument; Struct>] colors: 'C array voption ->
      ^B

  /// <summary>
  /// Draws a skinned mesh with an explicit bone palette and material.
  /// <b>raylib only</b> — MonoGame's skinned path goes through AnimatedModel;
  /// use <c>animatedModel(..., pose)</c> for the MonoGame explicit-palette path.
  /// <paramref name="bones"/> carries the palette in plain System.Numerics
  /// row-major layout (<c>bones[i] = InverseBindPose[i] * pose[i]</c>), NOT
  /// pre-transposed — the raylib backend this member serves transposes at
  /// upload where the shader contract needs it.
  /// </summary>
  [<Extension>]
  static member inline skinnedMesh< ^B, 'M, 'X, 'Mat, 'Bones
    when ^B: (member AddSkinnedMesh: 'M * 'X * 'Mat * 'Bones -> unit)> :
    buffer: ^B * mesh: 'M * transform: 'X * material: 'Mat * bones: 'Bones -> ^B

  /// <summary>
  /// Draws a static <paramref name="mesh"/> parented to <paramref name="bone"/>
  /// of the animated model <paramref name="animModel"/>. The attachment's world
  /// transform is <c>localTransform * boneWorld * transform</c> (row-vector
  /// convention): it inherits the instance's full world transform including
  /// scale, and <paramref name="localTransform"/> is the caller's grip
  /// offset/rotation/scale relative to the bone. An unknown bone is a no-op —
  /// no command is emitted. Pass the same <paramref name="pose"/> given to
  /// <c>animatedModel</c> to avoid a second pose evaluation this frame.
  /// </summary>
  /// <remarks>
  /// The attachment mesh's vertices must be in model-root space. On MonoGame,
  /// mesh parts extracted from a content-pipeline <c>Model</c> are bone-local —
  /// bake the part's absolute bone transform (<c>CopyAbsoluteBoneTransformsTo</c>)
  /// into <paramref name="localTransform"/> or the prop renders offset.
  /// </remarks>
  [<Extension>]
  static member inline attachedMesh< ^B, 'A, 'X, 'M, 'Mat, 'Pose
    when ^B: (member AddAttachedMesh:
      'A * BoneRef * 'X * 'M * 'Mat * 'X * 'Pose voption -> unit)> :
    buffer: ^B *
    animModel: 'A *
    bone: BoneRef *
    localTransform: 'X *
    mesh: 'M *
    material: 'Mat *
    transform: 'X *
    [<OptionalArgument; Struct>] pose: 'Pose voption ->
      ^B

  /// <summary>Draws a billboard (camera-facing quad) with a texture.
  /// <paramref name="rotation"/> spins the quad around the view axis, in degrees.
  /// <paramref name="sourceRect"/> selects an atlas/flipbook sub-rectangle in pixels
  /// (an all-zero rect = full texture). <paramref name="blend"/> overrides the blend
  /// mode (default: alpha blend); blended billboards draw in buffer order with no
  /// depth sorting.</summary>
  [<Extension>]
  static member inline billboard< ^B, 'T, 'R, 'Blend
    when ^B: (member AddBillboard:
      'T * Vector3 * Vector2 * Color * float32 * 'R * 'Blend voption -> unit)> :
    buffer: ^B *
    texture: 'T *
    position: Vector3 *
    size: Vector2 *
    color: Color *
    [<OptionalArgument; Struct>] rotation: float32 voption *
    [<OptionalArgument; Struct>] sourceRect: 'R voption *
    [<OptionalArgument; Struct>] blend: 'Blend voption ->
      ^B

  /// <summary>Draws multiple billboards in a single batch. All arrays are
  /// backend handles (XNA arrays on MonoGame) — no per-frame conversion.
  /// <paramref name="rotations"/> and <paramref name="sourceRects"/> are optional
  /// per-item arrays (null or shorter than <paramref name="count"/> = no rotation /
  /// full texture for those items); see <c>billboard</c> for their semantics.
  /// <paramref name="blend"/> overrides the blend mode for the whole batch
  /// (default: alpha blend). On MonoGame the batch draws with <c>textures[0]</c>;
  /// raylib honors per-item textures.</summary>
  [<Extension>]
  static member inline billboardBatch< ^B, 'T, 'P, 'S, 'C, 'R, 'Blend
    when ^B: (member AddBillboardBatch:
      'T array *
      'P array *
      'S array *
      'C array *
      int *
      float32 array *
      'R array *
      'Blend voption ->
        unit)> :
    buffer: ^B *
    textures: 'T array *
    positions: 'P array *
    sizes: 'S array *
    colors: 'C array *
    count: int *
    [<OptionalArgument; Struct>] rotations: float32 array voption *
    [<OptionalArgument; Struct>] sourceRects: 'R array voption *
    [<OptionalArgument; Struct>] blend: 'Blend voption ->
      ^B

  /// <summary>Draws a 3D line between two points.</summary>
  [<Extension>]
  static member inline line3D< ^B
    when ^B: (member AddLine3D: Vector3 * Vector3 * Color -> unit)> :
    buffer: ^B * start: Vector3 * finish: Vector3 * color: Color -> ^B

  /// <summary>Renders a cell grid instanced. If the context was built with the
  /// per-sub-mesh shader overload, each <c>ValueSome</c> sub-mesh is shaded by
  /// its own effect; otherwise the default PBR instanced path is used.</summary>
  [<Extension>]
  static member inline renderCellGridInstanced< ^Ctx, 'Buf, 'T
    when ^Ctx: (member RenderCellGridInstanced: 'Buf * CellGrid3D<'T> -> unit)> :
    buffer: 'Buf * ctx: ^Ctx * grid: CellGrid3D<'T> -> 'Buf

  /// <summary>Renders a cell grid instanced, wrapping each key's draws in an
  /// effect scope when <paramref name="shaderForKey"/> returns <c>ValueSome</c>.</summary>
  [<Extension>]
  static member inline renderCellGridInstanced< ^Ctx, 'Buf, 'T, 'Key, 'S
    when ^Ctx: (member RenderCellGridInstanced:
      'Buf * CellGrid3D<'T> * ('Key -> ValueOption<'S>) -> unit)
    and 'Key: equality> :
    buffer: 'Buf *
    ctx: ^Ctx *
    grid: CellGrid3D<'T> *
    [<InlineIfLambda>] shaderForKey: ('Key -> ValueOption<'S>) ->
      'Buf
      when 'Key: equality

  /// <summary>Like <c>renderCellGridInstanced</c> but restricted to a bounding volume.</summary>
  [<Extension>]
  static member inline renderCellGridVolumeInstanced< ^Ctx, 'Buf, 'T
    when ^Ctx: (member RenderCellGridVolumeInstanced:
      'Buf * BoundingBox * CellGrid3D<'T> -> unit)> :
    buffer: 'Buf * ctx: ^Ctx * bounds: BoundingBox * grid: CellGrid3D<'T> ->
      'Buf

  /// <summary>Like <c>renderCellGridInstanced</c> but restricted to a bounding
  /// volume, with per-key effect scoping.</summary>
  [<Extension>]
  static member inline renderCellGridVolumeInstanced< ^Ctx, 'Buf, 'T, 'Key, 'S
    when ^Ctx: (member RenderCellGridVolumeInstanced:
      'Buf * BoundingBox * CellGrid3D<'T> * ('Key -> ValueOption<'S>) -> unit)
    and 'Key: equality> :
    buffer: 'Buf *
    ctx: ^Ctx *
    bounds: BoundingBox *
    grid: CellGrid3D<'T> *
    [<InlineIfLambda>] shaderForKey: ('Key -> ValueOption<'S>) ->
      'Buf
      when 'Key: equality

  /// <summary>Renders a hex grid instanced. Per-sub-mesh shader scoping applies
  /// when the context was built with the triple overload.</summary>
  [<Extension>]
  static member inline renderHexGridInstanced< ^Ctx, 'Buf, 'T
    when ^Ctx: (member RenderHexGridInstanced: 'Buf * HexGrid3D<'T> -> unit)> :
    buffer: 'Buf * ctx: ^Ctx * grid: HexGrid3D<'T> -> 'Buf

  /// <summary>Renders a hex grid instanced, wrapping each key's draws in an
  /// effect scope when <paramref name="shaderForKey"/> returns <c>ValueSome</c>.</summary>
  [<Extension>]
  static member inline renderHexGridInstanced< ^Ctx, 'Buf, 'T, 'Key, 'S
    when ^Ctx: (member RenderHexGridInstanced:
      'Buf * HexGrid3D<'T> * ('Key -> ValueOption<'S>) -> unit)
    and 'Key: equality> :
    buffer: 'Buf *
    ctx: ^Ctx *
    grid: HexGrid3D<'T> *
    [<InlineIfLambda>] shaderForKey: ('Key -> ValueOption<'S>) ->
      'Buf
      when 'Key: equality

  /// <summary>Like <c>renderHexGridInstanced</c> but restricted to a bounding volume.</summary>
  [<Extension>]
  static member inline renderHexGridVolumeInstanced< ^Ctx, 'Buf, 'T
    when ^Ctx: (member RenderHexGridVolumeInstanced:
      'Buf * BoundingBox * HexGrid3D<'T> -> unit)> :
    buffer: 'Buf * ctx: ^Ctx * bounds: BoundingBox * grid: HexGrid3D<'T> -> 'Buf

  /// <summary>Like <c>renderHexGridInstanced</c> but restricted to a bounding
  /// volume, with per-key effect scoping.</summary>
  [<Extension>]
  static member inline renderHexGridVolumeInstanced< ^Ctx, 'Buf, 'T, 'Key, 'S
    when ^Ctx: (member RenderHexGridVolumeInstanced:
      'Buf * BoundingBox * HexGrid3D<'T> * ('Key -> ValueOption<'S>) -> unit)
    and 'Key: equality> :
    buffer: 'Buf *
    ctx: ^Ctx *
    bounds: BoundingBox *
    grid: HexGrid3D<'T> *
    [<InlineIfLambda>] shaderForKey: ('Key -> ValueOption<'S>) ->
      'Buf
      when 'Key: equality

  /// <summary>Sets the shadow origin for this frame's shadow pass.</summary>
  [<Extension>]
  static member inline setShadowOrigin< ^B
    when ^B: (member AddSetShadowOrigin: Vector3 -> unit)> :
    buffer: ^B * origin: Vector3 -> ^B

  /// <summary>Enables 3D shadow casting for subsequent geometry.</summary>
  [<Extension>]
  static member inline enableShadows< ^B
    when ^B: (member AddEnableShadows3D: unit -> unit)> : buffer: ^B -> ^B

  /// <summary>Disables 3D shadow casting for subsequent geometry.</summary>
  [<Extension>]
  static member inline disableShadows< ^B
    when ^B: (member AddDisableShadows3D: unit -> unit)> : buffer: ^B -> ^B

  /// <summary>
  /// Opens a per-group shading scope: draws until EndEffect are shaded by
  /// <paramref name="shader"/> instead of the default PBR shader, inheriting
  /// the gathered scene data (camera, lights, shadow pass, bones, time).
  /// </summary>
  [<Extension>]
  static member inline beginEffect< ^B, 'S
    when ^B: (member AddBeginEffect: 'S -> unit)> :
    buffer: ^B * shader: 'S -> ^B

  /// <summary>Closes the shading scope opened by BeginEffect.</summary>
  [<Extension>]
  static member inline endEffect< ^B
    when ^B: (member AddEndEffect: unit -> unit)> : buffer: ^B -> ^B

  /// <summary>Sets the ambient light for the scene.</summary>
  [<Extension>]
  static member inline setAmbientLight< ^B
    when ^B: (member AddSetAmbientLight: AmbientLight3D -> unit)> :
    buffer: ^B * light: AmbientLight3D -> ^B

  /// <summary>Adds a directional light to the scene.</summary>
  [<Extension>]
  static member inline addDirectionalLight< ^B
    when ^B: (member AddDirectionalLight: DirectionalLight3D -> unit)> :
    buffer: ^B * light: DirectionalLight3D -> ^B

  /// <summary>Adds a point light to the scene.</summary>
  [<Extension>]
  static member inline addPointLight< ^B
    when ^B: (member AddPointLight: PointLight3D -> unit)> :
    buffer: ^B * light: PointLight3D -> ^B

  /// <summary>Adds a spot light to the scene.</summary>
  [<Extension>]
  static member inline addSpotLight< ^B
    when ^B: (member AddSpotLight: SpotLight3D -> unit)> :
    buffer: ^B * light: SpotLight3D -> ^B

  /// <summary>Terminal function that discards the buffer, silencing the unused-value warning.</summary>
  [<Extension>]
  static member inline drop: buffer: 'B -> unit
