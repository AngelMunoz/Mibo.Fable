namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Per-element adaptive set node (docs/2026-08-05-MAPA-DESIGN.md): web port of
// ElementSetNode.fs. ASet.mapA / chooseA / filterA share one node: the
// mapping returns an adaptive value per element, cached by input element.
// Structural source changes go through the journal; element-aval changes go
// through a version scan of the cache, gated on the write generation.

/// <summary>
/// Maps every element of a set to an adaptive value (or chooses/filters, when
/// the aval's value is <c>ValueNone</c> to drop the element). Duplicate
/// output values share one reference count (refcounted set).
/// </summary>
type ElementSetNode<'T, 'U when 'T: equality and 'U: equality> =
  new:
    source: IAdaptiveSet<'T> * mapping: ('T -> aval<'U voption>) ->
      ElementSetNode<'T, 'U>

  interface ICommittedVersion
  interface ISetDeltaSink<'T>
  interface IAdaptiveSet<'U>
  interface IDisposable
  interface ISetSinkRegistry
