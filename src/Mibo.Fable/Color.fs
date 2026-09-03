namespace Mibo

// ─────────────────────────────────────────────────────────────────────────────
// Backend-neutral Color type.
//
// A simple byte RGBA color struct shared across all backends. Both raylib and
// MonoGame define their own Color structs with the same R/G/B/A byte layout —
// this type lets shared code (light definitions, camera configs, etc.) express
// colors without referencing either backend.
//
// Each backend provides inlineable conversion helpers (op_Implicit / explicit)
// to/from its native Color type at the Core↔backend boundary.
// ─────────────────────────────────────────────────────────────────────────────

[<Struct>]
type Color = { R: byte; G: byte; B: byte; A: byte }
