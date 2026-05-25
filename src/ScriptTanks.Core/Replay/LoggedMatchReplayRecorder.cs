using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Pure logged replay recorder. Mirrors the existing
/// <see cref="MatchReplayRecorder"/> loops (no-fire and scheduled-fire) while
/// emitting <c>match_started</c> at the initial tick, optional per-tick fire
/// logs (scheduled-fire overload), and <c>match_ended</c> at the final tick.
/// </summary>
/// <remarks>
/// This type only composes existing pure systems. It does not modify
/// <see cref="MatchReplayRecorder"/>, emit logs globally, integrate with
/// projectile-hit/damage/cleanup logs, serialization, UI, or Godot.
/// Replay-frame semantics match the existing recorder verbatim.
/// </remarks>
public static class LoggedMatchReplayRecorder
{
    public static LoggedMatchRecordedRunResult RecordUntilEnd(
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

        List<MatchReplayFrame> frames = new List<MatchReplayFrame>
        {
            new MatchReplayFrame(0, current),
        };

        CombatLog startLog = MatchCombatLogFactory.CreateMatchStartedLog(
            current.CurrentTick);

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(current, maxTicks);

        while (!endCondition.IsEnded)
        {
            current = MatchTickPipeline.Step(current);
            ticksExecuted++;

            frames.Add(new MatchReplayFrame(ticksExecuted, current));

            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            current,
            endCondition,
            ticksExecuted);

        MatchReplayRecording recording = new MatchReplayRecording(frames);

        MatchRecordedRunResult recordedRunResult = new MatchRecordedRunResult(
            runResult,
            recording);

        CombatLog endLog = MatchCombatLogFactory.CreateMatchEndedLog(
            current.CurrentTick,
            endCondition);

        CombatLog mergedLog = CombatLogMerger.Merge(startLog, endLog);

        return new LoggedMatchRecordedRunResult(recordedRunResult, mergedLog);
    }

    /// <summary>
    /// Pure logged scheduled-fire replay recorder. Mirrors
    /// <see cref="MatchReplayRecorder.RecordUntilEnd(MatchState, int, IReadOnlyList{MatchScheduledFireRequest})"/>
    /// while emitting <c>match_started</c> at the initial tick, per-tick fire
    /// request/result entries via
    /// <see cref="LoggedMatchTickFireThenProjectilePipeline.Step(MatchState, MatchFireRequest?)"/>,
    /// and <c>match_ended</c> at the final tick.
    /// </summary>
    /// <remarks>
    /// This type only composes existing pure systems. It does not modify
    /// <see cref="MatchReplayRecorder"/>, <see cref="MatchRunner"/>, emit
    /// logs globally, integrate with projectile-hit/damage/cleanup logs,
    /// standalone tank-destroyed event logs, serialization, UI, or Godot.
    /// Replay-frame semantics match the existing recorder verbatim. Schedule
    /// shape validation matches the
    /// <see cref="MatchReplayRecorder"/> scheduled-fire overload: any
    /// violation raises <see cref="ArgumentException"/> with
    /// <see cref="ArgumentException.ParamName"/> equal to
    /// <c>"scheduledFireRequests"</c>.
    /// </remarks>
    public static LoggedMatchRecordedRunResult RecordUntilEnd(
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

        List<MatchReplayFrame> frames = new List<MatchReplayFrame>
        {
            new MatchReplayFrame(0, current),
        };

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

            frames.Add(new MatchReplayFrame(ticksExecuted, current));
            logs.Add(tickOutcome.Log);

            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            current,
            endCondition,
            ticksExecuted);

        MatchReplayRecording recording = new MatchReplayRecording(frames);

        MatchRecordedRunResult recordedRunResult = new MatchRecordedRunResult(
            runResult,
            recording);

        logs.Add(MatchCombatLogFactory.CreateMatchEndedLog(
            current.CurrentTick,
            endCondition));

        CombatLog mergedLog = CombatLogMerger.Merge(logs);

        return new LoggedMatchRecordedRunResult(recordedRunResult, mergedLog);
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
