namespace Mibo.Fable.Adaptive

open Fable.Core

/// Internal. JS WeakRef interop (System.WeakReference is not supported by
/// Fable). Deref normalizes undefined and null slots to null.
module internal WeakRefs =
  [<Emit("new WeakRef($0)")>]
  let create(target: obj) : obj = jsNative

  [<Emit("($0 && $0.deref()) || null")>]
  let deref(weakRef: obj) : obj = jsNative
