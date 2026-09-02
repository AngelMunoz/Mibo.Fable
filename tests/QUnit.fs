module Mibo.Testing.QUnit

// Adapted from Perla.Fable.QUnit: the browser `QUnit` global becomes a named
// import from the `qunit` npm package so the same surface runs under Node
// with the qunit CLI. Emit attributes are duplicated from the signature file.

open Fable.Core

type Assert =
  [<Emit("$0.ok($1, $2...)")>]
  abstract ok: value: obj * ?message: string -> unit

  [<Emit("$0.notOk($1, $2...)")>]
  abstract notOk: value: obj * ?message: string -> unit

  [<Emit("$0.equal($1, $2, $3...)")>]
  abstract equal: actual: obj * expected: obj * ?message: string -> unit

  [<Emit("$0.notEqual($1, $2, $3...)")>]
  abstract notEqual: actual: obj * expected: obj * ?message: string -> unit

  [<Emit("$0.strictEqual($1, $2, $3...)")>]
  abstract strictEqual: actual: obj * expected: obj * ?message: string -> unit

  [<Emit("$0.notStrictEqual($1, $2, $3...)")>]
  abstract notStrictEqual:
    actual: obj * expected: obj * ?message: string -> unit

  [<Emit("$0.deepEqual($1, $2, $3...)")>]
  abstract deepEqual: actual: obj * expected: obj * ?message: string -> unit

  [<Emit("$0.notDeepEqual($1, $2, $3...)")>]
  abstract notDeepEqual: actual: obj * expected: obj * ?message: string -> unit

  [<Emit("$0.throws($1, $2...)")>]
  abstract throws: block: (unit -> unit) * ?expected: obj -> unit

  [<Emit("$0.expect($1)")>]
  abstract expect: count: int -> unit

  [<Emit("$0.true($1, $2...)")>]
  abstract pass: ?message: string -> unit

type ModuleHooks =
  abstract before: callback: (unit -> unit) -> unit
  abstract after: callback: (unit -> unit) -> unit
  abstract beforeEach: callback: (unit -> unit) -> unit
  abstract afterEach: callback: (unit -> unit) -> unit

type QUnit =
  [<Emit("$0.test($1, $2)")>]
  abstract test: name: string * callback: (Assert -> unit) -> unit

  [<Emit("$0.test($1, $2)")>]
  abstract testAsync:
    name: string * callback: (Assert -> JS.Promise<unit>) -> unit

  [<Emit("$0.skip($1, $2)")>]
  abstract skip: name: string * callback: (Assert -> unit) -> unit

  [<Emit("$0.todo($1, $2)")>]
  abstract todo: name: string * callback: (Assert -> unit) -> unit

  [<Emit("$0.only($1, $2)")>]
  abstract only: name: string * callback: (Assert -> unit) -> unit

  [<Emit("$0.start($1...)")>]
  abstract start: ?count: int -> unit

  [<Emit("$0.autostart()")>]
  abstract autostart: unit -> unit

  [<Emit("$0.module($1, $2)")>]
  abstract module': name: string * hooks: (ModuleHooks -> unit) -> unit

  [<Emit("$0.module($1)")>]
  abstract ``module``: name: string -> unit

[<Import("QUnit", "qunit")>]
let QUnit: QUnit = jsNative
