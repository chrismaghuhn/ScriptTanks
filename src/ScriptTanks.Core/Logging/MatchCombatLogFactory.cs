using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Pure factory for match lifecycle combat-log entries.
/// </summary>
/// <remarks>
/// This type only converts match lifecycle data into immutable combat-log
/// snapshots. It does not emit logs, store logs globally, integrate with
/// runners, replay recorders, pipelines, serialization, UI, or Godot.
/// Messages are concrete human-readable strings - no template formatter is
/// used or introduced here.
/// </remarks>
public static class MatchCombatLogFactory
{
    public static CombatLog CreateMatchStartedLog(SimTick tick)
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Match,
            CombatLogEventTypes.MatchStarted,
            "Match started.");

        return builder.Build();
    }

    public static CombatLog CreateMatchEndedLog(
        SimTick tick,
        MatchEndConditionResult endCondition)
    {
        ArgumentNullException.ThrowIfNull(endCondition);

        if (!endCondition.IsEnded)
        {
            throw new ArgumentException(
                "Cannot create a match-ended log for a running match.",
                nameof(endCondition));
        }

        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Match,
            CombatLogEventTypes.MatchEnded,
            CreateEndedMessage(endCondition));

        return builder.Build();
    }

    private static string CreateEndedMessage(MatchEndConditionResult endCondition)
    {
        return endCondition.Reason switch
        {
            MatchEndReason.TankDestroyed =>
                $"Match ended: tank {endCondition.WinnerTankId!.Value} won by tank destruction.",
            MatchEndReason.TimeoutHpAdvantage =>
                $"Match ended: tank {endCondition.WinnerTankId!.Value} won by timeout HP advantage.",
            MatchEndReason.TimeoutDraw =>
                "Match ended: timeout draw.",
            _ =>
                $"Match ended: {endCondition.Reason}.",
        };
    }
}
