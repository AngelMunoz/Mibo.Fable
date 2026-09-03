namespace Mibo.Fable.Adaptive

open System
open System.Collections.Generic

// Per-element adaptive list node (docs/2026-08-05-MAPA-DESIGN.md): web port
// of ElementListNode.fs. AList.mapA / chooseA / filterA share one node: the
// mapping returns an aval per element, cached by INPUT position.

/// <summary>
/// Maps every element of a list to an adaptive value (or chooses/filters,
/// when the aval's value is <c>ValueNone</c> to drop the element). The
/// mapping receives the input position (FDA parity: the i-variants).
/// </summary>
type ElementListNode<'T, 'U> =
  new:
    source: IAdaptiveList<'T> * mapping: (int -> 'T -> aval<'U voption>) ->
      ElementListNode<'T, 'U>

  interface IListDeltaSink<'T>
  interface IAdaptiveList<'U>
  interface IDisposable
  interface IListSinkRegistry
