# Changelog

[Unreleased]

# Added

- **Mibo core, web-first:** Brought the Elmish loop, headless runner, input, layout (2D/3D), draw command DSL, colors, and animation types into `src/`, compiled to JavaScript by Fable. Own `Vector2/3/4` structs replace `System.Numerics` (which Fable cannot compile); array pooling, spans, concurrent queues, and byref tricks were replaced with plain F# so everything runs on the web.
- **JS-facing runner API:** `Mibo.Fable.Exports` wraps the headless simulation runner (`createRunnerWithTick`, `stepFrame`, `dispatch`, `model`, …) with plain calls for JavaScript/TypeScript hosts.
- **Demo:** A canvas demo (`pnpm demo`) where a bouncing-ball simulation runs headless in compiled F# while the page only paints the model — the same split a three.js/Phaser frontend would use.

[0.0.1] - 2026-09-01

# Added

- **Initialized project:** Created a new Fable project with the necessary configuration files and dependencies.
