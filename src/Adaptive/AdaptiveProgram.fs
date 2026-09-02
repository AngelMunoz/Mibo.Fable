module Mibo.Fable.Adaptive.AdaptiveProgram

open System
open Mibo.Elmish

let mkProgram
  (init: AdaptiveFrameContext -> AdaptiveInit<'Frame>)
  (update: AdaptiveContext -> GameTime -> unit)
  : AdaptiveProgram<'Frame> =
  {
    Init = init
    Update = update
    Observers = []
    FixedStep = ValueNone
  }

/// Adds an observer notified with the forced frame after every step.
let withObserver
  (factory: unit -> IObserver<struct (GameContext * 'Frame * GameTime)>)
  (program: AdaptiveProgram<'Frame>)
  : AdaptiveProgram<'Frame> =
  {
    program with
        Observers = factory :: program.Observers
  }

let withFixedStep
  (cfg: AdaptiveFixedStepConfig)
  (program: AdaptiveProgram<'Frame>)
  : AdaptiveProgram<'Frame> =
  if cfg.StepSeconds <= 0.0f then
    invalidArg (nameof cfg.StepSeconds) "StepSeconds must be > 0"

  if cfg.MaxStepsPerFrame <= 0 then
    invalidArg (nameof cfg.MaxStepsPerFrame) "MaxStepsPerFrame must be > 0"

  {
    program with
        FixedStep = ValueSome cfg
  }
