namespace Mibo.Fable.Phaser

open Fable.Core
open Fable.Core.JsInterop

// Minimal hand-written Phaser 4 bindings. The full phaser.d.ts counts about
// 149k lines; this surface grows per need instead of mapping the whole file.

[<AllowNullLiteral>]
type InputPlugin =
  abstract on: event: string * fn: obj -> unit

[<AllowNullLiteral>]
type Graphics =
  abstract fillStyle: color: int * ?alpha: float -> Graphics

  abstract fillRect:
    x: float * y: float * width: float * height: float -> Graphics

  abstract fillCircle: x: float * y: float * radius: float -> Graphics
  abstract setPosition: x: float * y: float -> Graphics

[<AllowNullLiteral>]
type Text =
  abstract setText: text: string -> Text

[<AllowNullLiteral>]
type GameObjectFactory =
  abstract graphics: ?config: obj -> Graphics
  abstract text: x: float * y: float * text: string * ?style: obj -> Text

[<AbstractClass>]
[<AllowNullLiteral>]
[<Import("Scene", "phaser")>]
type Scene(?config: obj) =
  member _.game: obj = jsNative
  /// The ScenePlugin of this scene (start/launch/stop other scenes).
  member _.scene: obj = jsNative
  abstract add: GameObjectFactory
  default _.add = jsNative
  abstract input: InputPlugin
  default _.input = jsNative
  abstract preload: unit -> unit
  default _.preload() = ()
  abstract create: unit -> unit
  default _.create() = ()
  abstract update: time: float * delta: float -> unit
  default _.update(_time: float, _delta: float) = ()

[<AllowNullLiteral>]
[<Import("Game", "phaser")>]
type Game(config: obj) =
  member _.destroy(?removeCanvas: bool) : unit = jsNative

module Constants =
  let AUTO: int = import "AUTO" "phaser"
  let CANVAS: int = import "CANVAS" "phaser"
