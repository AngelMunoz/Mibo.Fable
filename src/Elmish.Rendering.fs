namespace Mibo.Elmish

open System
open System.Collections.Generic

type GameContext internal (width: int, height: int) =
  let services = Dictionary<Type, obj>()
  let mutable windowWidth = width
  let mutable windowHeight = height

  member val Services = services

  member _.WindowWidth = windowWidth

  member _.WindowHeight = windowHeight

  member internal ctx.UpdateDimensions(w: int, h: int) =
    windowWidth <- w
    windowHeight <- h

type IRenderer<'Model> =
  abstract Draw: GameContext * 'Model * GameTime -> unit

type RenderBuffer<'Key, 'Cmd when 'Key: comparison>
  (?capacity: int, ?keyComparer: IComparer<'Key>) =

  let initialCapacity = defaultArg capacity 1024

  let mutable items = Array.zeroCreate initialCapacity

  let mutable count = 0
  let mutable clearCounter = 0
  let mutable disposed = false
  let keyComparer = defaultArg keyComparer Comparer<'Key>.Default

  let sortComparer =
    { new IComparer<struct ('Key * 'Cmd)> with
        member _.Compare(struct (k1, _), struct (k2, _)) =
          keyComparer.Compare(k1, k2)
    }

  let ensureCapacity(needed: int) =
    if count + needed > items.Length then
      let newSize = max (items.Length * 2) (count + needed)

      let newArr = Array.zeroCreate newSize
      Array.blit items 0 newArr 0 count
      items <- newArr

  member _.Clear() =
    count <- 0
    clearCounter <- clearCounter + 1

    if clearCounter >= 300 then
      clearCounter <- 0
      Array.fill items 0 items.Length Unchecked.defaultof<struct ('Key * 'Cmd)>

  member _.Add(key: 'Key, cmd: 'Cmd) =
    ensureCapacity 1
    items[count] <- struct (key, cmd)
    count <- count + 1

  member _.Sort() =
    let view = Array.sub items 0 count

    Array.sortInPlaceWith
      (fun (struct (k1, _)) (struct (k2, _)) -> keyComparer.Compare(k1, k2))
      view

    Array.blit view 0 items 0 count

  member _.Count = count

  member _.Item(i) = items[i]

  member _.Dispose() =
    if not disposed then
      disposed <- true

  interface IDisposable with
    member this.Dispose() = this.Dispose()
