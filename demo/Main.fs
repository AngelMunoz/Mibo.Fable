module Demo.Main

open Fable.Core
open Browser
open Browser.Types

// ─────────────────────────────────────────────────────────────────────────────
// The page half of the demo. Each execution model runs in its own web worker
// (MvuWorker.fs / AdaptiveWorker.fs); the page transfers each canvas element
// to an OffscreenCanvas so its worker paints straight into the visible
// canvas. The wire carries input only: clicks become plain messages. The
// worker URLs are literal so Vite can find and bundle the worker chunks.
// ─────────────────────────────────────────────────────────────────────────────

type ISimWorker =
  abstract postMessage: data: obj * ?transfer: obj array -> unit

[<Emit("$0.transferControlToOffscreen()")>]
let transferToOffscreen(canvas: HTMLCanvasElement) : obj = jsNative

[<Emit("new Worker(new URL('./MvuWorker.fs.js', import.meta.url), { type: 'module' })")>]
let createMvuWorker() : ISimWorker = jsNative

[<Emit("new Worker(new URL('./AdaptiveWorker.fs.js', import.meta.url), { type: 'module' })")>]
let createAdaptiveWorker() : ISimWorker = jsNative

let mvuCanvas = document.getElementById "stage-mvu" :?> HTMLCanvasElement

let adaptiveCanvas =
  document.getElementById "stage-adaptive" :?> HTMLCanvasElement

let mvuWorker = createMvuWorker()
let adaptiveWorker = createAdaptiveWorker()

let mvuOffscreen = transferToOffscreen mvuCanvas
let adaptiveOffscreen = transferToOffscreen adaptiveCanvas

mvuWorker.postMessage(
  {|
    kind = "init"
    canvas = mvuOffscreen
  |},
  [| mvuOffscreen |]
)

adaptiveWorker.postMessage(
  {|
    kind = "init"
    canvas = adaptiveOffscreen
  |},
  [| adaptiveOffscreen |]
)

mvuCanvas.addEventListener(
  "click",
  fun _ -> mvuWorker.postMessage {| kind = "kick" |}
)

adaptiveCanvas.addEventListener(
  "click",
  fun _ -> adaptiveWorker.postMessage {| kind = "kick" |}
)
