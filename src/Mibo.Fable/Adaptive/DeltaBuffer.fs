namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

/// <summary>One reusable array plus count. Node-owned; grows amortized.</summary>
type internal DeltaBuffer<'T> = internal {
  mutable Items: 'T[]
  mutable Count: int
} with

  member this.IsEmpty = this.Count = 0

  member this.Clear() = this.Count <- 0

  /// Grow the array to hold at least n items. Amortized O(1); array growth only.
  member this.EnsureCapacity(n: int) : unit =
    if this.Items.Length < n then
      let next = Array.zeroCreate(max n (this.Items.Length * 2))
      Array.blit this.Items 0 next 0 this.Items.Length
      this.Items <- next

  /// Append one item to the buffer.
  member this.Append(item: 'T) : unit =
    this.EnsureCapacity(this.Count + 1)
    this.Items[this.Count] <- item
    this.Count <- this.Count + 1

  /// Append many items from a source array.
  member this.AppendRange(items: 'T[], count: int) : unit =
    this.EnsureCapacity(this.Count + count)
    Array.blit items 0 this.Items this.Count count
    this.Count <- this.Count + count

  /// Drop the entries whose key appears in <paramref name="keys" />,
  /// preserving order (shift-compact, one pass). Linear scan for small
  /// products (zero allocation); a hash set above the threshold.
  member this.RemoveKeys
    (keyOfItem: 'T -> 'K, keyOfKey: 'S -> 'K, keys: 'S[], keyCount: int)
    : unit =
    if keyCount > 0 && this.Count > 0 then
      let comparer = EqualityComparer<'K>.Default
      let mutable w = 0

      if int64 keyCount * int64 this.Count <= 4096L then
        for r in 0 .. this.Count - 1 do
          let item = this.Items[r]
          let key = keyOfItem item
          let mutable found = false
          let mutable j = 0

          while not found && j < keyCount do
            if comparer.Equals(key, keyOfKey keys[j]) then
              found <- true

            j <- j + 1

          if not found then
            this.Items[w] <- item
            w <- w + 1
      else
        let keySet = HashSet<'K>()

        for j in 0 .. keyCount - 1 do
          keySet.Add(keyOfKey keys[j]) |> ignore

        for r in 0 .. this.Count - 1 do
          let item = this.Items[r]

          if not(keySet.Contains(keyOfItem item)) then
            this.Items[w] <- item
            w <- w + 1

      this.Count <- w

  /// Compact in place: drop the first <c>doneCount</c> entries, keeping any
  /// entries appended after the captured start (reentrant writes survive).
  member this.Compact(doneCount: int) : unit =
    let live = this.Count

    if live > doneCount then
      Array.blit this.Items doneCount this.Items 0 (live - doneCount)
      this.Count <- live - doneCount
    else
      this.Count <- 0

module internal DeltaBuffer =
  let inline create<'T>() : DeltaBuffer<'T> = {
    Items = Array.zeroCreate 16
    Count = 0
  }

  /// Point-in-time copy: shares the buffer array, copies the count (the
  /// .NET struct-copy semantics of the original).
  let inline copy<'T>(source: DeltaBuffer<'T>) : DeltaBuffer<'T> = {
    Items = source.Items
    Count = source.Count
  }
