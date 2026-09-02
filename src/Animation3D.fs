namespace Mibo.Animation

open System
open System.Collections.Generic

// ─────────────────────────────────────────────────────────────────────────────
// Backend-neutral 3D skeletal animation state machine.
//
// The playback clock (frame advance, blend progress, loop wrap) operates on
// ints and floats only — it never touches bone data or native backend types.
// This module is the single implementation shared by all backends.
//
// Each backend loads its own clip data (raylib's ModelAnimation[], MonoGame's
// Animation3DClip[]) and builds an Animation3DClipsInfo from it at load time.
// The backend then uses this state machine to drive playback, and does its own
// bone-matrix computation / model mutation at render time.
// ─────────────────────────────────────────────────────────────────────────────

[<Struct>]
type Animation3DClipsInfo = {
  ClipNames: IReadOnlyDictionary<string, int>

  ClipNamesByIndex: string[]

  KeyFrameCounts: int[]
}

[<RequireQualifiedAccess; Struct>]
type BoneRef =
  | ByName of name: string
  | ByIndex of index: int

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
