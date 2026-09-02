namespace Mibo.Elmish

open System
open Mibo.Diagnostics

type HeadlessProgram<'Model, 'Msg> = {
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  Update: 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  UpdateCtx:
    (GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) voption
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  Tick: (GameTime -> 'Msg) voption
  FixedStep: FixedStepConfig<'Msg> voption
  DispatchMode: DispatchMode
  Observers: (unit -> IObserver<struct (GameContext * 'Model * GameTime)>) list
  Profiler: FrameProfiler voption
}
