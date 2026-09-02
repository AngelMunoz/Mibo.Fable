module Mibo.Fable.Adaptive.Transaction

/// <summary>
/// Runs a function as a transaction. Writes inside the transaction are deferred
/// and applied at commit. Nested calls join the running transaction.
/// Reads inside a transaction see the pre-transaction values.
/// </summary>
let run(f: unit -> 'T) : 'T =
  let ctx = GraphContext.Current
  ctx.ClaimOwner()

  try
    if ctx.TxActive then
      f()
    else
      ctx.TxActive <- true
      ctx.TxBuffer.Reset()
      let mutable committed = false

      let result =
        try
          let value = f()
          ctx.TxBuffer.Commit()
          committed <- true
          value
        finally
          if not committed then
            ctx.TxBuffer.Abort()

          ctx.TxActive <- false

      result
  finally
    ctx.ReleaseOwner()
