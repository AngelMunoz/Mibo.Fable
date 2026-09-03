namespace Mibo.Elmish

open System
open Mibo.Diagnostics

type HeadlessRunner<'Model, 'Msg>
  (program: HeadlessProgram<'Model, 'Msg>, ?width: int, ?height: int) =

  let loop = ElmishLoop.create(HeadlessProgram.toLoopCore program)

  // Set in the constructor do block. Resolved once so Step does no lookup.
  let mutable profilerOpt: FrameProfiler voption = ValueNone

  let observers =
    ResizeArray<IObserver<struct (GameContext * 'Model * GameTime)>>()

  let mutable gameTime = {
    TotalTime = TimeSpan.Zero
    ElapsedGameTime = TimeSpan.Zero
  }

  let w = defaultArg width 800
  let h = defaultArg height 600

  do
    let ctx = GameContext.create(w, h)

    // Registered before Init so user init code sees it. Only a profiler
    // supplied through withProfiler runs; without one nothing is measured.
    program.Profiler
    |> ValueOption.iter(fun profiler ->
      GameContext.register<FrameProfiler> profiler ctx)

    profilerOpt <- program.Profiler

    loop.Init(ctx)

    // Observers are stored by prepending (see withObserver), so reverse to
    // initialize and notify in registration order — matching Renderers,
    // Config, and ServiceRegistrations.
    for factory in List.rev program.Observers do
      observers.Add(factory())

  member _.ShouldQuit = loop.ShouldQuit

  member _.Model = loop.Model

  member _.GameTime = gameTime

  member _.Dispatch(msg: 'Msg) = loop.Dispatch(msg)

  member _.DispatchMany(msgs: 'Msg seq) =
    for msg in msgs do
      loop.Dispatch(msg)

  member _.Step(elapsed: TimeSpan) =
    if loop.ShouldQuit then
      ()
    else

      let elapsed = if elapsed < TimeSpan.Zero then TimeSpan.Zero else elapsed

      gameTime <- {
        TotalTime = gameTime.TotalTime + elapsed
        ElapsedGameTime = elapsed
      }

      match profilerOpt with
      | ValueSome profiler ->
        profiler.BeginFrame()
        loop.TickFrame(elapsed, gameTime) |> ignore
        profiler.EndUpdate()
      | ValueNone -> loop.TickFrame(elapsed, gameTime) |> ignore

      match loop.Context with
      | ValueSome ctx ->
        for i = 0 to observers.Count - 1 do
          observers[i].OnNext(ctx, loop.Model, gameTime)
      | ValueNone -> ()

  member this.StepN(count: int, elapsed: TimeSpan) =
    for _ = 1 to count do
      this.Step elapsed


  member this.StepUntil
    (predicate: 'Model -> bool, elapsed: TimeSpan, [<Struct>] ?maxFrames: int)
    =
    let max = defaultValueArg maxFrames 10000
    let mutable steps = 0
    let mutable met = predicate this.Model || this.ShouldQuit

    while steps < max && not met do
      this.Step elapsed
      steps <- steps + 1
      met <- predicate this.Model || this.ShouldQuit

    met

  member _.Dispose() =
    loop.DisposeSubs()

    for i = 0 to observers.Count - 1 do
      match observers[i] with
      | :? IDisposable as d -> d.Dispose()
      | _ -> ()

    observers.Clear()

  interface IDisposable with
    member this.Dispose() = this.Dispose()
