namespace Mibo.Fable.Adaptive

open Fable.Core

/// Internal. WeakMap-based markers plus prototype-member probes: the
/// JS-safe replacement for the interface type tests the original performs
/// (:? ICommittedVersion and :? ISetSinkRegistry & friends — always false on
/// JS). Fable emits interface implementation members as named prototype
/// members, so the probes test for them; nodes may also register eagerly in
/// the WeakMaps (the changeable sources do) and the lookups accept both.
module internal WeakMarkers =
  type internal IWeakMap =
    abstract has: key: obj -> bool
    abstract set: key: obj * value: obj -> unit

  [<Emit("new WeakMap()")>]
  let committedVersions: IWeakMap = jsNative

  [<Emit("new WeakMap()")>]
  let setRegistries: IWeakMap = jsNative

  [<Emit("new WeakMap()")>]
  let mapRegistries: IWeakMap = jsNative

  [<Emit("new WeakMap()")>]
  let listRegistries: IWeakMap = jsNative

  [<Emit("\"CommittedVersion\" in $0")>]
  let hasCommittedVersion(x: obj) : bool = jsNative

  [<Emit("\"AddSetSink\" in $0")>]
  let hasSetRegistry(x: obj) : bool = jsNative

  [<Emit("\"AddMapSink\" in $0")>]
  let hasMapRegistry(x: obj) : bool = jsNative

  [<Emit("\"AddListSink\" in $0")>]
  let hasListRegistry(x: obj) : bool = jsNative

  /// Mark a node as a version-inflating node (implements ICommittedVersion).
  let inline markCommittedVersion(node: IAdaptiveObject) : unit =
    committedVersions.set(node, node)

  /// Mark a node as a set delta sink registry (implements ISetSinkRegistry).
  let inline markSetRegistry(node: obj) : unit = setRegistries.set(node, node)

  /// Mark a node as a map delta sink registry (implements IMapSinkRegistry).
  let inline markMapRegistry(node: obj) : unit = mapRegistries.set(node, node)

  /// Mark a node as a list delta sink registry (implements IListSinkRegistry).
  let inline markListRegistry(node: obj) : unit = listRegistries.set(node, node)
