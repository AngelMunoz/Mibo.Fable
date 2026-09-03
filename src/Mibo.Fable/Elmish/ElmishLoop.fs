module Mibo.Elmish.ElmishLoop

let create(core: LoopCore<'Model, 'Msg>) = ElmishLoop<'Model, 'Msg>(core)

/// <summary>Projects a <see cref="T:Mibo.Elmish.Program`2"/> to a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
let coreOfProgram(program: Program<'Model, 'Msg>) : LoopCore<'Model, 'Msg> = {
  Init = program.Init
  Update =
    match program.UpdateCtx with
    | ValueSome update -> update
    | ValueNone -> fun _ctx msg model -> program.Update msg model
  Subscribe = program.Subscribe
  Tick = program.Tick
  FixedStep = program.FixedStep
  DispatchMode = program.DispatchMode
}
