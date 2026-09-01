module Mibo.Vectors

// Own vector types replacing System.Numerics for the Fable/JS build.
// Plain structs: compile to simple JS objects with X/Y/Z/W fields.

[<Struct>]
type Vector2 =
  new(x: float32, y: float32) = { X = x; Y = y }
  val X: float32
  val Y: float32
  static member Zero = Vector2(0f, 0f)
  static member One = Vector2(1f, 1f)
  static member UnitX = Vector2(1f, 0f)
  static member UnitY = Vector2(0f, 1f)
  static member (+) (a: Vector2, b: Vector2) = Vector2(a.X + b.X, a.Y + b.Y)
  static member (-) (a: Vector2, b: Vector2) = Vector2(a.X - b.X, a.Y - b.Y)
  static member (*) (a: Vector2, s: float32) = Vector2(a.X * s, a.Y * s)
  override v.ToString() = "(" + string v.X + ", " + string v.Y + ")"
  member v.ToString2() = $"({v.X}, {v.Y})"

[<Struct>]
type Vector3 =
  new(x: float32, y: float32, z: float32) = { X = x; Y = y; Z = z }
  val X: float32
  val Y: float32
  val Z: float32
  static member Zero = Vector3(0f, 0f, 0f)
  static member One = Vector3(1f, 1f, 1f)
  static member UnitX = Vector3(1f, 0f, 0f)
  static member UnitY = Vector3(0f, 1f, 0f)
  static member UnitZ = Vector3(0f, 0f, 1f)
  static member (+) (a: Vector3, b: Vector3) = Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z)
  static member (-) (a: Vector3, b: Vector3) = Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z)
  static member (*) (a: Vector3, s: float32) = Vector3(a.X * s, a.Y * s, a.Z * s)

[<Struct>]
type Vector4 =
  new(x: float32, y: float32, z: float32, w: float32) = { X = x; Y = y; Z = z; W = w }
  val X: float32
  val Y: float32
  val Z: float32
  val W: float32
  static member Zero = Vector4(0f, 0f, 0f, 0f)
  static member One = Vector4(1f, 1f, 1f, 1f)
