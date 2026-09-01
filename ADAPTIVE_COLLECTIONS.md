# Adaptive Collections — Implementation Map

Status: **plan for review — no code written.**

This document specifies how the adaptive collections — **AMap, ASet, and
AList** — will be built on top of `Mibo.Signals` (signals-core), covering the
**complete public API surface of Mibo.Adaptive** (`Api.fs`: ASet 82 functions,
AMap 89, AList 115, plus the delta types and the changeable write types).
Every public name is mapped to an implementation group or explicitly ruled
out for the web model with a reason.

## Decisions

- **Names stay Mibo's:** `AMap`, `ASet`, `AList`, in `Mibo.Signals`.
- **Write trio on maps:** `add` inserts a new key, `set` writes an existing
  key, `addOrUpdate` inserts or overwrites — the upsert is `addOrUpdate`.
- **`post*` keeps Mibo's names**, backed by boundary intents.
- **Full-surface parity is the goal.** Every public name is accounted for;
  nothing is cut for being unused.
- Sequencing of the work is the reviewer's call; this document only maps the
  surface.

## JS implementation architecture

**Representation: one internal class per collection node, module functions for
the API, per-key preact signals for the elements. No mixins, no inheritance,
no proxies.**

- One F# class per node, one stable hidden class, fields assigned in the
  constructor — monomorphic call sites everywhere.
- Per-key cells are real `@preact/signals-core` signals; derived collections
  hold per-key `computed` nodes. One element costs one signal object plus one
  computed and one closure when derived, GC-reclaimed on removal.
- Cell storage is F# `Map`: persistent snapshots make the frame pack O(1) and
  keep writes structurally shared.
- Batching is one module-level depth counter; zero side effects at module
  init.

**Tree-shaking.** Three implementation files (`Signals.Collections.Map.fs`,
`Set.fs`, `List.fs`) plus one internal shared journal module, each a separate
JS module with named exports. Fable omits unreferenced bindings and the fsi
files keep internals file-private, so unused operators and whole families drop
from the bundle at export granularity. Expected family cost after bundling: a
few kilobytes plus `@preact/signals-core` (~2 kB min).

**Performance rules.** Writes are two to four field mutations plus a flush. No
`Seq.*` combinators in write or delta paths. `Map` operations are O(log n)
with structural sharing; comfortable to thousands of keys; tens of thousands
of per-key nodes is the documented ceiling.

## Shared machinery (all families)

| Mibo.Adaptive concept | Web implementation |
|---|---|
| `IMapDeltaSink` / `ISetSink` / journal | `DeltaSink` list receiving `(sets, rems)` batches at commit |
| `CommitJournal` / `JournalSession` | flush at batch end, or immediately outside a batch |
| `Version` + `committedVersion` | int64 per collection, bumped at commit |
| `BumpWriteGeneration` / `ClaimOwner` | not needed — one JS thread; the runner owns the graph |
| Sink registries | sink list with `IDisposable` handles |
| `post*` members | same names, backed by boundary intents |
| `ofExternal` | maps a callback/event source to writes |
| `ofReader` | ruled out — .NET transactional reader machinery, no JS equivalent |
| Element nodes | per-key `computed` nodes |
| Weak tables / finalizers | JS GC — dropped nodes are collected |

## AMap<'K, 'V> — full surface (89)

Value writes touch one cell; structural writes touch the structure signal;
`tryFind`/`count`/element reads pick their channel conditionally; `force`/
`toMap` materialize for the frame.

- **Builders:** `empty`, `constant`, `single`, `ofAVal`, `ofArray`, `ofList`,
  `ofMap`, `ofSeq`, `ofAList`, `ofASet`, `ofASetIgnoreDuplicates`,
  `ofASetMapped`, `ofASetMappedIgnoreDuplicates`, `custom`, `delay`,
  `perform`.
- **Writes:** `add` (insert new key), `set` (write existing key),
  `addOrUpdate` (upsert), `remove`, `clear`, `postAddOrUpdate`, `postClear`,
  `postRemove`, `postSet`.
- **Reads:** `getValue`, `value`, `force`, `toMap`, `toAVal`, `toSeq`,
  `toAList`, `toASet`, `toASetValues`, `isEmpty`, `containsKey`, `find`,
  `tryFind`, `tryGetValue`, `item`, `keys`, `exists`, `existsA`, `forall`,
  `forallA`, `count`, `countBy`, `countByA`.
- **Derivations:** `map`, `mapA`, `mapV`, `mapSet`, `mapUse`, `filter`,
  `filterA`, `filterV`, `choose`, `choose2`, `choose2V`, `chooseA`, `chooseAV`,
  `chooseV`, `bind`, `bind2`, `bind3`, `joinOn`, `groupBy`, `difference`,
  `intersect`, `intersectV`, `intersectWith`, `union`, `unionWith`,
  `updateTo`.
- **Aggregates:** `fold`, `foldGroup`, `foldHalfGroup`, `average`,
  `averageBy`, `averageByA`, `sumBy`, `sumByA`, `tryMaxA`, `tryMinA`,
  `reduce`, `reduceBy`, `reduceByA`.
- **Ruled out:** `ofReader`.

## ASet<'T> — full surface (82)

Structure-only node: no value cells. `contains`/`count`/`exists`/`forall`
depend on the structure signal; `map`/`mapA`/`mapTo`/`collect` derive per-key
computed nodes.

- **Builders:** `empty`, `constant`, `single`, `range`, `ofSeq`, `ofArray`,
  `ofList`, `ofHashSet`, `ofAVal`, `ofExternal`, `custom`, `delay`,
  `perform`.
- **Writes:** `add`, `remove`, `set`, `postAdd`, `postRemove`, `postSet`.
- **Reads:** `getValue`, `value`, `force`, `toAVal`, `toSet`, `isEmpty`,
  `contains`, `exists`, `existsA`, `forall`, `forallA`, `count`, `countBy`,
  `countByA`, `average`, `averageBy`, `averageByA`, `sum`, `sumBy`, `sumByA`,
  `tryMax`, `tryMaxA`, `tryMin`, `tryMinA`, `reduce`, `reduceBy`, `reduceByA`,
  `fold`, `foldGroup`, `foldHalfGroup`.
- **Derivations:** `map`, `mapA`, `mapUse` (ruled out for v1 — element
  disposal design needed), `bind`, `bind2`, `bind3`, `collect`, `collect'`,
  `filter`, `filterA`, `choose`, `chooseA`, `chooseAV`, `chooseV`,
  `difference`, `exceptWith`, `intersect`, `intersectWith`, `union`,
  `unionMany`, `unionWith`, `xor`, `updateTo`.

## AList<'T> — full surface (115)

Stable internal ids are the design center: element cells are keyed by id, not
position, so insertions and moves never recompute element values — only the
order signal moves, and index-dependent consumers re-derive. Deltas are
`ListOp`s (kind, position, value), mirroring Mibo.Adaptive's journal.

- **Builders:** `empty`, `single`, `constant`, `ofSeq`, `ofArray`, `ofList`,
  `ofResizeArray`, `ofAVal`, `ofASet`, `ofExternal`, `custom`, `delay`,
  `perform`, `init`, `range`.
- **Writes:** `add`, `addRange`, `append`, `prepend`, `insertAt`, `remove`,
  `removeAt`, `updateAt`, `set`, `clear`, `postAppend`, `postClear`,
  `postInsertAt`, `postPrepend`, `postRemove`, `postRemoveAt`, `postSet`,
  `postUpdateAt`.
- **Reads:** `getValue`, `value`, `force`, `toAVal`, `toArray`, `toList`,
  `toASet`, `toIndexedASet`, `isEmpty`, `count`, `countBy`, `countByA`,
  `exists`, `existsA`, `forall`, `forallA`, `tryAt`, `tryGet`, `tryFirst`,
  `tryLast`, `tryMax`, `tryMaxA`, `tryMin`, `tryMinA`, `reduce`, `reduceBy`,
  `reduceByA`, `fold`, `foldGroup`, `foldHalfGroup`, `average`, `averageBy`,
  `averageByA`, `sum`, `sumBy`, `sumByA`.
- **Derivations:** `map`, `mapA`, `mapUse` (ruled out for v1), `mapUsei`
  (ruled out for v1), `mapi`, `mapiA`, `choose`, `chooseA`, `chooseAV`,
  `choosei`, `chooseiA`, `chooseiAV`, `chooseiV`, `chooseV`, `filter`,
  `filterA`, `filteri`, `filteriA`, `bind`, `bind2`, `bind3`, `concat`,
  `indexed`, `rev`, `sub`, `subA`, `take`, `takeA`, `skip`, `skipA`,
  `pairwise`, `pairwiseCyclic`, `sort`, `sortBy`, `sortByi`,
  `sortByDescending`, `sortByDescendingi`, `sortDescending`, `sortWith`.

Index stability is the list's contract: moves and inserts recompute
order-dependent consumers but never element values. Sorts rebuild the order
signal.

## Scalar and frame bridge (shared by all three)

`count`/`contains`/order reads depend on the structural channel. Element reads
depend on one cell. `force`/`toMap` materialize plain data for the RenderFrame
— pack once per step, read plain afterward. Aggregates over values re-run on
any write; no incremental reductions.

## Dry-run findings

1. **`joinOn` update ordering.** A join-key change swaps the old and new target
   dependencies inside one recompute; smoke-assert no stale read during the
   swap.
2. **Derived-structure granularity.** `filter` recomputes key sets O(n) on
   possible membership flips. Same cost as Mibo.Adaptive.
3. **Aggregates stay coarse.** `fold`/`sum`/`average` re-run on any write.
4. **Sinks hold strong references.** `observe` returns `IDisposable`; callers
   dispose or the list leaks.
5. **Sinks fire during update.** Unbatched writes flush immediately; sinks
   never write collection state — smoke-asserted.
6. **Reference invalidation per key.** Fresh-but-equal records still notify.
   Write only on change.
7. **`constant` collections are sealed** — no delta machinery for world data.
8. **Pack inside a batch raises** — pack-once-per-step contract.
9. **`post*` becomes boundary intents.** Same names, same "applied at the next
   step boundary" semantics, no threads.
10. **`mapUse`/`mapUsei`** need an element-disposal design before code.
11. **`ofReader`** is ruled out — .NET transactional reader machinery.
12. **Tree-shaking granularity.** Three modules keep families separable;
    unused operators drop from the bundle (verified behavior from the fsi
    work).
13. **Smoke contracts.** Per-key recompute isolation; count ignoring value
    writes; absence-to-presence transitions; join swap ordering; delta batch
    contents; one commit per batch; list index stability (move/insert without
    element recompute).
