using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure wrapper around <see cref="MatchTickFireThenProjectilePipeline.Step(MatchState, MatchFireRequest?)"/>
/// that also creates combat-log entries for an optional fire request.
/// </summary>
/// <remarks>
/// This type only composes existing pure systems. It does not mutate inputs,
/// emit logs globally, integrate with runners, replay recorders, serialization,
/// UI, or Godot. This wrapper logs fire request/result events only;
/// projectile-hit, damage, cleanup, and match-end logs are handled by future
/// tasks. All exception semantics come from the wrapped delegate; in
/// particular, null <paramref name="state"/> raises
/// <see cref="System.ArgumentNullException"/> from the delegate before this
/// wrapper dereferences <see cref="MatchState.CurrentTick"/>.
/// </remarks>
public static class LoggedMatchTickFireThenProjectilePipeline
{
    public static LoggedMatchTickFireThenProjectileOutcome Step(
        MatchState state,
        MatchFireRequest? fireRequest)
    {
        MatchTickFireThenProjectileOutcome tickOutcome =
            MatchTickFireThenProjectilePipeline.Step(state, fireRequest);

        CombatLog log;
        if (fireRequest.HasValue)
        {
            log = FireCombatLogFactory.CreateFireLog(
                state.CurrentTick,
                fireRequest.Value,
                tickOutcome.FireOutcome!);
        }
        else
        {
            log = new CombatLogBuilder().Build();
        }

        return new LoggedMatchTickFireThenProjectileOutcome(tickOutcome, log);
    }
}
