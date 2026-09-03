/// <summary>Builders for <see cref="T:Mibo.Elmish.ElmishLoop`2"/>.</summary>
module Mibo.Elmish.ElmishLoop

/// <summary>Creates an <see cref="T:Mibo.Elmish.ElmishLoop`2"/> from a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
val create: core: LoopCore<'Model, 'Msg> -> ElmishLoop<'Model, 'Msg>

/// <summary>Projects a <see cref="T:Mibo.Elmish.Program`2"/> to a <see cref="T:Mibo.Elmish.LoopCore`2"/>.</summary>
val coreOfProgram: program: Program<'Model, 'Msg> -> LoopCore<'Model, 'Msg>
