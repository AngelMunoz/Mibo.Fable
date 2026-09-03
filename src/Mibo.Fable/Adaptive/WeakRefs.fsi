namespace Mibo.Fable.Adaptive

open Fable.Core

/// Internal. JS WeakRef interop (System.WeakReference is not supported by
/// Fable). Deref normalizes undefined and null slots to null.
module internal WeakRefs =
  [<Emit("new WeakRef($0)")>]
  val create: target: obj -> obj

  [<Emit("($0 && $0.deref()) || null")>]
  val deref: weakRef: obj -> obj
