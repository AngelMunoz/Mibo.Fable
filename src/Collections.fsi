namespace Mibo.Elmish

open System.Collections.Generic

[<AutoOpen>]
module KeyValuePatterns =
  /// Struct-returning `KeyValuePair` active pattern. Unlike FSharp.Core's
  /// `KeyValue`, it does not allocate a reference tuple per enumeration item.
  val inline (|KeyValueV|): kvp: KeyValuePair<'K, 'V> -> struct ('K * 'V)

module Dictionary =
  /// Zero-allocation lookup — avoids the `bool * 'T` reference tuple that
  /// F# allocates for the single-argument `IDictionary.TryGetValue` extension.
  val inline tryGetValue:
    key: 'K -> dictionary: IDictionary<'K, 'V> -> 'V voption

  /// Adds the key and value if the key is not present; returns whether the add happened.
  val inline tryAdd:
    key: 'K -> value: 'V -> dictionary: IDictionary<'K, 'V> -> bool

module ReadOnlyDict =
  /// Looks a key up in a read-only dictionary.
  val inline tryGetValue:
    key: 'K -> dictionary: IReadOnlyDictionary<'K, 'V> -> 'V voption

module ResizeArray =
  /// Appends all of `b` to `a` and returns the combined backing array.
  val inline mergeToArray: a: ResizeArray<'T> -> b: ResizeArray<'T> -> 'T[]
