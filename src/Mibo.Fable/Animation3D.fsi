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
