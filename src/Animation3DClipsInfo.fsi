/// <summary>Functions for building and querying <see cref="T:Mibo.Animation.Animation3DClipsInfo"/>.</summary>
module Mibo.Animation.Animation3DClipsInfo

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
