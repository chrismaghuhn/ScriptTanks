using System;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure one-tick <see cref="MatchState"/> composer that optionally applies
/// one explicit <see cref="MatchFireRequest"/> before running the existing
/// projectile/tick pipeline. The fixed deterministic order is:
/// <list type="number">
///   <item><description>
///     Optional <see cref="MatchStateFireSystem.ResolveFire(MatchState, MatchFireRequest, ScriptTanks.Core.Simulation.SimTick)"/>
///     using <see cref="MatchState.CurrentTick"/> as the cooldown reference.
///   </description></item>
///   <item><description>
///     <see cref="MatchTickPipeline.Step(MatchState)"/> (which itself runs
///     projectile Step / Hit / Cleanup, then advances
///     <see cref="MatchState.CurrentTick"/> by one).
///   </description></item>
/// </list>
/// <para>
/// Fire happens before the projectile pipeline so a freshly spawned
/// projectile can move, hit, and be cleaned up within the same tick.
/// </para>
/// <para>
/// This composer is NOT a match runner. It does not implement a match
/// loop, max-tick or timeout checks, win/loss or end-condition logic, AI
/// or script execution, command queues, fire-intent generation, multiple
/// fire requests per tick, projectile-id allocation, aiming or muzzle
/// calculation, tank movement, fire-rate / friendly-fire / arena-bounds /
/// line-of-sight checks, combat logs, replay frames, serialization, or
/// Godot integration.
/// </para>
/// </summary>
public static class MatchTickFireThenProjectilePipeline
{
    public static MatchTickFireThenProjectileOutcome Step(
        MatchState state,
        MatchFireRequest? fireRequest)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (fireRequest.HasValue)
        {
            MatchStateFireOutcome fireOutcome = MatchStateFireSystem.ResolveFire(
                state,
                fireRequest.Value,
                state.CurrentTick);

            MatchState afterTick = MatchTickPipeline.Step(fireOutcome.UpdatedState);

            return new MatchTickFireThenProjectileOutcome(
                afterTick,
                fireOutcome,
                hadFireRequest: true);
        }

        MatchState afterNoFireTick = MatchTickPipeline.Step(state);

        return new MatchTickFireThenProjectileOutcome(
            afterNoFireTick,
            fireOutcome: null,
            hadFireRequest: false);
    }
}
