module Mibo.Fable.Adaptive.AdaptiveInit

open System

/// Creates an init from a frame builder — no disposables.
let inline ofFrameBuilder(frameBuilder: unit -> 'Frame) : AdaptiveInit<'Frame> = {
  FrameBuilder = frameBuilder
  Disposables = []
}

/// Appends disposables released when the runner is disposed.
let inline withDisposables
  (disposables: IDisposable list)
  (init: AdaptiveInit<'Frame>)
  : AdaptiveInit<'Frame> =
  {
    init with
        Disposables = init.Disposables @ disposables
  }

/// Adds a single disposable.
let inline withDisposable
  (disposable: IDisposable)
  (init: AdaptiveInit<'Frame>)
  : AdaptiveInit<'Frame> =
  {
    init with
        Disposables = disposable :: init.Disposables
  }
