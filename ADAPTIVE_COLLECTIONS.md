# Adaptive Collections — Implementation Map

Status: **implemented on the web port** (AMap, ASet, AList) and aligned to
the public surface of Mibo.Adaptive's `Api.fs` (ASet 66 names, AMap 71,
AList 89, plus the changeable modules and the public delta builders).
Every deviation from `Api.fs` is listed explicitly at the end.

## Decisions

- **Names stay Mibo's:** `AMap`, `ASet`, `AList` (and `CMap`/`CSet`/`CList`
  for the changeable side), in `Mibo.Signals`.
- **Public API mirrors `Api.fs`.** Write names and semantics match
  (`addOrUpdate`/`remove`/`set`-as-replace on maps; `append`/`insertAt`/
  `updateAt` on lists; `set`/`unionWith`/`exceptWith`/`intersectWith`/
  `perform` on both changeable families). No invented public functions.
- **No delta sinks.** Mibo.Adaptive's `I*DeltaSink`/journal machinery is
  internal node plumbing for low-churn push; it is not part of `Api.fs`
  and is not ported. There is no `observe`. Reads are pulls; materialize
  via `force`/`getValue`/`toMap`/`toSeq`.
- **`custom` keeps its pull-model contract.** `ASet.custom`/`AMap.custom`/
  `AList.custom` receive the current content plus the public
  `SetDeltaBuilder`/`MapDeltaBuilder`/`ListDeltaBuilder` and append the
  operations describing the change since their previous run. The compute
  re-runs on every read (`OnRead` poll), matching Mibo.Adaptive's poll
  semantics on the single JS thread.
- **`ofReader` is ported** (reader runs on every read). `mapUse`/`mapUsei`
  stay out for v1: they need an element-disposal design (they exist in
  `Api.fs`; the port must grow the same disposal contract before shipping
  them).

## JS implementation architecture

Unchanged from the original plan: one class per collection node, per-key
(or per-id) preact signal cells over an F# `Map` snapshot, one structural
version channel, one module-level batch depth counter, three tree-shakable
implementation files, no `Seq.*` in write paths, O(log n) Map operations.

`custom` nodes and `ofExternal` reuse the same state shape: mutable cells
plus a version `CVal`; the poll hook lives in the node's `OnRead` slot, so
every read path (`force`, `count`, derivations) sees fresh content without
speculative recomputes.

## Behavioral contracts under test

`tests/Collections.Tests.fsproj` (MapTests/SetTests/ListTests) ports the
semantics asserted by `Mibo.Adaptive.Tests`:

- `AMap map and filter respond to updates` (adds, removals, threshold
  crossings, non-matching writes ignored).
- `AMap mapA follows entry avals and structural edits` (scalar flag flips,
  whole-map `set`, removals, additions).
- `ASet union updates with add/remove` and keeps shared elements while
  either side holds them.
- `ASet map responds to CSet.set`; `AMap map responds to CMap.set`.
- Batch semantics: writes inside `Collections.batch` land as one net
  change; the pack guard raises for materialize-inside-batch.
- `CMap.perform`/`CSet.perform`/`CList.perform` apply builder batches
  atomically; list builder positions refer to the state as of the previous
  operation.
- `AList.custom` applies `Insert`/`Update`/`Remove` to stable-id cells;
  insertions never recompute existing mapped elements (index stability).
- `ofExternal` snapshots at most once per invalidate.

Web-only divergences from the .NET tests (also divergences of the port):
there is no transaction rollback and no mid-batch read visibility —
`Collections.batch` only coalesces, and materializing inside one raises.
The JS port documents this in `Collections.guardPack`.
