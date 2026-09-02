module Mibo.Fable.Adaptive.CVal

/// <summary>Creates a changeable value initially holding the given value.</summary>
let create(value: 'T) : cval<'T> = ChangeableValue value

/// <summary>Sets the current value (deferred inside a transaction).</summary>
let inline set (value: 'T) (cval: cval<'T>) : unit = cval.Set value

/// <summary>
/// Posts a new value; it is applied automatically at the next graph
/// operation. See <see cref="ChangeableValue&lt;'T&gt;.Post"/>.
/// </summary>
let inline post (value: 'T) (cval: cval<'T>) : unit = cval.Post value

/// <summary>Views the changeable value as an adaptive value.</summary>
let inline value(cval: cval<'T>) : aval<'T> = cval :> aval<'T>
