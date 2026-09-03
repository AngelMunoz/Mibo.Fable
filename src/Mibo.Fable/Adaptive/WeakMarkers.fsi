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
  val committedVersions: IWeakMap

  [<Emit("new WeakMap()")>]
  val setRegistries: IWeakMap

  [<Emit("new WeakMap()")>]
  val mapRegistries: IWeakMap

  [<Emit("new WeakMap()")>]
  val listRegistries: IWeakMap

  [<Emit("\"CommittedVersion\" in $0")>]
  val hasCommittedVersion: x: obj -> bool

  [<Emit("\"AddSetSink\" in $0")>]
  val hasSetRegistry: x: obj -> bool

  [<Emit("\"AddMapSink\" in $0")>]
  val hasMapRegistry: x: obj -> bool

  [<Emit("\"AddListSink\" in $0")>]
  val hasListRegistry: x: obj -> bool

  /// Mark a node as a version-inflating node (implements ICommittedVersion).
  val inline markCommittedVersion: node: IAdaptiveObject -> unit

  /// Mark a node as a set delta sink registry (implements ISetSinkRegistry).
  val inline markSetRegistry: node: obj -> unit

  /// Mark a node as a map delta sink registry (implements IMapSinkRegistry).
  val inline markMapRegistry: node: obj -> unit

  /// Mark a node as a list delta sink registry (implements IListSinkRegistry).
  val inline markListRegistry: node: obj -> unit
