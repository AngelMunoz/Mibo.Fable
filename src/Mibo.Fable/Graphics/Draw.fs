// ─────────────────────────────────────────────────────────────────────────────
// The fluent Draw DSL — one backend-neutral surface for 2D and 3D.
//
// Every member is an [<Extension>] static member inline with SRTP constraints:
// the DSL itself is defined ONCE here in Core, and each backend satisfies the
// constraints with small `member inline` witnesses on its render buffer (see
// Graphics2D/DrawWitnesses.fs and Graphics3D/DrawWitnesses.fs in Mibo.Raylib
// and Mibo.MonoGame). At the call site the whole chain erases to direct
// buffer.Add(...) calls — zero dispatch, zero closure allocation.
//
// Conventions:
//   * Parameters are sorted by common usage; anything with a sensible default
//     is optional (`layer` defaults to 0<RenderLayer>).
//   * Colors are Mibo.Color (backend-neutral; converts at the boundary).
//   * Vectors are System.Numerics (raylib-native; MonoGame converts).
//   * Backend-typed things (textures, fonts, cameras, shaders, models, state
//     records) are generic handles — the witness takes the backend's own type.
//   * Members that exist on only one backend (e.g. SetSamplerState) simply
//     have a witness on one buffer — calling them on the other backend is a
//     compile error naming the missing witness.
//   * Members spanning 2D and 3D (cameras, post-process, DrawImmediate) share
//     one name; the buffer type selects the witness. 3D has no layer concept —
//     its witnesses accept and ignore `layer`.
// ─────────────────────────────────────────────────────────────────────────────
namespace Mibo.Elmish.Graphics

open Mibo.Vectors
open System.Runtime.CompilerServices
open Mibo
open Mibo.Animation
open Mibo.Elmish.Graphics2D
open Mibo.Elmish.Graphics3D
open Mibo.Layout3D

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

type WithEllipses2D<'T
  when 'T: (member AddFillEllipse:
    int * int * float32 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddEllipseOutline:
    int * int * float32 * float32 * Color * int<RenderLayer> -> unit)> = 'T

type WithLines2D<'T
  when 'T: (member AddLine: Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddLineThick:
    Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)
  and 'T: (member AddBezier:
    Vector2 * Vector2 * Vector2 * Color * float32 * int<RenderLayer> -> unit)> =
  'T

type WithPolygons2D<'T
  when 'T: (member AddTriangle:
    Vector2 * Vector2 * Vector2 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddFillPoly:
    Vector2 * int * float32 * float32 * Color * int<RenderLayer> -> unit)
  and 'T: (member AddPolyOutline:
    Vector2 * int * float32 * float32 * Color * float32 * int<RenderLayer> ->
      unit)> = 'T

type WithShapes2D<'T
  when WithRects2D<'T>
  and WithCircles2D<'T>
  and WithRings2D<'T>
  and WithEllipses2D<'T>
  and WithLines2D<'T>
  and WithPolygons2D<'T>> = 'T

[<Extension>]
type Draw =

  // ──────────────────────────────────────────────
  // 2D — Sprites & Text (backend state records, pass-through)
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline sprite<'B, 'S
    when 'B: (member AddSpriteState: 'S -> unit)>
    (buffer: 'B, state: 'S)
    : 'B =
    buffer.AddSpriteState state
    buffer

  [<Extension>]
  static member inline text<'B, 'S when 'B: (member AddTextState: 'S -> unit)>
    (buffer: 'B, state: 'S)
    : 'B =
    buffer.AddTextState state
    buffer

  [<Extension>]
  static member inline text<'B, 'F
    when 'B: (member AddText:
      'F * string * Vector2 * float32 * float32 * Color * int<RenderLayer> ->
        unit)>
    (
      buffer: 'B,
      font: 'F,
      text: string,
      position: Vector2,
      size: float32,
      [<Struct>] ?tint: Color,
      [<Struct>] ?spacing: float32,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddText(
      font,
      text,
      position,
      size,
      defaultValueArg spacing 1.0f,
      defaultValueArg tint Color.White,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  // ──────────────────────────────────────────────
  // 2D — Rectangles
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline fillRect<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: float32,
      y: float32,
      w: float32,
      h: float32,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddFillRect(x, y, w, h, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline rectOutline<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: float32,
      y: float32,
      w: float32,
      h: float32,
      color: Color,
      [<Struct>] ?thickness: float32,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddRectOutline(
      x,
      y,
      w,
      h,
      color,
      defaultValueArg thickness 1.0f,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline fillRectRounded<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: float32,
      y: float32,
      w: float32,
      h: float32,
      color: Color,
      [<Struct>] ?roundness: float32,
      [<Struct>] ?segments: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddFillRectRounded(
      x,
      y,
      w,
      h,
      color,
      defaultValueArg roundness 0.5f,
      defaultValueArg segments 8,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline rectRoundedOutline<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: float32,
      y: float32,
      w: float32,
      h: float32,
      color: Color,
      [<Struct>] ?roundness: float32,
      [<Struct>] ?segments: int,
      [<Struct>] ?thickness: float32,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddRectRoundedOutline(
      x,
      y,
      w,
      h,
      color,
      defaultValueArg roundness 0.5f,
      defaultValueArg segments 8,
      defaultValueArg thickness 1.0f,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline rectGradientV<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: int,
      y: int,
      w: int,
      h: int,
      top: Color,
      bottom: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddRectGradientV(
      x,
      y,
      w,
      h,
      top,
      bottom,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline rectGradientH<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: int,
      y: int,
      w: int,
      h: int,
      left: Color,
      right: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddRectGradientH(
      x,
      y,
      w,
      h,
      left,
      right,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline rectGradient<'B when WithRects2D<'B>>
    (
      buffer: 'B,
      x: float32,
      y: float32,
      w: float32,
      h: float32,
      topLeft: Color,
      bottomLeft: Color,
      topRight: Color,
      bottomRight: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddRectGradient(
      x,
      y,
      w,
      h,
      topLeft,
      bottomLeft,
      topRight,
      bottomRight,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  // ──────────────────────────────────────────────
  // 2D — Circles, Rings, Ellipses
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline fillCircle<'B when WithCircles2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      radius: float32,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddFillCircle(
      center,
      radius,
      color,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline circleOutline<'B when WithCircles2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      radius: float32,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddCircleOutline(
      center,
      radius,
      color,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline circleSector<'B when WithCircles2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      radius: float32,
      startAngle: float32,
      endAngle: float32,
      color: Color,
      [<Struct>] ?segments: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddCircleSector(
      center,
      radius,
      startAngle,
      endAngle,
      color,
      defaultValueArg segments 16,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline circleSectorOutline<'B when WithCircles2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      radius: float32,
      startAngle: float32,
      endAngle: float32,
      color: Color,
      [<Struct>] ?segments: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddCircleSectorOutline(
      center,
      radius,
      startAngle,
      endAngle,
      color,
      defaultValueArg segments 16,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline circleGradient<'B when WithCircles2D<'B>>
    (
      buffer: 'B,
      centerX: int,
      centerY: int,
      radius: float32,
      inner: Color,
      outer: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddCircleGradient(
      centerX,
      centerY,
      radius,
      inner,
      outer,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline fillRing<'B when WithRings2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      innerRadius: float32,
      outerRadius: float32,
      startAngle: float32,
      endAngle: float32,
      color: Color,
      [<Struct>] ?segments: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddFillRing(
      center,
      innerRadius,
      outerRadius,
      startAngle,
      endAngle,
      color,
      defaultValueArg segments 16,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline ringOutline<'B when WithRings2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      innerRadius: float32,
      outerRadius: float32,
      startAngle: float32,
      endAngle: float32,
      color: Color,
      [<Struct>] ?segments: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddRingOutline(
      center,
      innerRadius,
      outerRadius,
      startAngle,
      endAngle,
      color,
      defaultValueArg segments 16,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline fillEllipse<'B when WithEllipses2D<'B>>
    (
      buffer: 'B,
      centerX: int,
      centerY: int,
      radiusH: float32,
      radiusV: float32,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddFillEllipse(
      centerX,
      centerY,
      radiusH,
      radiusV,
      color,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline ellipseOutline<'B when WithEllipses2D<'B>>
    (
      buffer: 'B,
      centerX: int,
      centerY: int,
      radiusH: float32,
      radiusV: float32,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddEllipseOutline(
      centerX,
      centerY,
      radiusH,
      radiusV,
      color,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  // ──────────────────────────────────────────────
  // 2D — Lines & Curves
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline line<'B when WithLines2D<'B>>
    (
      buffer: 'B,
      start: Vector2,
      finish: Vector2,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddLine(start, finish, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline lineThick<'B when WithLines2D<'B>>
    (
      buffer: 'B,
      start: Vector2,
      finish: Vector2,
      color: Color,
      [<Struct>] ?thickness: float32,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddLineThick(
      start,
      finish,
      color,
      defaultValueArg thickness 1.0f,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline lineStrip<'B, 'P
    when 'B: (member AddLineStrip: 'P[] * Color * int<RenderLayer> -> unit)>
    (buffer: 'B, points: 'P[], color: Color, [<Struct>] ?layer: int<RenderLayer>) : 'B =
    buffer.AddLineStrip(points, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline bezier<'B when WithLines2D<'B>>
    (
      buffer: 'B,
      start: Vector2,
      control: Vector2,
      finish: Vector2,
      color: Color,
      [<Struct>] ?thickness: float32,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddBezier(
      start,
      control,
      finish,
      color,
      defaultValueArg thickness 1.0f,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  // ──────────────────────────────────────────────
  // 2D — Triangles & Polygons
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline triangle<'B when WithPolygons2D<'B>>
    (
      buffer: 'B,
      v1: Vector2,
      v2: Vector2,
      v3: Vector2,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddTriangle(v1, v2, v3, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline triangleFan<'B, 'P
    when 'B: (member AddTriangleFan: 'P[] * Color * int<RenderLayer> -> unit)>
    (buffer: 'B, points: 'P[], color: Color, [<Struct>] ?layer: int<RenderLayer>) : 'B =
    buffer.AddTriangleFan(points, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline triangleStrip<'B, 'P
    when 'B: (member AddTriangleStrip: 'P[] * Color * int<RenderLayer> -> unit)>
    (buffer: 'B, points: 'P[], color: Color, [<Struct>] ?layer: int<RenderLayer>) : 'B =
    buffer.AddTriangleStrip(points, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline fillPoly<'B when WithPolygons2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      sides: int,
      radius: float32,
      rotation: float32,
      color: Color,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddFillPoly(
      center,
      sides,
      radius,
      rotation,
      color,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline polyOutline<'B when WithPolygons2D<'B>>
    (
      buffer: 'B,
      center: Vector2,
      sides: int,
      radius: float32,
      rotation: float32,
      color: Color,
      [<Struct>] ?thickness: float32,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddPolyOutline(
      center,
      sides,
      radius,
      rotation,
      color,
      defaultValueArg thickness 1.0f,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  // ──────────────────────────────────────────────
  // Shared — Camera (2D and 3D buffers, same witness names)
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline beginCamera<'B, 'C
    when 'B: (member AddBeginCamera: 'C * int<RenderLayer> -> unit)>
    (buffer: 'B, camera: 'C, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddBeginCamera(camera, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline beginCameraWith<'B, 'C
    when 'B: (member AddBeginCameraConfig: 'C * int<RenderLayer> -> unit)>
    (buffer: 'B, config: 'C, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddBeginCameraConfig(config, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline endCamera<'B
    when 'B: (member AddEndCamera: int<RenderLayer> -> unit)>
    (buffer: 'B, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddEndCamera(defaultValueArg layer 0<RenderLayer>)
    buffer

  // ──────────────────────────────────────────────
  // 2D — Shader, Render Target
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline beginShader<'B, 'S
    when 'B: (member AddBeginShader: 'S * int<RenderLayer> -> unit)>
    (buffer: 'B, shader: 'S, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddBeginShader(shader, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline endShader<'B
    when 'B: (member AddEndShader: int<RenderLayer> -> unit)>
    (buffer: 'B, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddEndShader(defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline beginTarget<'B, 'T
    when 'B: (member AddBeginTarget: 'T * int<RenderLayer> -> unit)>
    (buffer: 'B, target: 'T, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddBeginTarget(target, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline endTarget<'B
    when 'B: (member AddEndTarget: int<RenderLayer> -> unit)>
    (buffer: 'B, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddEndTarget(defaultValueArg layer 0<RenderLayer>)
    buffer

  // ──────────────────────────────────────────────
  // 2D — Render State
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline setBlend<'B, 'M
    when 'B: (member AddSetBlend: 'M * int<RenderLayer> -> unit)>
    (buffer: 'B, mode: 'M, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddSetBlend(mode, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline setSamplerState<'B, 'S
    when 'B: (member AddSamplerState: 'S * int<RenderLayer> -> unit)>
    (buffer: 'B, sampler: 'S, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddSamplerState(sampler, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline setScissor<'B
    when 'B: (member AddSetScissor:
      int * int * int * int * int<RenderLayer> -> unit)>
    (
      buffer: 'B,
      x: int,
      y: int,
      w: int,
      h: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddSetScissor(x, y, w, h, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline clearScissor<'B
    when 'B: (member AddClearScissor: int<RenderLayer> -> unit)>
    (buffer: 'B, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddClearScissor(defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline setLineWidth<'B
    when 'B: (member AddSetLineWidth: float32 * int<RenderLayer> -> unit)>
    (buffer: 'B, width: float32, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddSetLineWidth(width, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline setViewport<'B
    when 'B: (member AddSetViewport:
      int * int * int * int * int<RenderLayer> -> unit)>
    (
      buffer: 'B,
      x: int,
      y: int,
      w: int,
      h: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddSetViewport(x, y, w, h, defaultValueArg layer 0<RenderLayer>)
    buffer

  // ──────────────────────────────────────────────
  // Shared — Escape Hatches
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline drawImmediate<'B, 'Ctx
    when 'B: (member AddDrawImmediate: ('Ctx -> unit) * int<RenderLayer> -> unit)>
    (
      buffer: 'B,
      [<InlineIfLambda>] action: 'Ctx -> unit,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddDrawImmediate(action, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline clear<'B
    when 'B: (member AddClear: Color * int<RenderLayer> -> unit)>
    (buffer: 'B, color: Color, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddClear(color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline postProcess<'B, 'Ctx
    when 'B: (member AddPostProcess: ('Ctx -> unit) -> unit)>
    (buffer: 'B, [<InlineIfLambda>] action: 'Ctx -> unit)
    : 'B =
    buffer.AddPostProcess action
    buffer

  [<Extension>]
  static member inline postProcessWithDepth<'B, 'Ctx
    when 'B: (member AddPostProcessWithDepth: ('Ctx -> unit) -> unit)>
    (buffer: 'B, [<InlineIfLambda>] action: 'Ctx -> unit)
    : 'B =
    buffer.AddPostProcessWithDepth action
    buffer

  // ──────────────────────────────────────────────
  // 2D — Particles
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline particles<'B, 'T, 'P
    when 'B: (member AddParticles: 'T * 'P[] * int * int<RenderLayer> -> unit)>
    (
      buffer: 'B,
      texture: 'T,
      particles: 'P[],
      count: int,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddParticles(
      texture,
      particles,
      count,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  // ──────────────────────────────────────────────
  // 2D — Lighting (context handle + light records, pass-through)
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline setAmbient<'B, 'C
    when 'B: (member AddSetAmbient: 'C * Color * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, color: Color, [<Struct>] ?layer: int<RenderLayer>) : 'B =
    buffer.AddSetAmbient(lightCtx, color, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline addPointLight<'B, 'C, 'L
    when 'B: (member AddPointLight: 'C * 'L * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, light: 'L, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddPointLight(lightCtx, light, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline addDirectionalLight<'B, 'C, 'L
    when 'B: (member AddDirectionalLightState:
      'C * 'L * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, light: 'L, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddDirectionalLightState(
      lightCtx,
      light,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline addDirectionalLight<'B, 'C
    when 'B: (member AddDirectionalLight:
      'C * Vector2 * Color * float32 * bool * int<RenderLayer> -> unit)>
    (
      buffer: 'B,
      lightCtx: 'C,
      direction: Vector2,
      color: Color,
      [<Struct>] ?intensity: float32,
      [<Struct>] ?castsShadows: bool,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddDirectionalLight(
      lightCtx,
      direction,
      color,
      defaultValueArg intensity 1.0f,
      defaultValueArg castsShadows false,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline addOccluder<'B, 'C, 'O
    when 'B: (member AddOccluder: 'C * 'O * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, occluder: 'O, [<Struct>] ?layer: int<RenderLayer>) : 'B =
    buffer.AddOccluder(lightCtx, occluder, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline litSprite<'B, 'C, 'S
    when 'B: (member AddLitSprite: 'C * 'S -> unit)>
    (buffer: 'B, lightCtx: 'C, sprite: 'S)
    : 'B =
    buffer.AddLitSprite(lightCtx, sprite)
    buffer

  [<Extension>]
  static member inline litAnimatedSprite<'B, 'C, 'R, 'A
    when 'B: (member AddLitAnimatedSprite:
      'C * 'R * 'A * int<RenderLayer> -> unit)>
    (
      buffer: 'B,
      lightCtx: 'C,
      dest: 'R,
      animSprite: 'A,
      [<Struct>] ?layer: int<RenderLayer>
    ) : 'B =
    buffer.AddLitAnimatedSprite(
      lightCtx,
      dest,
      animSprite,
      defaultValueArg layer 0<RenderLayer>
    )

    buffer

  [<Extension>]
  static member inline endLighting<'B, 'C
    when 'B: (member AddEndLighting: 'C * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddEndLighting(lightCtx, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline enableShadows<'B, 'C
    when 'B: (member AddEnableShadows: 'C * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddEnableShadows(lightCtx, defaultValueArg layer 0<RenderLayer>)
    buffer

  [<Extension>]
  static member inline disableShadows<'B, 'C
    when 'B: (member AddDisableShadows: 'C * int<RenderLayer> -> unit)>
    (buffer: 'B, lightCtx: 'C, [<Struct>] ?layer: int<RenderLayer>)
    : 'B =
    buffer.AddDisableShadows(lightCtx, defaultValueArg layer 0<RenderLayer>)
    buffer

  // ──────────────────────────────────────────────
  // 3D — Geometry
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline mesh<'B, 'M, 'X, 'Mat
    when 'B: (member AddDrawMesh: 'M * 'X * 'Mat -> unit)>
    (buffer: 'B, mesh: 'M, transform: 'X, material: 'Mat)
    : 'B =
    buffer.AddDrawMesh(mesh, transform, material)
    buffer

  [<Extension>]
  static member inline meshSlice<'B, 'M, 'X, 'Mat
    when 'B: (member AddDrawMeshSlice: 'M * 'X * 'Mat * int * int -> unit)>
    (
      buffer: 'B,
      mesh: 'M,
      transform: 'X,
      material: 'Mat,
      [<Struct>] ?vertexOffset: int,
      [<Struct>] ?startIndex: int
    ) : 'B =
    buffer.AddDrawMeshSlice(
      mesh,
      transform,
      material,
      defaultValueArg vertexOffset 0,
      defaultValueArg startIndex 0
    )

    buffer

  [<Extension>]
  static member inline instanced<'B, 'M, 'X, 'Mat, 'C
    when 'B: (member AddDrawInstanced:
      'M * 'X[] * 'Mat * int * 'C[] voption -> unit)>
    (
      buffer: 'B,
      mesh: 'M,
      transforms: 'X[],
      material: 'Mat,
      instanceCount: int,
      [<Struct>] ?colors: 'C[]
    ) : 'B =
    buffer.AddDrawInstanced(mesh, transforms, material, instanceCount, colors)
    buffer

  [<Extension>]
  static member inline instancedSlice<'B, 'M, 'X, 'Mat, 'C
    when 'B: (member AddDrawInstancedSlice:
      'M * 'X[] * 'Mat * int * 'C[] voption * int * int -> unit)>
    (
      buffer: 'B,
      mesh: 'M,
      transforms: 'X[],
      material: 'Mat,
      instanceCount: int,
      [<Struct>] ?colors: 'C[],
      [<Struct>] ?vertexOffset: int,
      [<Struct>] ?startIndex: int
    ) : 'B =
    buffer.AddDrawInstancedSlice(
      mesh,
      transforms,
      material,
      instanceCount,
      colors,
      defaultValueArg vertexOffset 0,
      defaultValueArg startIndex 0
    )

    buffer

  [<Extension>]
  static member inline model<'B, 'M, 'X
    when 'B: (member AddDrawModel: 'M * 'X -> unit)>
    (buffer: 'B, model: 'M, transform: 'X)
    : 'B =
    buffer.AddDrawModel(model, transform)
    buffer

  [<Extension>]
  static member inline modelWith<'B, 'M, 'X, 'Mat
    when 'B: (member AddDrawModelWith: 'M * 'X * 'Mat -> unit)>
    (buffer: 'B, model: 'M, transform: 'X, material: 'Mat)
    : 'B =
    buffer.AddDrawModelWith(model, transform, material)
    buffer

  [<Extension>]
  static member inline modelWithPerMesh<'B, 'M, 'X, 'Mat
    when 'B: (member AddDrawModelWithPerMesh: 'M * 'X * (int -> 'Mat) -> unit)>
    (
      buffer: 'B,
      model: 'M,
      transform: 'X,
      [<InlineIfLambda>] resolver: int -> 'Mat
    ) : 'B =
    buffer.AddDrawModelWithPerMesh(model, transform, resolver)
    buffer

  [<Extension>]
  static member inline animatedModel<'B, 'A, 'X, 'Pose
    when 'B: (member AddAnimatedModel: 'A * 'X * 'Pose voption -> unit)>
    (buffer: 'B, animModel: 'A, transform: 'X, [<Struct>] ?pose: 'Pose)
    : 'B =
    buffer.AddAnimatedModel(animModel, transform, pose)
    buffer

  [<Extension>]
  static member inline animatedModelWith<'B, 'A, 'X, 'Mat, 'Pose
    when 'B: (member AddAnimatedModelWith:
      'A * 'X * 'Mat * 'Pose voption -> unit)>
    (
      buffer: 'B,
      animModel: 'A,
      transform: 'X,
      material: 'Mat,
      [<Struct>] ?pose: 'Pose
    ) : 'B =
    buffer.AddAnimatedModelWith(animModel, transform, material, pose)

    buffer

  [<Extension>]
  static member inline animatedModelWithPerMesh<'B, 'A, 'X, 'Mat, 'Pose
    when 'B: (member AddAnimatedModelWithPerMesh:
      'A * 'X * (int -> 'Mat) * 'Pose voption -> unit)>
    (
      buffer: 'B,
      animModel: 'A,
      transform: 'X,
      [<InlineIfLambda>] resolver: int -> 'Mat,
      [<Struct>] ?pose: 'Pose
    ) : 'B =
    buffer.AddAnimatedModelWithPerMesh(animModel, transform, resolver, pose)

    buffer

  [<Extension>]
  static member inline animatedModelInstanced<'B, 'A, 'X, 'Pose, 'O, 'C
    when 'B: (member AddAnimatedModelInstanced:
      'A * 'X[] * 'Pose[] * 'O voption * 'C[] voption -> unit)>
    (
      buffer: 'B,
      animModel: 'A,
      transforms: 'X[],
      poses: 'Pose[],
      [<Struct>] ?material: 'O,
      [<Struct>] ?colors: 'C[]
    ) : 'B =
    buffer.AddAnimatedModelInstanced(
      animModel,
      transforms,
      poses,
      material,
      colors
    )

    buffer

  [<Extension>]
  static member inline skinnedMesh<'B, 'M, 'X, 'Mat, 'Bones
    when 'B: (member AddSkinnedMesh: 'M * 'X * 'Mat * 'Bones -> unit)>
    (buffer: 'B, mesh: 'M, transform: 'X, material: 'Mat, bones: 'Bones)
    : 'B =
    buffer.AddSkinnedMesh(mesh, transform, material, bones)
    buffer

  [<Extension>]
  static member inline attachedMesh<'B, 'A, 'X, 'M, 'Mat, 'Pose
    when 'B: (member AddAttachedMesh:
      'A * BoneRef * 'X * 'M * 'Mat * 'X * 'Pose voption -> unit)>
    (
      buffer: 'B,
      animModel: 'A,
      bone: BoneRef,
      localTransform: 'X,
      mesh: 'M,
      material: 'Mat,
      transform: 'X,
      [<Struct>] ?pose: 'Pose
    ) : 'B =
    buffer.AddAttachedMesh(
      animModel,
      bone,
      localTransform,
      mesh,
      material,
      transform,
      pose
    )

    buffer

  [<Extension>]
  static member inline billboard<'B, 'T, 'R, 'Blend
    when 'B: (member AddBillboard:
      'T * Vector3 * Vector2 * Color * float32 * 'R * 'Blend voption -> unit)>
    (
      buffer: 'B,
      texture: 'T,
      position: Vector3,
      size: Vector2,
      color: Color,
      [<Struct>] ?rotation: float32,
      [<Struct>] ?sourceRect: 'R,
      [<Struct>] ?blend: 'Blend
    ) : 'B =
    buffer.AddBillboard(
      texture,
      position,
      size,
      color,
      defaultValueArg rotation 0f,
      defaultValueArg sourceRect (Unchecked.defaultof<'R>),
      blend
    )

    buffer

  [<Extension>]
  static member inline billboardBatch<'B, 'T, 'P, 'S, 'C, 'R, 'Blend
    when 'B: (member AddBillboardBatch:
      'T[] * 'P[] * 'S[] * 'C[] * int * float32[] * 'R[] * 'Blend voption ->
        unit)>
    (
      buffer: 'B,
      textures: 'T[],
      positions: 'P[],
      sizes: 'S[],
      colors: 'C[],
      count: int,
      [<Struct>] ?rotations: float32[],
      [<Struct>] ?sourceRects: 'R[],
      [<Struct>] ?blend: 'Blend
    ) : 'B =
    buffer.AddBillboardBatch(
      textures,
      positions,
      sizes,
      colors,
      count,
      defaultValueArg rotations null,
      defaultValueArg sourceRects null,
      blend
    )

    buffer

  [<Extension>]
  static member inline line3D<'B
    when 'B: (member AddLine3D: Vector3 * Vector3 * Color -> unit)>
    (buffer: 'B, start: Vector3, finish: Vector3, color: Color)
    : 'B =
    buffer.AddLine3D(start, finish, color)
    buffer

  // ──────────────────────────────────────────────
  // 3D — Grid Instancing
  //
  // Renders a Cell/Hex grid with one instanced draw per (key × sub-mesh).
  // The witness is on the context (an opaque backend handle —
  // InstancedRenderContext on both backends), not the buffer: F# SRTP resolves
  // members in the type's own declaration file, and the context is declared in
  // the backend's Layout3D file where it sees the grid types. The context owns
  // the key/material/transform resolvers and pooled storage. Two shader
  // sources, both additive:
  //   * Per-sub-mesh: build the context with the (mesh, material, shader) triple
  //     overload — each ValueSome sub-mesh is wrapped in its own effect scope.
  //   * Per-key / whole-grid: pass shaderForKey. ValueSome wraps the whole key's
  //     draws in one scope; ValueNone falls through to the default PBR path
  //     (whole-grid shading: fun _ -> ValueSome shader).
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline renderCellGridInstanced<'Ctx, 'Buf, 'T
    when 'Ctx: (member RenderCellGridInstanced: 'Buf * CellGrid3D<'T> -> unit)>
    (buffer: 'Buf, ctx: 'Ctx, grid: CellGrid3D<'T>)
    : 'Buf =
    ctx.RenderCellGridInstanced(buffer, grid)
    buffer

  [<Extension>]
  static member inline renderCellGridInstanced<'Ctx, 'Buf, 'T, 'Key, 'S
    when 'Ctx: (member RenderCellGridInstanced:
      'Buf * CellGrid3D<'T> * ('Key -> 'S ValueOption) -> unit)
    and 'Key: equality>
    (
      buffer: 'Buf,
      ctx: 'Ctx,
      grid: CellGrid3D<'T>,
      [<InlineIfLambda>] shaderForKey: 'Key -> 'S ValueOption
    ) : 'Buf =
    ctx.RenderCellGridInstanced(buffer, grid, shaderForKey)
    buffer

  [<Extension>]
  static member inline renderCellGridVolumeInstanced<'Ctx, 'Buf, 'T
    when 'Ctx: (member RenderCellGridVolumeInstanced:
      'Buf * BoundingBox * CellGrid3D<'T> -> unit)>
    (buffer: 'Buf, ctx: 'Ctx, bounds: BoundingBox, grid: CellGrid3D<'T>)
    : 'Buf =
    ctx.RenderCellGridVolumeInstanced(buffer, bounds, grid)
    buffer

  [<Extension>]
  static member inline renderCellGridVolumeInstanced<'Ctx, 'Buf, 'T, 'Key, 'S
    when 'Ctx: (member RenderCellGridVolumeInstanced:
      'Buf * BoundingBox * CellGrid3D<'T> * ('Key -> 'S ValueOption) -> unit)
    and 'Key: equality>
    (
      buffer: 'Buf,
      ctx: 'Ctx,
      bounds: BoundingBox,
      grid: CellGrid3D<'T>,
      [<InlineIfLambda>] shaderForKey: 'Key -> 'S ValueOption
    ) : 'Buf =
    ctx.RenderCellGridVolumeInstanced(buffer, bounds, grid, shaderForKey)
    buffer

  [<Extension>]
  static member inline renderHexGridInstanced<'Ctx, 'Buf, 'T
    when 'Ctx: (member RenderHexGridInstanced: 'Buf * HexGrid3D<'T> -> unit)>
    (buffer: 'Buf, ctx: 'Ctx, grid: HexGrid3D<'T>)
    : 'Buf =
    ctx.RenderHexGridInstanced(buffer, grid)
    buffer

  [<Extension>]
  static member inline renderHexGridInstanced<'Ctx, 'Buf, 'T, 'Key, 'S
    when 'Ctx: (member RenderHexGridInstanced:
      'Buf * HexGrid3D<'T> * ('Key -> 'S ValueOption) -> unit)
    and 'Key: equality>
    (
      buffer: 'Buf,
      ctx: 'Ctx,
      grid: HexGrid3D<'T>,
      [<InlineIfLambda>] shaderForKey: 'Key -> 'S ValueOption
    ) : 'Buf =
    ctx.RenderHexGridInstanced(buffer, grid, shaderForKey)
    buffer

  [<Extension>]
  static member inline renderHexGridVolumeInstanced<'Ctx, 'Buf, 'T
    when 'Ctx: (member RenderHexGridVolumeInstanced:
      'Buf * BoundingBox * HexGrid3D<'T> -> unit)>
    (buffer: 'Buf, ctx: 'Ctx, bounds: BoundingBox, grid: HexGrid3D<'T>)
    : 'Buf =
    ctx.RenderHexGridVolumeInstanced(buffer, bounds, grid)
    buffer

  [<Extension>]
  static member inline renderHexGridVolumeInstanced<'Ctx, 'Buf, 'T, 'Key, 'S
    when 'Ctx: (member RenderHexGridVolumeInstanced:
      'Buf * BoundingBox * HexGrid3D<'T> * ('Key -> 'S ValueOption) -> unit)
    and 'Key: equality>
    (
      buffer: 'Buf,
      ctx: 'Ctx,
      bounds: BoundingBox,
      grid: HexGrid3D<'T>,
      [<InlineIfLambda>] shaderForKey: 'Key -> 'S ValueOption
    ) : 'Buf =
    ctx.RenderHexGridVolumeInstanced(buffer, bounds, grid, shaderForKey)
    buffer

  // ──────────────────────────────────────────────
  // 3D — Shadows & Effect Scopes
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline setShadowOrigin<'B
    when 'B: (member AddSetShadowOrigin: Vector3 -> unit)>
    (buffer: 'B, origin: Vector3)
    : 'B =
    buffer.AddSetShadowOrigin origin
    buffer

  [<Extension>]
  static member inline enableShadows<'B
    when 'B: (member AddEnableShadows3D: unit -> unit)>
    (buffer: 'B)
    : 'B =
    buffer.AddEnableShadows3D()
    buffer

  [<Extension>]
  static member inline disableShadows<'B
    when 'B: (member AddDisableShadows3D: unit -> unit)>
    (buffer: 'B)
    : 'B =
    buffer.AddDisableShadows3D()
    buffer

  [<Extension>]
  static member inline beginEffect<'B, 'S
    when 'B: (member AddBeginEffect: 'S -> unit)>
    (buffer: 'B, shader: 'S)
    : 'B =
    buffer.AddBeginEffect shader
    buffer

  [<Extension>]
  static member inline endEffect<'B when 'B: (member AddEndEffect: unit -> unit)>
    (buffer: 'B)
    : 'B =
    buffer.AddEndEffect()
    buffer

  // ──────────────────────────────────────────────
  // 3D — Lights (backend-neutral Core types)
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline setAmbientLight<'B
    when 'B: (member AddSetAmbientLight: AmbientLight3D -> unit)>
    (buffer: 'B, light: AmbientLight3D)
    : 'B =
    buffer.AddSetAmbientLight light
    buffer

  [<Extension>]
  static member inline addDirectionalLight<'B
    when 'B: (member AddDirectionalLight: DirectionalLight3D -> unit)>
    (buffer: 'B, light: DirectionalLight3D)
    : 'B =
    buffer.AddDirectionalLight light
    buffer

  [<Extension>]
  static member inline addPointLight<'B
    when 'B: (member AddPointLight: PointLight3D -> unit)>
    (buffer: 'B, light: PointLight3D)
    : 'B =
    buffer.AddPointLight light
    buffer

  [<Extension>]
  static member inline addSpotLight<'B
    when 'B: (member AddSpotLight: SpotLight3D -> unit)>
    (buffer: 'B, light: SpotLight3D)
    : 'B =
    buffer.AddSpotLight light
    buffer

  // ──────────────────────────────────────────────
  // Terminal
  // ──────────────────────────────────────────────

  [<Extension>]
  static member inline drop<'B>(buffer: 'B) : unit = ()
