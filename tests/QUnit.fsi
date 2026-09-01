module Mibo.Testing.QUnit

open Fable.Core

/// <summary>QUnit assert API bound to the <c>qunit</c> npm package (Node).</summary>
/// <remarks>
/// Adapted from Perla.Fable.QUnit, retargeted from the browser global to a
/// named <c>QUnit</c> import so tests run under Node with the qunit CLI.
/// </remarks>
type Assert =

  /// <summary>Fails the test when <paramref name="value"/> is not truthy.</summary>
  [<Emit("$0.ok($1, $2...)")>]
  abstract ok: value: obj * ?message: string -> unit

  /// <summary>Fails the test when <paramref name="value"/> is truthy.</summary>
  [<Emit("$0.notOk($1, $2...)")>]
  abstract notOk: value: obj * ?message: string -> unit

  /// <summary>Loose (==) comparison of two values.</summary>
  [<Emit("$0.equal($1, $2, $3...)")>]
  abstract equal: actual: obj * expected: obj * ?message: string -> unit

  /// <summary>Loose (!=) comparison of two values.</summary>
  [<Emit("$0.notEqual($1, $2, $3...)")>]
  abstract notEqual: actual: obj * expected: obj * ?message: string -> unit

  /// <summary>Strict (===) comparison of two values.</summary>
  [<Emit("$0.strictEqual($1, $2, $3...)")>]
  abstract strictEqual: actual: obj * expected: obj * ?message: string -> unit

  /// <summary>Strict (!==) comparison of two values.</summary>
  [<Emit("$0.notStrictEqual($1, $2, $3...)")>]
  abstract notStrictEqual:
    actual: obj * expected: obj * ?message: string -> unit

  /// <summary>Recursive structural comparison of two values.</summary>
  [<Emit("$0.deepEqual($1, $2, $3...)")>]
  abstract deepEqual: actual: obj * expected: obj * ?message: string -> unit

  /// <summary>Inverse recursive structural comparison of two values.</summary>
  [<Emit("$0.notDeepEqual($1, $2, $3...)")>]
  abstract notDeepEqual: actual: obj * expected: obj * ?message: string -> unit

  /// <summary>Fails the test when the callback does not throw.</summary>
  [<Emit("$0.throws($1, $2...)")>]
  abstract throws: block: (unit -> unit) * ?expected: obj -> unit

  /// <summary>Fails the test when the callback throws.</summary>
  [<Emit("$0.doesNotThrow($1, $2...)")>]
  abstract doesNotThrow: block: (unit -> unit) * ?expected: obj -> unit

  /// <summary>Unconditionally fails the test.</summary>
  [<Emit("$0.fail($1...)")>]
  abstract fail: ?message: string -> unit

  /// <summary>Overrides the default assertion count for the current test.</summary>
  [<Emit("$0.expect($1)")>]
  abstract expect: count: int -> unit

  /// <summary>Passes the test with an optional message.</summary>
  [<Emit("$0.true($1, $2...)")>]
  abstract pass: ?message: string -> unit

/// <summary>QUnit's module hook registration for before/after and beforeEach/afterEach.</summary>
type ModuleHooks =
  /// <summary>Registers a hook run once before the module's tests.</summary>
  abstract before: callback: (unit -> unit) -> unit
  /// <summary>Registers a hook run once after the module's tests.</summary>
  abstract after: callback: (unit -> unit) -> unit
  /// <summary>Registers a hook run before each test in the module.</summary>
  abstract beforeEach: callback: (unit -> unit) -> unit
  /// <summary>Registers a hook run after each test in the module.</summary>
  abstract afterEach: callback: (unit -> unit) -> unit

/// <summary>The QUnit test runner surface, imported from the <c>qunit</c> npm package.</summary>
type QUnit =

  /// <summary>Registers a synchronous test.</summary>
  [<Emit("$0.test($1, $2)")>]
  abstract test: name: string * callback: (Assert -> unit) -> unit

  /// <summary>Registers a test whose callback returns a promise; the test completes when it settles.</summary>
  [<Emit("$0.test($1, $2)")>]
  abstract testAsync:
    name: string * callback: (Assert -> JS.Promise<unit>) -> unit

  /// <summary>Registers a test that is always skipped.</summary>
  [<Emit("$0.skip($1, $2)")>]
  abstract skip: name: string * callback: (Assert -> unit) -> unit

  /// <summary>Registers a test that fails if any assertion passes.</summary>
  [<Emit("$0.todo($1, $2)")>]
  abstract todo: name: string * callback: (Assert -> unit) -> unit

  /// <summary>Registers a test expected to fail.</summary>
  [<Emit("$0.expectedToFail($1, $2)")>]
  abstract expectedToFail: name: string * callback: (Assert -> unit) -> unit

  /// <summary>Groups tests and registers module-level hooks.</summary>
  [<Emit("$0.module($1, $2)")>]
  abstract module': name: string * hooks: (ModuleHooks -> unit) -> unit

  /// <summary>Groups tests under a module without hooks.</summary>
  [<Emit("$0.module($1)")>]
  abstract ``module``: name: string -> unit

/// <summary>The QUnit singleton exported by the <c>qunit</c> npm package.</summary>
/// <remarks>
/// On Node the package exports a named <c>QUnit</c>; the qunit CLI discovers
/// registered tests in imported modules and drives the run (exit code included).
/// </remarks>
[<Import("QUnit", "qunit")>]
val QUnit: QUnit
