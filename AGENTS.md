# Mibo.Fable

Mibo.Fable is the web-first port of the [Mibo](https://github.com/AngelMunoz/Mibo) simulation core: an Elmish (MVU) game core written in F#, compiled to JavaScript with [Fable](https://fable-lang.org), so game simulations can run headless in browsers or Node while frontends (Canvas, three.js, Phaser, Pixi) paint the model.

General setup and usage instructions can be found in the [README.md](README.md) file, and template details in [MANUAL.md](MANUAL.md).

## Architecture

- `src/` — the simulation core published as the `Mibo.Fable` package
  - `Vectors.fs` — own `Vector2/3/4` structs. **Must stay first in the compile order** in `Mibo.Fable.fsproj`; everything else depends on it.
  - `Elmish.*.fs` — the MVU loop, time, commands, subscriptions, and the headless runner (`HeadlessRunner`: `Step`, `StepN`, `StepUntil`, `Dispatch`, `Model`).
  - `Layout/`, `Layout3D/` — grid, hex, and spatial layouts.
  - `Graphics/`, `Graphics2D/`, `Graphics3D/` — the backend-neutral draw command DSL.
  - `Diagnostics.fs` — frame profiler. Uses a system clock and reports zeros for GC counters on JS (no way to count allocations there).
  - `Mibo.Fable.fs` — the JavaScript-facing exports module (`Mibo.Fable.Exports`).

- `demo/` — a Vite page that steps a headless simulation each `requestAnimationFrame` and paints the model with Canvas2D. No rendering library by design; three.js/Phaser frontends would replace exactly this part.

## Web constraints (keep the core Fable-compatible)

Everything in `src/` must compile with the Fable compiler. Do not reintroduce .NET-only APIs:

- `System.Numerics` — use `Mibo.Vectors` instead. Fable cannot resolve `Vector3`/`Vector4` at all.
- `System.Buffers` (ArrayPool), `Span`, and byref array access (`let x = &arr.[i]`) — use plain arrays, `Array.blit`, and direct index writes.
- `System.Collections.Concurrent`, `System.Threading` threads/semaphores/timers, `Stopwatch`, `GC` — the browser has one thread; `requestAnimationFrame` replaces OS pacing. The `.NET`-only `Run`/`RunAsync` members were deliberately left out of the headless runner.
- `GameContext` service registry helpers (`register`/`tryGetService`/`getService`) and any accessor that passes a generic type parameter through the registry **must stay `inline`** — Fable erases generics, and `typeof<'T>` only resolves at inlined call sites.
- Service lookup uses `unbox` after the `typeof<'T>` dictionary hit, never a `:? 'T` test — interface type tests always evaluate to false on JS and would silently break lookups.

## Commands

| Command           | Description                                                          |
| ----------------- | -------------------------------------------------------------------- |
| `pnpm install`    | Installs node dependencies and restores dotnet tools (postinstall)    |
| `pnpm build`      | Compiles the library to JavaScript (`src/Mibo.Fable.fs.js` + `src/fable_modules/`) |
| `pnpm demo`       | Watches the demo and serves it with Vite (http://localhost:5173)      |
| `pnpm build:demo` | Builds the demo production bundle in `demo/dist/`                     |

## Imperatives

1. **NEVER PUSH WITHOUT PERMISSION.** Always ask before pushing to the remote.
2. **NEVER FORCE PUSH.** Tell the user they have to force push instead of you.
