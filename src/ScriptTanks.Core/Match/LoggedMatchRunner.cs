using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Minimal logged runner for the existing no-fire match loop.
/// </summary>
/// <remarks>
/// This type mirrors the existing no-fire runner loop and composes match
/// lifecycle combat logs. It does not modify <see cref="MatchRunner"/>, emit
/// logs globally, integrate with replay recording, scheduled fire,
/// serialization, UI, or Godot. Fire, projectile, damage, and cleanup logs
/// are out of scope here.
/// </remarks>
public static class LoggedMatchRunner
{
    public static LoggedMatchRunResult RunUntilEnd(
        MatchState initialState,
        int maxTicks)
    {
        ArgumentNullException.ThrowIfNull(initialState);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "maxTicks must not be negative.");
        }

        MatchState current = initialState;
        int ticksExecuted = 0;

        CombatLog startLog = MatchCombatLogFactory.CreateMatchStartedLog(
            current.CurrentTick);

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(current, maxTicks);

        while (!endCondition.IsEnded)
        {
            current = MatchTickPipeline.Step(current);
            ticksExecuted++;
            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            current,
            endCondition,
            ticksExecuted);

        CombatLog endLog = MatchCombatLogFactory.CreateMatchEndedLog(
            current.CurrentTick,
            endCondition);

        CombatLog mergedLog = CombatLogMerger.Merge(startLog, endLog);

        return new LoggedMatchRunResult(runResult, mergedLog);
    }

    /// <summary>
    /// Logged scheduled-fire runner overload. Mirrors
    /// <see cref="MatchRunner.RunUntilEnd(MatchState, int, IReadOnlyList{MatchScheduledFireRequest})"/>
    /// while emitting <c>match_started</c> at the initial tick, per-tick fire
    /// request/result entries via
    /// <see cref="LoggedMatchTickFireThenProjectilePipeline.Step(MatchState, MatchFireRequest?)"/>,
    /// and <c>match_ended</c> at the final tick.
    /// </summary>
    /// <remarks>
    /// This type only composes existing pure systems. It does not modify
    /// <see cref="MatchRunner"/>, emit logs globally, integrate with replay
    /// recording, serialization, UI, or Godot. Projectile-hit, damage,
    /// projectile-cleanup, and standalone tank-destroyed event logs are out of
    /// scope here. Schedule-shape validation matches the
    /// <see cref="MatchRunner"/> scheduled-fire overload.
    /// </remarks>
    public static LoggedMatchRunResult RunUntilEnd(
        MatchState initialState,
        int maxTicks,
        IReadOnlyList<MatchScheduledFireRequest> scheduledFireRequests)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(scheduledFireRequests);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "maxTicks must not be negative.");
        }

        ValidateScheduledFireRequests(initialState, scheduledFireRequests);

        MatchState current = initialState;
        int ticksExecuted = 0;
        int nextRequestIndex = 0;

        List<CombatLog> logs = new List<CombatLog>
        {
            MatchCombatLogFactory.CreateMatchStartedLog(current.CurrentTick),
        };

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(current, maxTicks);

        while (!endCondition.IsEnded)
        {
            MatchFireRequest? fireRequest = null;

            if (nextRequestIndex < scheduledFireRequests.Count
                && scheduledFireRequests[nextRequestIndex].Tick == current.CurrentTick)
            {
                fireRequest = scheduledFireRequests[nextRequestIndex].Request;
                nextRequestIndex++;
            }

            LoggedMatchTickFireThenProjectileOutcome tickOutcome =
                LoggedMatchTickFireThenProjectilePipeline.Step(current, fireRequest);

            current = tickOutcome.TickOutcome.UpdatedState;
            ticksExecuted++;

            logs.Add(tickOutcome.Log);

            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            current,
            endCondition,
            ticksExecuted);

        logs.Add(MatchCombatLogFactory.CreateMatchEndedLog(
            current.CurrentTick,
            endCondition));

        CombatLog mergedLog = CombatLogMerger.Merge(logs);

        return new LoggedMatchRunResult(runResult, mergedLog);
    }

    private static void ValidateScheduledFireRequests(
        MatchState initialState,
        IReadOnlyList<MatchScheduledFireRequest> scheduledFireRequests)
    {
        int previousTick = -1;

        for (int i = 0; i < scheduledFireRequests.Count; i++)
        {
            int tick = scheduledFireRequests[i].Tick.Value;

            if (tick < initialState.CurrentTick.Value)
            {
                throw new ArgumentException(
                    "Scheduled fire requests must not be earlier than the initial state's current tick.",
                    nameof(scheduledFireRequests));
            }

            if (i > 0 && tick <= previousTick)
            {
                throw new ArgumentException(
                    "Scheduled fire requests must be strictly ordered by tick and may not contain duplicate ticks.",
                    nameof(scheduledFireRequests));
            }

            previousTick = tick;
        }
    }
}
