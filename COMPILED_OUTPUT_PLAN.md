# Compiled Output Plan (JS and TS)

Toolchain: Fable 5.15.0, Fable.Core 5.2.0, .NET 8, Node via pnpm.

## Goal

Make the compiled JS and TS output clean and stable.

1. One top-level module per file. No nested modules, no `namespace` + module.
2. F# side erasure: `inline` plus Fable attributes.
3. Clean exported names: no `Module_` mangles, no sub-module prefixes, no hash
   suffixes on the public surface.
4. A sound build flow for both languages.

Non-goals: behavior changes, new features, .NET-only concerns.

## Binding rules

1. **File shape.** Every file starts with the fully-qualified module
   declaration: `module Mibo.Input.ActionState`. Never `namespace Mibo.Input`
   plus a nested `module ActionState =`. The nested form prefixes every export
   (`ActionState_empty`); the module-declaration form exports bare
   (`empty`, `update`). Types live inside the module.
2. **Nodes: no AttachMembers.** Nodes, sinks, buffers, and contexts are not
   meant to be used directly. Consumers go through module functions. No
   AttachMembers on them.
3. **AttachMembers: stable long-lived modules only.** Use AttachMembers for
   things that are explicitly public or internal and long-lived — the stable
   API modules such as the AMap module, the AVal module. AttachMembers makes
   the class one atomic export (all methods bundle together), so never use it
   on internals.
4. **Compile order.** A type signature moves with the module that needs it.
   A member referenced from another file is exported; keep cross-file entry
   points as module functions, never as members with arguments.
5. **Emission anchor.** A module whose every member is `[<Emit>]`-bound or
   inline emits no file. Anchor such a module with one plain non-inline
   function, or consumers import from a missing file.
6. **File modification: the EDIT tool only.** No `sed`, no Python, no shell
   redirection (`>` / `>>`), no `cat` appends, no heredocs against `.fs`,
   `.fsi`, `.fsproj`, or any source file. Scripted edits corrupted files
   during the first pass: truncated `Adaptive.fsi`, duplicated member lines,
   detached attributes, broken indentation. Every modification goes through
   the EDIT tool after Reading the target region, so each change is an
   exact, reviewable string replacement. Shell commands stay limited to
   reads, builds, and tests.

Fable rules that drive this plan (official docs): Fable never renames
functions and values of the first module of a file. Members of later or inner
modules get a prefix. Methods with arguments get a hash suffix. A module named
after a type in the same file gets the `Module` infix.

## Baseline (measured 2026-09-02)

| Metric                                          | Value              |
| ----------------------------------------------- | ------------------ |
| Source files                                    | 62 (all with .fsi) |
| Files with 2 or more top-level modules          | 21                 |
| Inner modules                                   | 9, in 3 files      |
| `Module_` mangles in output                     | 71 (6 unique)      |
| Hash-suffixed names in output                   | 934 (175 unique)   |
| Hashed constructors (`$ctor`)                   | 225                |
| `_arg` binders                                  | 32                 |
| JS outputs importing `../tests/fable_modules`   | 39 before clean; 0 after clean build |

## Phase 0 — build and output hygiene

1. `.gitignore` covers `*.fs.js`, `*.fs.js.map`, `*.fs.ts`, `dist-ts/`,
   `fable_modules/`.
2. `package.json` scripts:

   ```json
   "build:ts": "dotnet fable src/Mibo.Fable --lang ts --sourceMaps -o dist-ts",
   "clean:src": "dotnet fable clean src/Mibo.Fable --yes",
   "clean:ts": "dotnet fable clean dist-ts --yes -e .ts",
   "build:all": "pnpm build && pnpm build:ts"
   ```

3. JS stays in `src/Mibo.Fable/` (workspace default; tests and demo use it). TS goes to
   `dist-ts/`. Never chain both builds into one folder: a TS build into
   `src/Mibo.Fable/` removes the JS runtime library.
4. Any clean also deletes `fable_modules`. Build again after every clean.
5. When CI exists: build both languages, run `pnpm test`.

Acceptance: fresh clone, `pnpm install`, `pnpm build`, `pnpm test` green, no
`../tests` imports in `src/Mibo.Fable/*.fs.js`.

## Phase 1 — one module per file

### Rules for every split

1. File header: `module Mibo.Fable.Adaptive.Arrays` — the fully-qualified
   module name as a top-level module declaration. No `namespace` line.
2. Keep member names and `internal` visibility. Insert new files in
   `Mibo.Fable.fsproj` at the position of the old file. `.fsi` goes before
   `.fs`.
3. Split the matching `.fsi` content into the new signature files.
4. A type signature moves with the module that needs it. If a type is used by
   a module in another file, that module file compiles after the type's file.
5. Every file modification uses the EDIT tool after Reading the target
   region (binding rule 6). No shell text tools.
6. Run `dotnet fantomas .` and `pnpm test` after each file group.
6. All adaptive files live in `src/Mibo.Fable/Adaptive/`. No `Adaptive.` prefix on file
   names: `Adaptive/Arrays.fs`, not `Adaptive.Arrays.fs`. File names are free
   in F#; module names are unchanged.

### File map

Final paths under `src/Mibo.Fable/Adaptive/` unless noted.

| Source file              | New files                                                              |
| ------------------------ | ---------------------------------------------------------------------- |
| `Adaptive.Collections.fs` | 18: `WeakRefs`, `WeakMarkers`, `DeltaBuffer`, `SetDelta`, `MapDelta`, `ListDelta`, `SinkList`, `RefCountedSet`, `SetNodeState`, `MapNodeState`, `ElementEntry`, `CollectionNodes` (node types), `TwoSetState`, `Choose2State`, `CollectEntry`, `CollectState`, `BindSetState`, `BindMapState` |
| `Adaptive.Api.fs`        | 7: `ASet`, `CSet`, `AMap`, `CMap`, `AList`, `CList`, `AListSliceExtensions` |
| `Adaptive.fs`            | core types stay in `Adaptive.fs`; 6 to `Adaptive/`: `Arrays`, `AdaptiveRuntime`, `Posting`, `Transaction`, `AVal`, `CVal` |
| `Adaptive.MapNodes.fs`   | 3 state modules (`SetToMapState`, `SetToMapKeepAllState`, `MapToSetState`); node types stay in `Adaptive/MapNodes.fs` |
| `Adaptive.Headless.fs`   | types stay in `Adaptive/Headless.fs`; `AdaptiveInit`, `AdaptiveProgram` |
| `Adaptive.Changeable.fs` | stays one file as `Adaptive/Changeable.fs`; Phase 2 write functions land here |
| `Collections.fs`         | 4 under `Collections/`: `KeyValuePatterns`, `Dictionary`, `ReadOnlyDict`, `ResizeArray` |
| `InputServices.fs`       | 6 under `InputServices/`: `Input`, `Keyboard`, `Mouse`, `Touch`, `Gamepad`, `Gesture` |
| `Graphics3D/Light3D.fs`  | 4 under `Graphics3D/`: one per light module                            |
| `InputMapper.fs`         | 3: `InputMap`, `ActionState`, `InputMapper`                            |
| `Animation3D.fs`         | 2: `Animation3DClipsInfo`, `Animation3DState`                          |
| `Layout/Spatial2D.fs`    | keep as is: emitted output is already clean (inline members)           |
| `Layout3D/Spatial3D.fs`  | keep as is: same reason                                                |
| remaining Layout pairs   | 2 each: `Layout`, `HexLayout`, `Layered`, `LayeredHex`, `LayeredGrid3D`, `LayeredHex3D`, `HexLayout3D`, `Layout3D` |

Inner modules become top-level modules in their own files. A companion module
(module named after a type in the same file) moves to its own file; F# names
and call sites stay unchanged.

### Split mechanics (empirical rules from the first pass)

1. Cut per top-level declaration, never per module. A module block ends at
   the next column-0 declaration (`module`, `type`, `[<`), and
   namespace-level types interleave with modules. Module-to-module ranges
   swallowed types and broke the parse.
2. Never reuse line numbers across edits. Every insertion or deletion shifts
   the lines. Re-read the anchor lines immediately before each cut. Stale
   numbers produced truncated and garbage files (`InputMapper.fs`,
   `Adaptive.fsi`).
3. `internal` and `private` are part of the declaration line
   (`module internal SetToMapState`). Strip them before matching names, or
   every lookup misses.
4. Compile order is the hard constraint. A type's file must precede every
   file that uses it. Types used only by an extracted module move with it;
   types used by several files move to the earliest consumer.
5. Mutual references tie a module to the types file. `AdaptiveRuntime` and
   `GraphContext` reference each other; the module stays in `Adaptive.fs`.
   F# allows mutual references inside one file only.
6. An attribute line must travel with its declaration. An insertion that
   lands between `[<AutoOpen>]` and `module` detaches the attribute and
   silently disables auto-open (name-resolution failures downstream).
7. A namespace-level type's `private` constructor is invisible to the
   same file's modules (error 801). Factory functions for such types need
   the constructor public, or the type must nest inside a module (the sinks
   nest inside `Collections`, so their `private` constructors work with
   `mk*` functions in the same module).
8. Hiding a constructor through the signature (no `new:` line) requires
   `[<Sealed>]` or `[<Class>]` on both the signature and the implementation,
   with the attribute indentation matching the nesting (column 2 inside a
   module, column 0 at namespace level).
9. Static members are members: they emit hash-suffixed exports. Only
   module-level `let` functions are hash-free. The write API lives in module
   functions that call the members in-file; a member referenced only within
   its own file is emitted file-local and not exported.
10. `let inline` wrappers that call members push the member import into the
    consumer's file. The delegation target must be a non-inline module
    function in the class's file, never the member itself.
11. Shape-affecting attributes (`AttachMembers`) are read from the
    implementation, but consumers compile against the signature: the
    attribute must appear in both files, or the consumers' compiled imports
    keep the old shape.
12. Extraction targets can collide with the source file name (module
    `LayeredGrid3D` out of `Layout3D/LayeredGrid3D.fs`). Check the target
    path before writing, or swap which module stays.

### Files with more nuances than expected

| File | Nuance |
| --- | --- |
| `Adaptive.Collections.fs` | 12 modules and 6 nested state modules interleaved with module `Collections`' own functions. Each nested state module moved together with its state record; node types stayed. 18 files. |
| `Adaptive.MapNodes.fs` | State record types sit between node types; each state record moved with its state module (`SetToMapState`, `SetToMapKeepAllState`, `MapToSetState`). |
| `Adaptive.fs` | `AdaptiveRuntime` and `GraphContext` reference each other; the module stays in the types file. `Arrays` compiles before the types (the types call the `Arrays` helpers). |
| `InputMapper.fs` | Three types, each tied to one module: `Trigger`+`InputMap` together, `ActionState` alone, `IInputMapper` stays with `InputMapper`. |
| `Elmish.Subscriptions.fs` | The `subId` measure and the `SubId` abbreviation move with the `SubId` module. |
| `Layout/Layout.fs`, `Layout3D/Layout3D.fs`, `Layout3D/HexLayout3D.fs`, `Layout/HexLayout.fs` | The section type (`GridSection2D`, `GridSection3D`, `HexGrid3DSection`) moves into the `*Helpers` file — the earliest consumer — and `[<AutoOpen>]` travels with it. |
| `Color.fs`, `Elmish.ProgramTypes.fs`, `Elmish.Rendering.fs`, `Adaptive.SinkList` | Companion modules (module named after the type) move to their own files; F# names and call sites stay unchanged. |
| `Layout3D/LayeredGrid3D.fs` | The module `LayeredGrid3D` shares the source file name; the kept module swapped to avoid the collision. |

Acceptance:

- `grep -c "Module_" src/Mibo.Fable/*.fs.js` returns 0.
- Public names carry no sub-module prefix (`empty` instead of `ASet_empty`).
- `pnpm test` green, `pnpm build:ts` green, demo builds.

## Phase 2 — clean public member names

F# gives each method with arguments a signature hash in its compiled name.
Fable keeps it. Members that implement interfaces stay members.

1. Members with arguments become module-level functions. Hash-free.
2. Getters stay properties. `get_` names carry no hash.
3. Public constructors get factory functions. This removes `$ctor` hashes
   from the consumer surface.
4. Keep thin members where F# pipeline syntax matters. JS users take the
   function form. The entry module `Mibo.Fable.fs` shows the pattern.

Acceptance: no hash-suffixed name on the exported surface of
`Mibo.Fable.fs.js`, the `Adaptive.Api*` outputs, and the module files.
Internal plumbing may keep hashes.

## Phase 3 — erasure and TS unions

1. `[<Erase>]` for JS-facing structural types and unions when you add them.
2. `[<StringEnum>]` for string enums when you add them.
3. Trial `[<TypeScriptTaggedUnion("type")>]` on a union only if a suitable
   union exists. It changes the runtime shape (string tags, named fields as
   properties) and `match` compiles to string compares. Test both outputs.

## Phase 4 — verification matrix

| Check              | Command or probe                                         | Expected |
| ------------------ | -------------------------------------------------------- | -------- |
| F# format          | `dotnet fantomas .`                                      | no diff  |
| JS build           | `pnpm build`                                             | 0 errors |
| TS build           | `pnpm build:ts`                                          | 0 errors |
| Tests              | `pnpm test`                                              | green (58) |
| Demo               | `pnpm build:demo`                                        | green    |
| `Module_` mangles  | `grep -c "Module_" src/Mibo.Fable/*.fs.js`                          | 0        |
| Sub-module prefixes| `grep -E "^export function (ASet\|CSet\|AMap\|CMap\|AList\|CList\|AVal\|CVal)_"` | 0 |
| Public hash names  | grep entry and Api exports for `_[0-9A-Z]{5}`            | 0        |
| Library path       | `grep "../tests/fable_modules" src/Mibo.Fable/*.fs.js`              | 0        |

Every phase ends with this matrix. Each phase ships value on its own.

## Suggested order

| Phase | Content                        |
| ----- | ------------------------------ |
| 0     | hygiene                        |
| 1     | one module per file + fsi work |
| 2     | public member names            |
| 3     | erasure and TS unions          |
