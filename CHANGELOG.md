# Changelog

## [Unreleased]

### Added

- **Core:** The Mibo simulation core runs on the web. Games write F# with the Elmish loop, headless runner, input, 2D/3D layout, and the draw command DSL, then compile to JavaScript to run headless in the browser or Node. Frontends like three.js or Phaser paint the model through the runner.
- **Adaptive runtime:** The adaptive State · Projection · Update · Force execution model works on the web as a port of Mibo.Adaptive's scalar core. Writes bump versions and defer inside `Transaction.run`; reads version-check their dependency snapshots and recompute at most once per change, with dynamic dependencies (`bind`) re-reading whatever the mapping returns. Includes fixed-step sub-stepping with a backlog cap, posted writes and intents that drain as one batch at the next step boundary, and `PostTask` for background work whose result re-enters at the next drain.
- **Adaptive collections:** `ASet`, `AMap`, and `AList` arrive as a port of Mibo.Adaptive's collection layer, with the changeable `CSet`/`CMap`/`CList` write side. Writes append net deltas to per-dependency journals (cross-kind cancellation for sets, last-op-wins coalescing for maps); reads drain the journal and deliver the reduced delta downstream. Refcounted sets back `map`/`union`/`collect`, so an element contributed by several sources leaves only when its last contributor drops. Two-source algebra (`difference`/`intersect`/`xor`, `choose2`/`unionWith`), live per-group `groupBy`, per-key `joinOn`, incremental reductions, the per-element `mapA`/`chooseA`/`filterA` families, and the pull-model `custom` compute with its delta builders all mirror Mibo.Adaptive's public API; `ofExternal`/`ofReader` connect world data.
- **MVU runtime:** Long-running work no longer blocks the frame. `Cmd.ofPromise` joins `Cmd.ofAsync` and `Cmd.ofTask`, mapping a promise's result — or its error — to a message on a later frame.
- **Demo:** One bouncing-ball simulation runs under both execution models side by side, with a shared renderer. Each execution model lives in its own web worker: the simulation steps at 30 Hz, the painter repaints at 60 fps and blends positions between the last two snapshots, and the canvas transfers to an OffscreenCanvas, so no simulation data crosses to the page — clicks go in as messages.
- **Tests:** `pnpm test` runs a smoke suite covering both runtimes — step ordering, memoized projections that recompute only when their inputs change, journal/delta delivery, transaction netting, posts, and fixed-step caps. QUnit-based suites run under Node with the qunit CLI.
- **JS and TS output:** Every public module compiles to its own JavaScript and TypeScript file with stable, bare export names (`ASet.empty`, `Sub.batch`) and no generated name suffixes on the public surface, so bundlers tree-shake per family and imports read the same as the F# API.

## [0.0.1] - 2026-09-01

### Added

- Initialized project from the Fable package template.
