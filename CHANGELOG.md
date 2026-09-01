# Changelog

## [Unreleased]

### Added

- **Core:** The Mibo simulation core runs on the web. Games write F# with the Elmish loop, headless runner, input, 2D/3D layout, and the draw command DSL, then compile to JavaScript to run headless in the browser or Node. Frontends like three.js or Phaser paint the model through the runner.
- **Signals runtime:** The adaptive State · Projection · Update · Force execution model works on the web. Update functions mutate signal roots in place; each step publishes a readonly frame to observers, so rendering never reads a moving graph. Includes fixed-step sub-stepping with a backlog cap, posted intents for input and events, and `PostTask` for background work whose result re-enters at the next step boundary.
- **MVU runtime:** Long-running work no longer blocks the frame. `Cmd.ofPromise` joins `Cmd.ofAsync` and `Cmd.ofTask`, mapping a promise's result — or its error — to a message on a later frame.
- **Demo:** One bouncing-ball simulation runs under both execution models side by side, with a shared renderer. Each canvas accepts clicks to kick its ball.
- **Tests:** `pnpm test` runs a smoke suite covering both runtimes — step ordering, memoized projections that recompute only when their inputs change, intent and command delivery, async completions, and fixed-step caps.

## [0.0.1] - 2026-09-01

### Added

- Initialized project from the Fable package template.
