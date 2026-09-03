/// <summary>Operations on changeable sets.</summary>
module Mibo.Fable.Adaptive.CSet

open System
open System.Collections.Generic

/// <summary>An empty changeable set.</summary>
val inline empty<'T> : cset<'T>
/// <summary>A changeable set with the given items.</summary>
val inline ofSeq: items: seq<'T> -> cset<'T>
/// <summary>Adds an element. No-op when already present.</summary>
val inline add: item: 'T -> set: cset<'T> -> unit
/// <summary>Removes an element. No-op when absent.</summary>
val inline remove: item: 'T -> set: cset<'T> -> unit
/// <summary>Posts an add (the cval.Post handoff pattern).</summary>
val inline postAdd: item: 'T -> set: cset<'T> -> unit
/// <summary>Posts a remove.</summary>
val inline postRemove: item: 'T -> set: cset<'T> -> unit
/// <summary>Posts a full replace (supersedes the other ops of the same pending batch).</summary>
val inline postSet: value: Set<'T> -> set: cset<'T> -> unit
/// <summary>Replaces the whole set.</summary>
val inline set: value: Set<'T> -> set: cset<'T> -> unit
/// <summary>Replaces the whole set and returns whether the content changed.</summary>
val inline updateTo: target: seq<'T> -> set: cset<'T> -> bool
/// <summary>Applies a batch of set operations atomically.</summary>
val perform: delta: SetDeltaBuilder<'T> -> set: cset<'T> -> unit
/// <summary>Adds all the given elements (one atomic batch).</summary>
val inline unionWith: other: seq<'T> -> set: cset<'T> -> unit
/// <summary>Removes all the given elements (one atomic batch).</summary>
val inline exceptWith: other: seq<'T> -> set: cset<'T> -> unit
/// <summary>Keeps only the elements also present in <c>other</c> (one atomic batch).</summary>
val inline intersectWith: other: seq<'T> -> set: cset<'T> -> unit
/// <summary>Views the changeable set as an adaptive set.</summary>
val inline value: set: cset<'T> -> aset<'T>
/// <summary>Materializes the current state as an immutable snapshot.</summary>
val inline force: set: cset<'T> -> HashSet<'T>
/// <summary>Materializes the F# <c>Set</c> counterpart.</summary>
val inline toSet: set: cset<'T> -> Set<'T> when 'T: comparison
