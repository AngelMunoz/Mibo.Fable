# Mibo.Fable

The Mibo game simulation core, compiled to JavaScript/TypeScript-friendly output with [Fable](https://fable-lang.org).

Write the game simulation in F# (Elmish/MVU loop, input, layout grids, draw commands), run it headless in the browser or Node, and paint the model with whatever frontend you like — Canvas, three.js, Phaser, Pixi.

<!-- Could you please keep the link below so people can find the original template 🙏 -->

[![](https://img.shields.io/badge/Project_made_using_Glutinum.Template-7679db?style=for-the-badge)](https://github.com/glutinum-org/Glutinum.Template)

[![NuGet](https://img.shields.io/nuget/v/Mibo.Fable.svg)](https://www.nuget.org/packages/Mibo.Fable)

## How it fits together

- `src/Mibo.Fable/` — the simulation core. The Elmish loop and headless runner live in the `Mibo.Elmish` namespace; input, layout, and the backend-neutral draw DSL in `Mibo.Input`, `Mibo.Layout`, `Mibo.Layout3D`, `Mibo.Elmish.Graphics`. Vectors are plain F# structs (`Mibo.Vectors`), chosen over `System.Numerics` so the whole core compiles to JS. Renderer packages (`Mibo.Fable.ThreeJS`, ...) live next to it as sibling projects under `src/`.
- `Mibo.Fable.Exports` (in `src/Mibo.Fable/Mibo.Fable.fs`) — the small wrapper JavaScript hosts use to drive the headless runner: `createRunnerWithTick`, `stepFrame`, `dispatch`, `model`, and friends.
- `demo/` — a Vite multi-page demo: a landing page links to the Canvas2D page (a bouncing-ball simulation in two workers painting through OffscreenCanvas) and the ThreeJS page (spinning cubes driven on the main thread and in workers).
- `tests/` — QUnit smoke suites. `pnpm test` runs them in Node; `pnpm test:browser` runs the same suites plus browser-only host tests in a real Chromium through Vite and Playwright.

F# game code should use the `Mibo.*` namespaces directly and skip the exports wrapper — Fable compiles it all together.

## Usage

```fsharp
open Mibo.Fable.Exports

let runner = createRunnerWithTick init update (fun dtMs -> Tick dtMs) 480 320

let rec loop () =
    stepFrame 16.6 runner          // advance the simulation one frame
    draw (model runner)            // paint the model however you like
    Browser.Dom.window.requestAnimationFrame(fun _ -> loop ()) |> ignore
```

## Origin

Mibo.Fable started as a web-first port of [Mibo](https://github.com/AngelMunoz/Mibo)'s simulation core (the Elmish loop, input, layout, and draw command types from `Mibo.Core`). It is maintained separately from Mibo on purpose: Mibo targets native backends (raylib, MonoGame), while Mibo.Fable targets the web and adapts the core to Fable's runtime — own vector types, no .NET-only primitives. See [AGENTS.md](AGENTS.md) for the constraints that keep `src/Mibo.Fable/` Fable-compatible.

## Build and demo

| Script            | What it does                                                     |
| ----------------- | ---------------------------------------------------------------- |
| `pnpm build`      | Compiles the library to JavaScript (`src/Mibo.Fable/Mibo.Fable.fs.js` + `src/Mibo.Fable/fable_modules/`) |
| `pnpm demo`       | Watches the demo and serves it with Vite at http://localhost:5173 |
| `pnpm build:demo` | Builds the demo production bundle in `demo/dist/`                 |
| `pnpm test`       | Compiles and runs the smoke suites in `tests/` under Node         |
| `pnpm test:browser` | Runs the smoke suites plus browser-only host tests in a real Chromium |
