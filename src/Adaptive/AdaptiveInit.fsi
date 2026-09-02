/// <summary>Helpers for building a <see cref="T:Mibo.Fable.Adaptive.AdaptiveInit`1"/>.</summary>
module Mibo.Fable.Adaptive.AdaptiveInit

open System

/// Creates an init from a frame builder - no disposables.
val inline ofFrameBuilder:
  frameBuilder: (unit -> 'Frame) -> AdaptiveInit<'Frame>

/// Appends disposables released when the runner is disposed.
val inline withDisposables:
  disposables: IDisposable list ->
  init: AdaptiveInit<'Frame> ->
    AdaptiveInit<'Frame>

/// Adds a single disposable.
val inline withDisposable:
  disposable: IDisposable -> init: AdaptiveInit<'Frame> -> AdaptiveInit<'Frame>
