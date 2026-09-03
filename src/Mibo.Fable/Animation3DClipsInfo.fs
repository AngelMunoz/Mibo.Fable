[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mibo.Animation.Animation3DClipsInfo

open System.Collections.Generic

/// <summary>Build clip info from parallel name and keyframe-count arrays.</summary>
let create(clipNames: (string * int)[]) : Animation3DClipsInfo =
  let dict = Dictionary<string, int>(clipNames.Length)
  let namesByIndex = Array.zeroCreate<string> clipNames.Length
  let counts = Array.zeroCreate<int> clipNames.Length

  for i = 0 to clipNames.Length - 1 do
    let name, count = clipNames[i]
    dict[name] <- i
    namesByIndex[i] <- name
    counts[i] <- count

  {
    ClipNames = dict
    ClipNamesByIndex = namesByIndex
    KeyFrameCounts = counts
  }

/// <summary>Try to get the index for an animation name.</summary>
let inline tryGetClipIndex
  (name: string)
  (clips: Animation3DClipsInfo)
  : int voption =
  match clips.ClipNames.TryGetValue(name) with
  | true, idx -> ValueSome idx
  | _ -> ValueNone

/// <summary>Get the list of animation names.</summary>
let names(clips: Animation3DClipsInfo) : string[] =
  clips.ClipNames.Keys |> Seq.toArray

/// <summary>Get the number of animation clips.</summary>
let inline count(clips: Animation3DClipsInfo) : int =
  clips.KeyFrameCounts.Length

/// <summary>Check if the clip set is empty.</summary>
let inline isEmpty(clips: Animation3DClipsInfo) : bool =
  clips.KeyFrameCounts.Length = 0
