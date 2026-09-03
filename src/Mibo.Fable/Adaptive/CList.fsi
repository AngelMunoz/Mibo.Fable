/// <summary>Operations on changeable lists.</summary>
module Mibo.Fable.Adaptive.CList

open System
open System.Collections.Generic

/// <summary>An empty changeable list.</summary>
val inline empty<'T> : clist<'T>
/// <summary>A changeable list with the given items.</summary>
val inline ofSeq: items: seq<'T> -> clist<'T>
/// <summary>A changeable list with the given items.</summary>
val inline ofArray: items: 'T[] -> clist<'T>
/// <summary>A changeable list with the given items.</summary>
val inline ofList: items: 'T list -> clist<'T>
/// <summary>Appends an element at the end of the list.</summary>
val inline append: value: 'T -> list: clist<'T> -> unit
/// <summary>Inserts an element at the start of the list.</summary>
val inline prepend: value: 'T -> list: clist<'T> -> unit
/// <summary>Inserts an element before the element currently at the position.</summary>
val inline insertAt: position: int -> value: 'T -> list: clist<'T> -> unit
/// <summary>Removes the element currently at the position.</summary>
val inline removeAt: position: int -> list: clist<'T> -> unit
/// <summary>Replaces the element currently at the position.</summary>
val inline updateAt: position: int -> value: 'T -> list: clist<'T> -> unit
/// <summary>Removes the first occurrence of the value. No-op when absent.</summary>
val inline remove: value: 'T -> list: clist<'T> -> unit
/// <summary>Posts an append (the cval.Post handoff pattern).</summary>
val inline postAppend: value: 'T -> list: clist<'T> -> unit
/// <summary>Posts an insert at the start.</summary>
val inline postPrepend: value: 'T -> list: clist<'T> -> unit
/// <summary>Posts an insert before the element currently at the position.</summary>
val inline postInsertAt: position: int -> value: 'T -> list: clist<'T> -> unit
/// <summary>Posts a remove at the position.</summary>
val inline postRemoveAt: position: int -> list: clist<'T> -> unit
/// <summary>Posts a replace at the position.</summary>
val inline postUpdateAt: position: int -> value: 'T -> list: clist<'T> -> unit
/// <summary>Posts a remove of the first occurrence of the value.</summary>
val inline postRemove: value: 'T -> list: clist<'T> -> unit
/// <summary>Posts a clear.</summary>
val inline postClear: list: clist<'T> -> unit
/// <summary>Posts a full replace (supersedes the other ops of the same pending batch).</summary>
val inline postSet: values: seq<'T> -> list: clist<'T> -> unit
/// <summary>Removes all elements.</summary>
val inline clear: list: clist<'T> -> unit
/// <summary>Replaces the whole list (last-wins over the batch inside a transaction).</summary>
val inline set: values: seq<'T> -> list: clist<'T> -> unit
/// <summary>Replaces the whole list and returns whether the content changed.</summary>
val inline updateTo: target: 'T[] -> list: clist<'T> -> bool
/// <summary>Applies a batch of positional list operations atomically.</summary>
val perform: delta: ListDeltaBuilder<'T> -> list: clist<'T> -> unit
/// <summary>Appends all the given elements (one atomic batch).</summary>
val inline addRange: items: seq<'T> -> list: clist<'T> -> unit
/// <summary>Views the changeable list as an adaptive list.</summary>
val inline value: list: clist<'T> -> alist<'T>
/// <summary>Materializes the current state as an immutable array snapshot.</summary>
val inline force: list: clist<'T> -> 'T[]
/// <summary>Materializes the F# <c>list</c> counterpart.</summary>
val inline toList: list: clist<'T> -> 'T list
