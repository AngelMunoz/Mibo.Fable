namespace Mibo.Elmish

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Mibo.Diagnostics

// ─────────────────────────────────────────────────────────────────────────────
// The shared Elmish message-processing loop.
//
// Both RaylibGame (the windowed host) and HeadlessRunner (the headless host)
// run the exact same message-processing core: a DispatchQueue, execCmd,
// updateSubs, deferred-effect draining, FixedStep accumulation, tick dispatch,
// and the StartBatch/TryDequeue/EndBatch message pump. This type captures that
// shared core so the two hosts become thin I/O shells around it.
//
// A host constructs an ElmishLoop from a LoopCore (the six fields that define
// message-processing behavior) and then:
//   1. calls Init(ctx) once after registering backend services
//   2. calls TickFrame(dt, gameTime) each frame
//   3. reads Model / ShouldQuit / GameTime as needed
// ─────────────────────────────────────────────────────────────────────────────

type internal DispatchQueue<'Msg>(mode: DispatchMode) =
  let gate = obj()
  let mutable isProcessing = false
  let mutable current = System.Collections.Generic.Queue<'Msg>()
  let mutable next = System.Collections.Generic.Queue<'Msg>()

  member _.Mode = mode

  member _.Dispatch(msg: 'Msg) =
    match mode with
    | Immediate -> current.Enqueue(msg)
    | FrameBounded ->
      lock gate (fun () ->
        if isProcessing then
          next.Enqueue(msg)
        else
          current.Enqueue(msg))

  member _.StartBatch() =
    match mode with
    | Immediate -> ()
    | FrameBounded -> lock gate (fun () -> isProcessing <- true)

  member _.EndBatch() =
    match mode with
    | Immediate -> ()
    | FrameBounded ->
      lock gate (fun () ->
        isProcessing <- false
        let tmp = current
        current <- next
        next <- tmp)

  member _.TryDequeue() : 'Msg voption =
    if current.Count > 0 then
      ValueSome(current.Dequeue())
    else
      ValueNone

[<Struct>]
type LoopCore<'Model, 'Msg> = {
  Init: GameContext -> struct ('Model * Cmd<'Msg>)
  Update: GameContext -> 'Msg -> 'Model -> struct ('Model * Cmd<'Msg>)
  Subscribe: GameContext -> 'Model -> Sub<'Msg>
  Tick: (GameTime -> 'Msg) voption
  FixedStep: FixedStepConfig<'Msg> voption
  DispatchMode: DispatchMode
}

type ElmishLoop<'Model, 'Msg> internal (core: LoopCore<'Model, 'Msg>) =

  let msgQueue = DispatchQueue<'Msg>(core.DispatchMode)
  let mutable state: 'Model = Unchecked.defaultof<'Model>
  let mutable ctxOpt: GameContext voption = ValueNone
  let activeSubs = Dictionary<SubId, IDisposable>()
  let subIdsInUse = HashSet<SubId>()
  let subIdsToRemove = ResizeArray<SubId>(32)
  let subBuffer = ResizeArray<struct (SubId * Subscribe<'Msg>)>()
  let subStack = ResizeArray<Sub<'Msg>>()
  let deferredEffs = ResizeArray<Effect<'Msg>>(64)
  let deferredEffsRun = ResizeArray<Effect<'Msg>>(64)
  let mutable fixedAccSeconds = 0.0f
  let mutable shouldQuit = false

  // Resolved once in Init so the per frame path does no dictionary lookup.
  let mutable profilerOpt: FrameProfiler voption = ValueNone

  let dispatch(msg: 'Msg) = msgQueue.Dispatch(msg)

  let execCmd(cmd: Cmd<'Msg>) =
    match cmd with
    | Empty -> ()
    | Msg msg -> dispatch msg
    | Quit -> shouldQuit <- true
    | Single eff -> eff.Invoke(dispatch)
    | Batch effs ->
      for i = 0 to effs.Length - 1 do
        effs[i].Invoke(dispatch)
    | DeferNextFrame effs -> deferredEffs.AddRange(effs)
    | NowAndDeferNextFrame(now, next) ->
      for i = 0 to now.Length - 1 do
        now[i].Invoke(dispatch)

      deferredEffs.AddRange(next)

  let updateSubs(ctx: GameContext) =
    subBuffer.Clear()
    subStack.Clear()
    subStack.Add(core.Subscribe ctx state)
    Sub.flatten subStack subBuffer

    subIdsInUse.Clear()
    subIdsToRemove.Clear()

    for id, subscribeFn in subBuffer do
      subIdsInUse.Add(id) |> ignore

      if not(activeSubs.ContainsKey(id)) then
        try
          activeSubs.Add(id, subscribeFn dispatch)
        with ex ->
          Console.WriteLine($"Error starting sub {SubId.value id}: {ex}")

    for KeyValueV(key, _disp) in activeSubs do
      if not(subIdsInUse.Contains(key)) then
        subIdsToRemove.Add(key)

    for i = 0 to subIdsToRemove.Count - 1 do
      let key = subIdsToRemove[i]

      match Dictionary.tryGetValue key activeSubs with
      | ValueSome disp ->
        disp.Dispose()
        activeSubs.Remove(key) |> ignore
      | ValueNone -> ()

  member _.ShouldQuit = shouldQuit

  member _.Model = state

  member _.Context = ctxOpt

  member internal _.ActiveSubs = activeSubs

  member _.Dispatch(msg: 'Msg) = dispatch msg

  member _.Init(ctx: GameContext) =
    ctxOpt <- ValueSome ctx
    profilerOpt <- GameContext.tryGetService<FrameProfiler> ctx
    let struct (initialState, initialCmds) = core.Init ctx
    state <- initialState
    execCmd initialCmds
    updateSubs ctx

  member _.TickFrame(elapsed: TimeSpan, gameTime: GameTime) : bool =
    let deltaSeconds = float32 elapsed.TotalSeconds

    if deferredEffs.Count <> 0 then
      deferredEffsRun.Clear()
      deferredEffsRun.AddRange(deferredEffs)
      deferredEffs.Clear()

      for i = 0 to deferredEffsRun.Count - 1 do
        deferredEffsRun[i].Invoke(dispatch)

    match core.FixedStep with
    | ValueNone ->
      match profilerOpt with
      | ValueSome profiler -> profiler.AddSimSteps(1, false)
      | ValueNone -> ()
    | ValueSome cfg ->
      let maxFrame = cfg.MaxFrameSeconds |> ValueOption.defaultValue 0.25f

      let struct (acc2, steps, dropped) =
        FixedStep.compute
          cfg.StepSeconds
          cfg.MaxStepsPerFrame
          maxFrame
          fixedAccSeconds
          deltaSeconds

      fixedAccSeconds <- acc2

      match profilerOpt with
      | ValueSome profiler -> profiler.AddSimSteps(steps, dropped)
      | ValueNone -> ()

      for _i = 1 to steps do
        dispatch(cfg.Map cfg.StepSeconds)

    core.Tick |> ValueOption.iter(fun map -> dispatch(map gameTime))

    let mutable stateChanged = false
    let mutable msg = Unchecked.defaultof<'Msg>

    match ctxOpt with
    | ValueSome ctx ->
      msgQueue.StartBatch()

      let mutable moreMsgs = true

      while moreMsgs do
        match msgQueue.TryDequeue() with
        | ValueSome m ->
          msg <- m
          let struct (newState, cmds) = core.Update ctx msg state
          state <- newState
          execCmd cmds
          stateChanged <- true
        | ValueNone -> moreMsgs <- false

      msgQueue.EndBatch()
    // TickFrame before Init: keep the messages queued until a context exists.
    | ValueNone -> ()

    if stateChanged then
      ctxOpt |> ValueOption.iter(updateSubs)

    stateChanged

  member _.DisposeSubs() =
    for KeyValueV(_key, disp) in activeSubs do
      disp.Dispose()

    activeSubs.Clear()
