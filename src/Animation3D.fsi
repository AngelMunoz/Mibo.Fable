namespace Mibo.Animation

open System
open System.Collections.Generic

/// <summary>
/// Backend-neutral animation clip metadata. Carries only what the playback clock
/// needs: clip names and their keyframe counts.
/// </summary>
/// <remarks>
/// Build this from your backend's loaded clip data at load time. The backend
/// keeps its own rich clip objects alongside for bone sampling; this struct is
/// the shareable subset the state machine reads.
/// </remarks>
[<NoComparison>]
[<Struct>]
type Animation3DClipsInfo = {
  /// <summary>Map of clip name to index (resolved at load time for zero-allocation playback).</summary>
  ClipNames: IReadOnlyDictionary<string, int>

  /// <summary>Clip names indexed by clip index (reverse lookup for currentClipName).</summary>
  ClipNamesByIndex: string[]

  /// <summary>Keyframe count per clip, indexed by clip index. Read by the clock loop each frame.</summary>
  KeyFrameCounts: int[]
}

/// <summary>
/// Addresses a bone of an animated model for pose queries and attachment draws.
/// <c>ByName</c> is the authoring-friendly path (resolved through the mesh's
/// name→index lookup); <c>ByIndex</c> is the fast path (no lookup).
/// A missing bone is never an error: queries return <c>ValueNone</c> and
/// attachment draws emit no command.
/// </summary>
[<RequireQualifiedAccess; Struct>]
type BoneRef =
  /// <summary>Look the bone up by its authored name (e.g. "Hand_R").</summary>
  | ByName of name: string
  /// <summary>Address the bone directly by index (zero lookup cost).</summary>
  | ByIndex of index: int

/// <summary>
/// Runtime state for a playing 3D skeletal animation. Pure value — no backend
/// types. Each entity that needs independent animation must have its own copy.
/// </summary>
[<NoComparison>]
[<Struct>]
type Animation3DState = {
  Clips: Animation3DClipsInfo
  CurrentClipIndex: int
  CurrentFrame: float32
  Speed: float32
  Loop: bool
  Finished: bool
  BlendTargetIndex: int
  BlendTargetFrame: float32
  BlendProgress: float32
  BlendDuration: float32
}

module Animation3DClipsInfo =
  /// Builds clip info from (name, keyFrameCount) pairs.
  val create: clipNames: (string * int)[] -> Animation3DClipsInfo

  /// Looks up a clip index by name.
  val inline tryGetClipIndex:
    name: string -> clips: Animation3DClipsInfo -> int voption

  /// All clip names in definition order.
  val names: clips: Animation3DClipsInfo -> string[]
  /// Number of clips.
  val inline count: clips: Animation3DClipsInfo -> int
  /// True when no clips are defined.
  val inline isEmpty: clips: Animation3DClipsInfo -> bool

module Animation3DState =
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
  val update:
    deltaSeconds: float32 -> state: Animation3DState -> Animation3DState

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
