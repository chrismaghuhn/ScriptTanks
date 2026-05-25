using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure wrapper around <see cref="MatchStateFireSystem.ResolveFire(MatchState, MatchFireRequest, SimTick)"/>
/// that also produces a <see cref="CombatLog"/> via
/// <see cref="FireCombatLogFactory.CreateFireLog(SimTick, MatchFireRequest, MatchStateFireOutcome)"/>.
/// </summary>
/// <remarks>
/// This type only composes existing pure systems. It does not mutate inputs,
/// emit logs globally, integrate with the tick pipeline, runner, replay
/// recorder, serialization, UI, or Godot. All exception semantics come from
/// the wrapped <see cref="MatchStateFireSystem"/> delegate; the wrapper
/// performs no extra validation.
/// </remarks>
public static class LoggedMatchStateFireSystem
{
    public static LoggedMatchStateFireOutcome ResolveFire(
        MatchState state,
        MatchFireRequest request,
        SimTick currentTick)
    {
        MatchStateFireOutcome fireOutcome = MatchStateFireSystem.ResolveFire(
            state,
            request,
            currentTick);

        CombatLog log = FireCombatLogFactory.CreateFireLog(
            currentTick,
            request,
            fireOutcome);

        return new LoggedMatchStateFireOutcome(fireOutcome, log);
    }
}
