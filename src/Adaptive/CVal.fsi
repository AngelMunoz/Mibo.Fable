/// <summary>Operations on changeable values.</summary>
module Mibo.Fable.Adaptive.CVal

/// <summary>Creates a changeable value initially holding the given value.</summary>
val create: value: 'T -> cval<'T>
/// <summary>Sets the current value (deferred inside a transaction).</summary>
val inline set: value: 'T -> cval: cval<'T> -> unit
/// <summary>
/// Posts a new value; it is applied automatically at the next graph
/// operation. See <see cref="ChangeableValue&lt;'T&gt;.Post"/>.
/// </summary>
val inline post: value: 'T -> cval: cval<'T> -> unit
/// <summary>Views the changeable value as an adaptive value.</summary>
val inline value: cval: cval<'T> -> aval<'T>
