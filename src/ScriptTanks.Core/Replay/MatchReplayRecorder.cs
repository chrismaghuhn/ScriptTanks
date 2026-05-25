using System;
using System.Collections.Generic;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Pure replay recorder that runs the existing match loop and captures a
/// full-state <see cref="MatchReplayFrame"/> at frame 0 and after every
/// executed tick. Mirrors the loop shapes of
/// <see cref="MatchRunner.RunUntilEnd(MatchState, int)"/> (no-fire) and
/// <see cref="MatchRunner.RunUntilEnd(MatchState, int, IReadOnlyList{MatchScheduledFireRequest})"/>
/// (scheduled fire) but is a separate static class - the existing runner
/// is not modified.
/// </summary>
/// <remarks>
/// Out of scope: AI / script execution, command queues, automatic
/// fire-request generation, projectile-id allocation, replay playback,
/// serialization, compression, delta encoding, combat logs, randomness,
/// system-clock access, and Godot integration.
/// </remarks>
public static class MatchReplayRecorder
{
    public static MatchRecordedRunResult RecordUntilEnd(
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

        List<MatchReplayFrame> frames = new()
        {
            new MatchReplayFrame(0, current),
        };

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

        return new MatchRecordedRunResult(runResult, recording);
    }

    /// <summary>
    /// Deterministic recorder overload that records full-state replay
    /// frames while consuming a strictly ordered list of
    /// <see cref="MatchScheduledFireRequest"/> values. Each tick applies
    /// at most one fire request whose <see cref="MatchScheduledFireRequest.Tick"/>
    /// matches <see cref="MatchState.CurrentTick"/>; otherwise the tick
    /// advances with no fire request via
    /// <see cref="MatchTickFireThenProjectilePipeline.Step(MatchState, MatchFireRequest?)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Schedule shape rules (validated up front):
    /// <list type="bullet">
    ///   <item><description>
    ///     No <see cref="MatchScheduledFireRequest.Tick"/> may be earlier
    ///     than <see cref="MatchState.CurrentTick"/> of
    ///     <paramref name="initialState"/>.
    ///   </description></item>
    ///   <item><description>
    ///     Ticks must be strictly increasing (no duplicates, no decreases).
    ///   </description></item>
    /// </list>
    /// All shape violations raise <see cref="ArgumentException"/> with
    /// <see cref="ArgumentException.ParamName"/> equal to
    /// <c>"scheduledFireRequests"</c>. Mirrors the validation contract of
    /// <see cref="MatchRunner.RunUntilEnd(MatchState, int, IReadOnlyList{MatchScheduledFireRequest})"/>.
    /// </para>
    /// <para>
    /// Frame 0 is always recorded before the initial end check. If the
    /// initial state is already ended, the recording contains exactly one
    /// frame and no scheduled request is consumed. Requests whose tick is
    /// not reached before the match ends are silently unused.
    /// </para>
    /// <para>
    /// Out of scope: AI / script execution, command queues, automatic
    /// fire-request generation, projectile-id allocation, replay playback,
    /// serialization, compression, delta encoding, combat logs, randomness,
    /// system-clock access, and Godot integration.
    /// </para>
    /// </remarks>
    public static MatchRecordedRunResult RecordUntilEnd(
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

        List<MatchReplayFrame> frames = new()
        {
            new MatchReplayFrame(0, current),
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

            MatchTickFireThenProjectileOutcome tickOutcome =
                MatchTickFireThenProjectilePipeline.Step(current, fireRequest);

            current = tickOutcome.UpdatedState;
            ticksExecuted++;

            frames.Add(new MatchReplayFrame(ticksExecuted, current));

            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            current,
            endCondition,
            ticksExecuted);

        MatchReplayRecording recording = new MatchReplayRecording(frames);

        return new MatchRecordedRunResult(runResult, recording);
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
