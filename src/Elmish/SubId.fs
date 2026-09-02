namespace Mibo.Elmish

open System
open FSharp.UMX

[<Measure>]
type subId

type SubId = string<subId>

module SubId =
  let inline ofString(value: string) : SubId = UMX.tag<subId> value

  let inline value(id: SubId) : string = UMX.untag id

  /// <summary>
  /// Prefixes a SubId with a namespace for parent-child subscription composition.
  /// </summary>
  /// <example>
  /// <code>
  /// // Creates "Player/moveInput"
  /// SubId.prefix "Player" (SubId.ofString "moveInput")
  /// </code>
  /// </example>
  let inline prefix (prefix: string) (id: SubId) : SubId =
    if String.IsNullOrEmpty(prefix) then
      id
    else
      let idStr = value id

      if String.IsNullOrEmpty(idStr) then
        ofString prefix
      else
        ofString(prefix + "/" + idStr)
