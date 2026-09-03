namespace Mibo.Fable.Adaptive

open System.Collections.Generic

/// <summary>
/// A set with per-element reference counts: two source elements can map onto one
/// output element, and the output element disappears only when the last source
/// reference disappears.
/// </summary>
type internal RefCountedSet<'T when 'T: equality> = internal {
  Data: HashSet<'T>
  Refcounts: Dictionary<'T, int>
} with

  /// Add one reference. Returns whether the element is newly present.
  member this.Add(item: 'T) : bool =
    let mutable n = 0

    if this.Refcounts.TryGetValue(item, &n) then
      this.Refcounts[item] <- n + 1
      false
    else
      this.Refcounts[item] <- 1
      this.Data.Add item

  /// Remove one reference. Returns whether the element is fully removed.
  member this.Remove(item: 'T) : bool =
    let mutable n = 0

    if this.Refcounts.TryGetValue(item, &n) then
      if n = 1 then
        this.Refcounts.Remove item |> ignore
        this.Data.Remove item
      else
        this.Refcounts[item] <- n - 1
        false
    else
      false

module internal RefCountedSet =
  let inline create<'T when 'T: equality>() : RefCountedSet<'T> = {
    Data = HashSet<'T>()
    Refcounts = Dictionary<'T, int>()
  }
