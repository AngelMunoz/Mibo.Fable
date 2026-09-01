module Mibo.Vectors

[<Struct>]
type Vector2 =
  new: x: float32 * y: float32 -> Vector2
  val X: float32
  val Y: float32
  static member Zero: Vector2
  static member One: Vector2
  static member UnitX: Vector2
  static member UnitY: Vector2
  static member (+): a: Vector2 * b: Vector2 -> Vector2
  static member (-): a: Vector2 * b: Vector2 -> Vector2
  static member (*): a: Vector2 * s: float32 -> Vector2
  override ToString: unit -> string
  member ToString2: unit -> string

[<Struct>]
type Vector3 =
  new: x: float32 * y: float32 * z: float32 -> Vector3
  val X: float32
  val Y: float32
  val Z: float32
  static member Zero: Vector3
  static member One: Vector3
  static member UnitX: Vector3
  static member UnitY: Vector3
  static member UnitZ: Vector3
  static member (+): a: Vector3 * b: Vector3 -> Vector3
  static member (-): a: Vector3 * b: Vector3 -> Vector3
  static member (*): a: Vector3 * s: float32 -> Vector3

[<Struct>]
type Vector4 =
  new: x: float32 * y: float32 * z: float32 * w: float32 -> Vector4

  val X: float32
  val Y: float32
  val Z: float32
  val W: float32
  static member Zero: Vector4
  static member One: Vector4
