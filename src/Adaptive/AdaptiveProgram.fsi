[<RequireQualifiedAccess>]
module Mibo.Fable.Adaptive.AdaptiveProgram

open System
open Mibo.Elmish

/// Creates an adaptive program from an Init and an Update phase.
val mkProgram:
  init: (AdaptiveFrameContext -> AdaptiveInit<'Frame>) ->
  update: (AdaptiveContext -> GameTime -> unit) ->
    AdaptiveProgram<'Frame>

/// Adds an observer notified with the forced frame after every step.
val withObserver:
  factory: (unit -> IObserver<struct (GameContext * 'Frame * GameTime)>) ->
  program: AdaptiveProgram<'Frame> ->
    AdaptiveProgram<'Frame>

/// Enables framework-managed fixed-step sub-stepping.
val withFixedStep:
  cfg: AdaptiveFixedStepConfig ->
  program: AdaptiveProgram<'Frame> ->
    AdaptiveProgram<'Frame>
