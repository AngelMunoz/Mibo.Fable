namespace Mibo.Elmish

/// <summary>
/// Generic system pipeline for composing frame updates with type-enforced snapshot boundaries.
/// </summary>
/// <remarks>
/// This module provides a pipeline pattern where mutable systems run first (physics,
/// particles), then a snapshot is taken, and readonly systems run on the snapshot.
/// The type system enforces this ordering at compile time.
/// <para>
/// The pipeline accumulates a single <see cref="T:Mibo.Elmish.Cmd`1" /> (not a list) to keep it fast:
/// no list appends, no reversing, and no quadratic behavior as you add phases.
/// </para>
/// <para>Pattern Overview:</para>
/// <ol>
/// <li><see cref="M:Mibo.Elmish.System.start"/> - Begin with mutable model</li>
/// <li><see cref="M:Mibo.Elmish.System.pipeMutable"/> - Run systems that mutate (physics, AI movement)</li>
/// <li><see cref="M:Mibo.Elmish.System.snapshot"/> - Take readonly snapshot (type changes from Model to Snapshot)</li>
/// <li><see cref="M:Mibo.Elmish.System.pipe"/> - Run readonly systems (rendering prep, queries)</li>
/// <li><see cref="M:Mibo.Elmish.System.finish"/> - Convert back to model and return the accumulated command</li>
/// </ol>
/// </remarks>
/// <example>
/// <code>
/// let updateSystems model =
///     model
///     |&gt; System.start
///     |&gt; System.pipeMutable physicsSystem
///     |&gt; System.pipeMutable particleSystem
///     |&gt; System.snapshot Model.toSnapshot
///     |&gt; System.pipe aiDecisionSystem
///     |&gt; System.finish Model.fromSnapshot
/// </code>
/// </example>
module System =

  /// Start pipeline with mutable model.
  val inline start: model: 'Model -> struct ('Model * Cmd<'Msg>)

  /// Combines two commands, preserving Empty and deferring to Cmd.batch2 otherwise.
  val inline combine: a: Cmd<'Msg> -> b: Cmd<'Msg> -> Cmd<'Msg>

  /// <summary>Pipe a mutable system (pre-snapshot phase).</summary>
  /// <remarks>Use for systems that need to mutate model state (physics, particles). The system receives the model and returns updated model with commands.</remarks>
  val inline pipeMutable:
    system: ('Model -> struct ('Model * Cmd<'Msg>)) ->
    struct ('Model * Cmd<'Msg>) ->
      struct ('Model * Cmd<'Msg>)

  /// <summary>Transition from mutable Model to readonly Snapshot.</summary>
  /// <remarks>This is the "barrier" in the pipeline. After calling snapshot, only readonly systems (using <see cref="M:Mibo.Elmish.System.pipe"/>) can be added.</remarks>
  /// <example>
  /// <code>
  /// |&gt; System.snapshot Model.toSnapshot
  /// </code>
  /// </example>
  val inline snapshot:
    toSnapshot: ('Model -> 'Snapshot) ->
    struct ('Model * Cmd<'Msg>) ->
      struct ('Snapshot * Cmd<'Msg>)

  /// <summary>Pipe a readonly system (post-snapshot phase).</summary>
  /// <remarks>Use for systems that read state but don't mutate it (rendering prep, AI decisions that only emit commands, query systems).</remarks>
  val inline pipe:
    system: ('Snapshot -> struct ('Snapshot * Cmd<'Msg>)) ->
    struct ('Snapshot * Cmd<'Msg>) ->
      struct ('Snapshot * Cmd<'Msg>)

  /// <summary>Dispatch commands based on snapshot state.</summary>
  /// <remarks>
  /// Use for systems that produce messages without modifying state.
  /// The snapshot passes through unchanged.
  /// </remarks>
  /// <example>
  /// <code>
  /// |&gt; System.dispatch (fun snap -&gt;
  ///     if snap.Health &lt;= 0f then Cmd.ofMsg PlayerDied else Cmd.none)
  /// </code>
  /// </example>
  val inline dispatch:
    dispatcher: ('Snapshot -> Cmd<'Msg>) ->
    struct ('Snapshot * Cmd<'Msg>) ->
      struct ('Snapshot * Cmd<'Msg>)

  /// <summary>Dispatch commands with input selection from snapshot.</summary>
  /// <remarks>
  /// <para>Use for encapsulated/autonomous subsystems that:</para>
  /// <list type="bullet">
  /// <item>Manage their own internal state (via closure)</item>
  /// <item>Receive optional input derived from the snapshot</item>
  /// <item>Dispatch commands to communicate with the parent</item>
  /// </list>
  /// <para>
  /// The selector extracts relevant data from the snapshot, allowing the dispatcher
  /// to remain decoupled from the parent's model structure.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Autonomous subsystem with internal state (closure)
  /// let healthTracker : HealthInput voption -&gt; ModelSnapshot -&gt; Cmd&lt;Msg&gt; =
  ///     let mutable hp = 100f
  ///     fun input snap -&gt;
  ///         input |&gt; ValueOption.iter (function
  ///             | TakeDamage amt -&gt; hp &lt;- hp - amt
  ///             | Heal amt -&gt; hp &lt;- min 100f (hp + amt))
  ///         if hp &lt;= 0f then Cmd.ofMsg PlayerDied else Cmd.none
  ///
  /// // Selector bridges parent snapshot to subsystem input
  /// let selectHealthInput snap =
  ///     if snap.PlayerWasHit then ValueSome (TakeDamage snap.DamageAmount)
  ///     else ValueNone
  ///
  /// // Usage in pipeline
  /// |&gt; System.dispatchWith selectHealthInput healthTracker
  /// </code>
  /// </example>
  val inline dispatchWith:
    selectInput: ('Snapshot -> 'Input voption) ->
    dispatcher: ('Input voption -> 'Snapshot -> Cmd<'Msg>) ->
    struct ('Snapshot * Cmd<'Msg>) ->
      struct ('Snapshot * Cmd<'Msg>)

  /// <summary>Finish pipeline: convert snapshot back to model and return the accumulated command.</summary>
  /// <remarks>Returns a tuple ready for the Elmish update function.</remarks>
  val inline finish:
    fromSnapshot: ('Snapshot -> 'Model) ->
    struct ('Snapshot * Cmd<'Msg>) ->
      struct ('Model * Cmd<'Msg>)
