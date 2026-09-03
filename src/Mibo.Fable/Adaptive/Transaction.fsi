/// <summary>
/// Runs a function as a transaction. Writes inside the transaction are
/// deferred and applied at commit. Nested calls join the running
/// transaction. Reads inside a transaction see the pre-transaction values.
/// </summary>
module Mibo.Fable.Adaptive.Transaction

/// <summary>
/// Runs a function as a transaction. Writes inside the transaction are
/// deferred and applied at commit. Nested calls join the running
/// transaction. Reads inside a transaction see the pre-transaction
/// values. A failure aborts: deferred writes are discarded.
/// </summary>
/// <example>
/// <code>
/// Transaction.run (fun () ->
///     CVal.set 1 a
///     CVal.set 2 b) |> ignore
/// </code>
/// </example>
val run: f: (unit -> 'T) -> 'T
