# Mibo.Fable

**IMPERATIVE**: Report to me ONLY in ASD-STE100 Simplified Technical English.

Mibo.Fable is the web-first port of the [Mibo](https://github.com/AngelMunoz/Mibo) simulation core: an Elmish (MVU) game core written in F#, compiled to JavaScript with [Fable](https://fable-lang.org), so game simulations can run headless in browsers or Node while frontends (Canvas, three.js, Phaser, Pixi) paint the model.

- **Before you write a new building block, check what exists.** The port of Mibo.Core already ships the loop, runners, input, layout, draw commands, and signals. Compose existing pieces; do not re-create them.

General setup and usage instructions are in [README.md](README.md). Template details are in [MANUAL.md](MANUAL.md).

## Imperatives

1. **NEVER PUSH WITHOUT PERMISSION.** Always ask before pushing to the remote. A previous push permission for one set of work DOES NOT MEAN TO ALWAYS PUSH AFTER. Permissions are granted per set of work, not for session length.
2. **NEVER FORCE PUSH.** Tell the user they have to force push instead of you.
3. **Always run `dotnet fantomas .` before committing code.** Format all F# files before staging.
4. **Never use `Option.get` or `ValueOption.get`.** Thread optional values through the combinators instead: `Option.map` / `bind` / `filter` / `iter` / `defaultValue` / `orElse`, and the `ValueOption.*` and `AVal.map` equivalents. Match only when the two branches carry different logic, not to unwrap and re-wrap.
5. Pull requests made with the `gh` command should use a markdown file as the PR body, not inline escaped markdown strings.
6. New F# files **MUST** be paired with their own signature file.
7. Comments go to the signature files. Implementation quirk comments may stay in implementation files.

## Architecture

- `src/Mibo.Fable/` — the simulation core published as the `Mibo.Fable` package. Renderer packages (`Mibo.Fable.Threejs`, `Mibo.Fable.Phaser`, ...) are sibling projects under `src/` that reference this one, mirroring how `Mibo.MonoGame`/`Mibo.Raylib` sit next to `Mibo.Core` upstream.
  - `Vectors.fs` — own `Vector2/3/4` structs. **Must stay first in the compile order** in `Mibo.Fable.fsproj`; everything else depends on it.
  - `Adaptive.fs` — the scalar core under namespace `Mibo.Fable.Adaptive`: `CVal`/`AVal`, transactions, posting, dependency collector. A web port of `Mibo.Adaptive`'s `Core/Library.fs` (pull-lazy dependency graph: writes bump versions, reads version-check and recompute at most once per change). Loads right after `Vectors.fs`.
  - `Adaptive.Collections.fs` + node files — the adaptive collections (`aset`/`amap`/`alist`, `cset`/`cmap`/`clist`) as a web port of `Mibo.Adaptive`'s `Core/Collections/*`: journals, delta sinks, refcounted sets, two-source algebra, reductions, per-element `*A` nodes. The public surface lives in `Adaptive.Api.fs` (`ASet`/`AMap`/`AList`/`CSet`/`CMap`/`CList`).
  - `Elmish.*.fs` — the MVU loop, time, commands, subscriptions, and the headless runner (`HeadlessRunner`: `Step`, `StepN`, `StepUntil`, `Dispatch`, `Model`).
  - `Adaptive.Headless.fs` — the adaptive runner (`AdaptiveHeadless`: State · Projection · Update · Force). Same host surface as `HeadlessRunner`, but `update` mutates adaptive roots and observers receive a readonly frame forced at the end of each step. Fixed-step, intent queues, `PostTask` (promise completions re-enter via the post drain) included; see `tests/Adaptive.Tests.fs` for the semantic contract.
  - `Layout/`, `Layout3D/` — grid, hex, and spatial layouts.
  - `Graphics/`, `Graphics2D/`, `Graphics3D/` — the backend-neutral draw command DSL.
  - `Diagnostics.fs` — frame profiler. Uses a system clock and reports zeros for GC counters on JS (no way to count allocations there).
  - `Mibo.Fable.fs` — the JavaScript-facing exports module (`Mibo.Fable.Exports`).

- `demo/` — a Vite page that steps a headless simulation each `requestAnimationFrame` and paints the model with Canvas2D. No rendering library by design; three.js/Phaser frontends replace exactly this part.

- `tests/` — the smoke suites (`pnpm test`). New runtime behavior gets a check here before it is considered done.

## Web constraints (keep the core Fable-compatible)

Everything in `src/Mibo.Fable/` must compile with the Fable compiler. Do not reintroduce .NET-only APIs:

- `System.Numerics` — use `Mibo.Vectors` instead. Fable cannot resolve `Vector3`/`Vector4` at all.
- `System.Buffers` (ArrayPool), `Span`, and byref array access (`let x = &arr.[i]`) — use plain arrays, `Array.blit`, and direct index writes.
- `System.Collections.Concurrent`, `System.Threading` threads/semaphores/timers, `Stopwatch`, `GC` — the browser has one thread; `requestAnimationFrame` replaces OS pacing. The `.NET`-only `Run`/`RunAsync` members are deliberately out of the headless runner.
- `GameContext` service registry helpers (`register`/`tryGetService`/`getService`) and any accessor that passes a generic type parameter through the registry **must stay `inline`** — Fable erases generics, and `typeof<'T>` only resolves at inlined call sites.
- Service lookup uses `unbox` after the `typeof<'T>` dictionary hit, never a `:? 'T` test — interface type tests always evaluate to false on JS and break lookups silently.

## Project Considerations

The core abstractions of the library MUST NOT incur performance penalties for users. Abstractions aim to be zero-cost or close to zero-cost. We still strive for performance, now for the JS runtime: the rules change shape, not the goal.

- Prefer records and value-shaped types over classes. Under Fable everything lives on the JS heap, so the struct-vs-class choice is about semantics. Allocation discipline matters more than on .NET: there is no ArrayPool.
- Avoid heap allocations in hot paths. Reuse arrays and buffers across frames. Write a signal root only when its value changed.
- Favor arrays over lists. `Array.blit` and index writes replace Span and pooling.
- Prefer inline functions for hot-path helpers. Fable erases them into call sites.
- Favor functional programming patterns, but allow mutable state when performance needs it.
- Write-path equality is value-based (`EqualityComparer.Default`, structural on Fable). A write with an equal value marks nothing; collection deltas are derived at the source.
- Public API should be ergonomic and easy to use.
- Public API should be well documented with XML comments.
- Public API should follow elmish-friendly patterns where applicable (State · Projection · Update · Force for the adaptive side).

## Fable quirks

Fable compiles F# to JS with a subset of .NET semantics. These behave differently than on .NET:

- **TimeSpan comparisons can truncate.** Some `TimeSpan` operations compile to int coercion on JS; sub-millisecond frame deltas vanish. Do time math through `TotalMilliseconds` (a float) and `TimeSpan.FromMilliseconds`.
- **Interface type tests are always false.** `:? IThing` never matches on JS. The service registry uses keyed lookup plus `unbox` (safe: registration keys on `typeof<'T>`).
- **Generics need `inline`.** `typeof<'T>` and generic pass-through accessors only work when the function is `inline`; Fable substitutes the concrete type at each call site.
- **Interface member dispatch needs concrete receivers.** Calling Contains or the get_Item indexer through IReadOnlySet/IReadOnlyList throws at runtime: no such qualified members exist in the emitted JS. Route views through Collections.asHashSet/asDictionary/asResizeList before member access; plain iteration (for x in view) is safe.
- **`throw 1` in emitted JS** means Fable dropped part of a member — usually broken indentation in the F# source or an unsupported construct. The compiler can still exit 0. Grep the emitted output when behavior looks impossible.

## Commands

| Command           | Description                                                                        |
| ----------------- | ---------------------------------------------------------------------------------- |
| `pnpm install`    | Installs node dependencies and restores dotnet tools (postinstall)                 |
| `pnpm build`      | Compiles the library to JavaScript (`src/Mibo.Fable/Mibo.Fable.fs.js` + `src/Mibo.Fable/fable_modules/`) |
| `pnpm demo`       | Watches the demo and serves it with Vite (http://localhost:5173)                   |
| `pnpm build:demo` | Builds the demo production bundle in `demo/dist/`                                  |
| `pnpm test`       | Compiles and runs the smoke suites in `tests/`                                     |

## Changelog Management

We follow https://github.com/ionide/KeepAChangelog guidelines

Changelog Format:

```markdown
# Changelog

## [Unreleased]

### Added

- **Signals runtime:** The adaptive State · Projection · Update · Force execution model now runs on the web. Update functions mutate signal roots; each step publishes a readonly frame to observers.

## [0.0.1] - 2026-09-01

### Added

- Initial release
```

Each section may contain the following categories:

- Added
- Changed
- Deprecated
- Removed
- Fixed
- Security

When adding entries to the changelog, make sure to follow format and categories.

### Writing style

The changelog is written for **developers upgrading their version**, not as a development
journal. Keep these rules in mind:

1. **Concise and reader-focused.** Each entry is one bullet that says what changed and why a
   user cares - not how it's implemented internally. No internal module/file paths, no build/
   milestone/phase numbers (e.g. "B12", "Phase 3"), no section references (e.g. "§6.2"), and no
   "mirrors the canonical X" narration. A reader should understand the entry without reading the
   code.

2. **Group by user-facing concern, not by task.** One bullet per feature/fix area. If multiple
   commits touch the same subsystem (e.g. several shadow-pass fixes), collapse them into one
   bullet that names each fix briefly, rather than one bullet per commit.

3. **Only released code can be Changed or Fixed.** Features that have never shipped belong in
   `Added` - there is no prior version to change from or fix against. Design choices and
   implementation details of a new feature are part of its `Added` description, not separate
   `Fixed`/`Changed` entries. Use `Changed`/`Fixed` only for modifications to already-released
   behavior (and mark breakage with **Breaking:** or **Breaking (behavioral):**).

4. **Lead with the affected surface.** Bold-prefix each bullet with the area:
   `**Signals runtime:**`, `**MVU runtime:**`, `**Demo:**`, `**Docs:**`, etc. - so a reader can
   scan for their area. Keep breaking changes at the top of their category.

5. **Plain language.** Describe the user-visible effect ("shadows render correctly on scaled
   objects"), not the code diff ("BoundingSphere.Transform now scales center and radius"). The
   reader wants to know what they'll observe, not what line changed.
