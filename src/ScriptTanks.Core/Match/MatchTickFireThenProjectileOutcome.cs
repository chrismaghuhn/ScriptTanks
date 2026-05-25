using System;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable result of
/// <see cref="MatchTickFireThenProjectilePipeline.Step(MatchState, MatchFireRequest?)"/>.
/// Bundles the final post-tick <see cref="MatchState"/>, the optional
/// fire-resolution outcome, and a flag indicating whether the caller
/// supplied a <see cref="MatchFireRequest"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is a pure data carrier. It does not run gameplay systems,
/// advance ticks, mutate match state, allocate projectile ids, derive muzzle
/// positions or fire velocities, or queue intents.
/// </para>
/// <para>
/// State distinction:
/// <list type="bullet">
///   <item><description>
///     <see cref="FireOutcome"/>'s <see cref="MatchStateFireOutcome.UpdatedState"/>
///     is the **immediate post-fire** snapshot - the new state right after
///     <see cref="MatchStateFireSystem.ResolveFire(MatchState, MatchFireRequest, ScriptTanks.Core.Simulation.SimTick)"/>,
///     **before** the projectile pipeline and tick advance ran. Its
///     <see cref="MatchState.CurrentTick"/> equals the input tick.
///   </description></item>
///   <item><description>
///     <see cref="UpdatedState"/> is the **post-fire + post-projectile-pipeline +
///     post-tick** snapshot - the result after
///     <see cref="MatchTickPipeline.Step(MatchState)"/> ran on the
///     post-fire state (or directly on the input state when no fire request
///     was supplied). Its <see cref="MatchState.CurrentTick"/> is incremented
///     by exactly one.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// Out of scope: AI / script execution, command queues, fire-intent
/// collection, multiple fire requests per tick, fire-request generation,
/// projectile-id allocation, aiming or muzzle calculation, match-runner
/// integration, end-condition logic, tank movement, fire-rate /
/// friendly-fire / arena-bounds / line-of-sight checks, combat logs, replay
/// frames, serialization, and Godot integration.
/// </para>
/// </remarks>
public sealed class MatchTickFireThenProjectileOutcome
{
    public MatchState UpdatedState { get; }

    public MatchStateFireOutcome? FireOutcome { get; }

    public bool HadFireRequest { get; }

    public MatchTickFireThenProjectileOutcome(
        MatchState updatedState,
        MatchStateFireOutcome? fireOutcome,
        bool hadFireRequest)
    {
        ArgumentNullException.ThrowIfNull(updatedState);

        if (hadFireRequest && fireOutcome is null)
        {
            throw new ArgumentException(
                "A fire-request outcome must include a fire outcome.",
                nameof(fireOutcome));
        }

        if (!hadFireRequest && fireOutcome is not null)
        {
            throw new ArgumentException(
                "A no-fire-request outcome must not include a fire outcome.",
                nameof(fireOutcome));
        }

        UpdatedState = updatedState;
        FireOutcome = fireOutcome;
        HadFireRequest = hadFireRequest;
    }
}
