namespace Mibo.Elmish

open System
open Mibo.Diagnostics
open Mibo.Windowing

[<Struct>]
type GameConfig = {
  Width: int
  Height: int
  Title: string
  TargetFPS: int voption
  MinWidth: int voption
  MinHeight: int voption
  Resizable: bool
  WindowMode: WindowMode
}

type Program<'Model, 'Msg> = {
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  Update: 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  UpdateCtx:
    (GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)) voption
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  Config: (GameConfig -> GameConfig) list
  Renderers: (unit -> IRenderer<'Model>) list
  Tick: (GameTime -> 'Msg) voption
  FixedStep: FixedStepConfig<'Msg> voption
  DispatchMode: DispatchMode
  AssetsBasePath: string voption
  HasInput: bool
  HasInputMapper: bool
  ServiceRegistrations: (GameContext -> unit) list
  Profiler: FrameProfiler voption
}
