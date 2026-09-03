namespace Mibo.Fable.Adaptive

/// <summary>One reusable array plus count. Node-owned; grows amortized.</summary>
type internal DeltaBuffer<'T> = internal {
  mutable Items: 'T[]
  mutable Count: int
} with

  member IsEmpty: bool
  member Clear: unit -> unit
  /// Grow the array to hold at least n items. Amortized O(1); array growth only.
  member EnsureCapacity: n: int -> unit
  /// Append one item to the buffer.
  member Append: item: 'T -> unit
  /// Append many items from a source array.
  member AppendRange: items: 'T[] * count: int -> unit

  /// Drop the entries whose key appears in <paramref name="keys" />,
  /// preserving order (shift-compact, one pass). Linear scan for small
  /// products (zero allocation); a hash set above the threshold.
  member RemoveKeys:
    keyOfItem: ('T -> 'K) * keyOfKey: ('S -> 'K) * keys: 'S[] * keyCount: int ->
      unit

  /// Compact in place: drop the first <c>doneCount</c> entries, keeping any
  /// entries appended after the captured start (reentrant writes survive).
  member Compact: doneCount: int -> unit

module internal DeltaBuffer =
  val inline create<'T> : unit -> DeltaBuffer<'T>
  /// Point-in-time copy: shares the buffer array, copies the count (the
  /// .NET struct-copy semantics of the original).
  val inline copy<'T> : source: DeltaBuffer<'T> -> DeltaBuffer<'T>
