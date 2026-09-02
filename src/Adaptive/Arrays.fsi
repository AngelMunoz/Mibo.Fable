/// Internal. Reference-dropping fill; stands in for the original's
/// Array.Clear calls (clearing only drops references — JS needs no GC
/// prompt — but the calls are kept for parity).
module internal Mibo.Fable.Adaptive.Arrays

/// Reference-dropping fill for <c>len</c> slots of <c>arr</c> from
/// <c>start</c> (the original's Array.Clear parity helper).
val inline clearRange: arr: 'a[] -> start: int -> len: int -> unit
