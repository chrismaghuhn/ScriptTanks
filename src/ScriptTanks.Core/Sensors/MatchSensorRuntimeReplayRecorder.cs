using System;
using System.Collections.Generic;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Minimal replay recorder for sensor-runtime matches over <see cref="MatchSensorRuntimeState"/>.
/// </summary>
/// <remarks>
/// Validates scheduled scan request shape once before recording via
/// <see cref="MatchScheduledSensorScanRequestValidator.Validate(ScriptTanks.Core.Simulation.SimTick, IReadOnlyList{MatchScheduledSensorScanRequest})"/>.
/// Records the initial runtime as <see cref="MatchSensorRuntimeReplayFrame"/> 0, then one frame
/// after each executed tick, with <see cref="MatchSensorRuntimeReplayFrame.FrameIndex"/> equal to
/// ticks executed at that point. End conditions are evaluated with
/// <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/> before the first tick and
/// after each tick. Returns a <see cref="MatchSensorRuntimeRunWithReplayResult"/> with final
/// runtime, ticks executed, next scheduled scan index, end condition, and replay frames. This
/// type does not play back frames, serialize or compress them, log combat, run AI or script
/// logic, sort or mutate schedules, or integrate diagnostics or Godot.
/// </remarks>
public static class MatchSensorRuntimeReplayRecorder
{
    public static MatchSensorRuntimeRunWithReplayResult RecordUntilEnd(
        MatchSensorRuntimeState initialRuntime,
        int maxTicks,
        IReadOnlyList<MatchScheduledSensorScanRequest> scheduledScanRequests)
    {
        ArgumentNullException.ThrowIfNull(initialRuntime);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "Maximum ticks must not be negative.");
        }

        MatchScheduledSensorScanRequestValidator.Validate(
            initialRuntime.State.CurrentTick,
            scheduledScanRequests);

        var frames = new List<MatchSensorRuntimeReplayFrame>
        {
            new MatchSensorRuntimeReplayFrame(
                frameIndex: 0,
                state: initialRuntime),
        };

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(
                initialRuntime.State,
                maxTicks);

        if (endCondition.IsEnded)
        {
            return new MatchSensorRuntimeRunWithReplayResult(
                initialRuntime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition,
                frames);
        }

        MatchSensorRuntimeState runtime = initialRuntime;
        int nextIndex = 0;
        int ticksExecuted = 0;

        while (!endCondition.IsEnded)
        {
            MatchSensorRuntimeScheduledTickOutcome tickOutcome =
                MatchSensorRuntimeScheduledTickPipeline.Step(
                    runtime,
                    scheduledScanRequests,
                    nextIndex);

            runtime = tickOutcome.UpdatedRuntime;
            nextIndex = tickOutcome.NextIndex;
            ticksExecuted++;

            frames.Add(new MatchSensorRuntimeReplayFrame(
                frameIndex: ticksExecuted,
                state: runtime));

            endCondition = MatchEndConditionEvaluator.Evaluate(
                runtime.State,
                maxTicks);
        }

        return new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted,
            nextIndex,
            endCondition,
            frames);
    }
}
