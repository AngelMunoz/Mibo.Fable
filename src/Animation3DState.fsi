/// <summary>Pure playback functions for <see cref="T:Mibo.Animation.Animation3DState"/>.</summary>
module Mibo.Animation.Animation3DState

/// Starts a clip by name at its first frame with the given frames-per-second.
val create:
  clips: Animation3DClipsInfo ->
  clipName: string ->
  fps: float32 ->
    Animation3DState

/// Starts a clip by index at its first frame with the given frames-per-second.
val createByIndex:
  clips: Animation3DClipsInfo ->
  clipIndex: int ->
  fps: float32 ->
    Animation3DState

/// Starts a clip by name; keeps playing when it is already the current clip.
val play: clipName: string -> state: Animation3DState -> Animation3DState
/// Starts a clip by index; keeps playing when it is already the current clip.
val playByIndex: clipIndex: int -> state: Animation3DState -> Animation3DState
/// Starts a clip by name only when it differs from the current clip.
val playIfNot: clipName: string -> state: Animation3DState -> Animation3DState

/// Blends from the current clip into the named clip over the given duration.
val blendTo:
  clipName: string ->
  duration: float32 ->
  state: Animation3DState ->
    Animation3DState

/// Blends from the current clip into the indexed clip over the given duration.
val blendToByIndex:
  clipIndex: int ->
  duration: float32 ->
  state: Animation3DState ->
    Animation3DState

/// True while a blend is in progress.
val inline isBlending: state: Animation3DState -> bool
/// Restarts the current clip at its first frame.
val restart: state: Animation3DState -> Animation3DState

/// Advances the animation by deltaSeconds.
val update: deltaSeconds: float32 -> state: Animation3DState -> Animation3DState

/// True when the current clip finished (non-looping).
val inline isFinished: state: Animation3DState -> bool
/// True when the named clip is the current clip and has not finished.
val isPlaying: clipName: string -> state: Animation3DState -> bool
/// Duration of the current clip in seconds.
val inline duration: state: Animation3DState -> float32
/// Name of the current clip.
val currentClipName: state: Animation3DState -> string

/// Returns the state with a new playback speed.
val inline withSpeed:
  speed: float32 -> state: Animation3DState -> Animation3DState

/// Returns the state with looping on or off.
val inline withLoop: loop: bool -> state: Animation3DState -> Animation3DState
