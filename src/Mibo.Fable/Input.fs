namespace Mibo.Input

open System
open Mibo.Vectors

// ─────────────────────────────────────────────────────────────────────────────
// Backend-neutral logical input codes.
//
// These are struct DUs (not enums) on purpose:
//  - exhaustive pattern matching in user code (the compiler proves completeness)
//  - no invalid values at runtime (unlike enums, which can hold any int)
//  - closed type: a third backend (SDL, Silk, ...) can be added without changing
//    these types — it just translates its native codes to/from these cases.
//
// Each DU has an `Unknown` case: a backend maps any native input it does not
// recognize to `Unknown` rather than fabricating a value. Note that the default
// raylib polling implementation filters out `Unknown` values at the polling
// boundary to avoid flooding the input queue with indistinguishable events.
// ─────────────────────────────────────────────────────────────────────────────

[<Struct>]
[<RequireQualifiedAccess>]
type KeyCode =
  // Letters
  | A
  | B
  | C
  | D
  | E
  | F
  | G
  | H
  | I
  | J
  | K
  | L
  | M
  | N
  | O
  | P
  | Q
  | R
  | S
  | T
  | U
  | V
  | W
  | X
  | Y
  | Z
  // Digits (top row)
  | D0
  | D1
  | D2
  | D3
  | D4
  | D5
  | D6
  | D7
  | D8
  | D9
  // Function keys
  | F1
  | F2
  | F3
  | F4
  | F5
  | F6
  | F7
  | F8
  | F9
  | F10
  | F11
  | F12
  // Arrow / navigation cluster
  | Up
  | Down
  | Left
  | Right
  | PageUp
  | PageDown
  | Home
  | End
  | Insert
  | Delete
  // Editing / control
  | Space
  | Enter
  | Escape
  | Tab
  | Backspace
  | CapsLock
  // Modifiers
  | LeftShift
  | RightShift
  | LeftControl
  | RightControl
  | LeftAlt
  | RightAlt
  | LeftSuper
  | RightSuper
  // Punctuation / symbol keys (US layout)
  | Grave
  | Minus
  | Equal
  | LeftBracket
  | RightBracket
  | Backslash
  | Semicolon
  | Apostrophe
  | Comma
  | Period
  | Slash
  // Keypad
  | Kp0
  | Kp1
  | Kp2
  | Kp3
  | Kp4
  | Kp5
  | Kp6
  | Kp7
  | Kp8
  | Kp9
  | KpDecimal
  | KpDivide
  | KpMultiply
  | KpSubtract
  | KpAdd
  | KpEnter
  | KpEqual
  // Media / system
  | PrintScreen
  | ScrollLock
  | Pause
  | Menu
  // Locks / indicators
  | NumLock
  // Fallback
  | Unknown

[<Struct>]
[<RequireQualifiedAccess>]
type MouseButtonCode =
  | Left
  | Right
  | Middle
  | Extra1
  | Extra2
  | Extra3
  | Extra4
  | Unknown

[<Struct>]
[<RequireQualifiedAccess>]
type GamepadButtonCode =
  // Face buttons (backend maps to its own up/right/down/left convention)
  | FaceUp
  | FaceRight
  | FaceDown
  | FaceLeft
  // Shoulder / bumper
  | LeftShoulder
  | RightShoulder
  // Triggers are exposed as analog values (see GamepadAnalog); some backends
  // also report a digital trigger press, so we include them here.
  | LeftTrigger
  | RightTrigger
  // Stick clicks
  | LeftStick
  | RightStick
  // Select / Start / Home
  | Select
  | Start
  | Home
  // D-pad
  | DPadUp
  | DPadRight
  | DPadDown
  | DPadLeft
  | Unknown

[<Struct>]
[<RequireQualifiedAccess>]
type GestureKind =
  | Tap
  | DoubleTap
  | Hold
  | Drag
  | SwipeRight
  | SwipeLeft
  | SwipeUp
  | SwipeDown
  | Pinch
  | Unknown

// ─────────────────────────────────────────────────────────────────────────────
// Delta types (backend-neutral: use the codes above + System.Numerics vectors).
// ─────────────────────────────────────────────────────────────────────────────

[<Struct>]
type KeyboardDelta = {
  Pressed: KeyCode[]
  Released: KeyCode[]
}

[<Struct>]
type MouseButtons = {
  Pressed: MouseButtonCode[]
  Released: MouseButtonCode[]
}

[<Struct>]
type MouseDelta = {
  Position: Vector2
  PositionDelta: Vector2
  Buttons: MouseButtons
  ScrollDelta: float32
  ScrollDeltaV: Vector2
}

[<Struct>]
[<RequireQualifiedAccess>]
type TouchState =
  | Pressed
  | Moved
  | Released

[<Struct>]
type TouchPoint = {
  Id: int
  Position: Vector2
  State: TouchState
}

[<Struct>]
type TouchDelta = { Touches: TouchPoint[] }

[<Struct>]
type GamepadButtons = {
  Pressed: GamepadButtonCode[]
  Released: GamepadButtonCode[]
}

[<Struct>]
type GamepadAnalog = {
  LeftThumbstick: Vector2
  RightThumbstick: Vector2
  LeftTrigger: float32
  RightTrigger: float32
}

[<Struct>]
type GamepadDelta = {
  PlayerIndex: int
  Buttons: GamepadButtons
  Analog: GamepadAnalog
}

[<Struct>]
type GamepadConnection = { PlayerIndex: int; IsConnected: bool }

[<Struct>]
type GestureDelta = {
  Gesture: GestureKind
  HoldDuration: float32
  DragVector: Vector2
  DragAngle: float32
  PinchVector: Vector2
  PinchAngle: float32
}

// ─────────────────────────────────────────────────────────────────────────────
// IInput contract (lives in Core; backend supplies the implementation).
// ─────────────────────────────────────────────────────────────────────────────

[<RequireQualifiedAccess>]
type MouseCapture =
  | Free
  | Captured

type IInput =
  abstract Poll: unit -> unit
  abstract SetMouseCapture: MouseCapture -> unit
  abstract KeyboardDelta: IObservable<KeyboardDelta>
  abstract MouseDelta: IObservable<MouseDelta>
  abstract TouchDelta: IObservable<TouchDelta>
  abstract GamepadDelta: IObservable<GamepadDelta>
  abstract GamepadConnection: IObservable<GamepadConnection>
  abstract GestureDelta: IObservable<GestureDelta>
