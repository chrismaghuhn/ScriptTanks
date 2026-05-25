using System;
using System.Collections.Generic;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Minimal run-until-ended runner for sensor-runtime snapshots (<see cref="MatchSensorRuntimeState"/>).
/// </summary>
/// <remarks>
/// Validates scheduled scan request shape once before running via
/// <see cref="MatchScheduledSensorScanRequestValidator.Validate(ScriptTanks.Core.Simulation.SimTick, IReadOnlyList{MatchScheduledSensorScanRequest})"/>
/// using <see cref="Match.MatchState.CurrentTick"/> from <paramref name="initialRuntime"/>.
/// Scheduled request consumption starts at index 0. End conditions are evaluated with
/// <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/> before the first tick
/// and after each tick; execution stops when the match is ended. The result carries the final
/// runtime, ticks executed, next scheduled scan index, and end condition. This type does not
/// record replay frames, log combat events, run AI or script logic, sort or mutate schedules,
/// or integrate diagnostics or Godot.
/// </remarks>
public static class MatchSensorRuntimeRunner
{
    public static MatchSensorRuntimeEndedRunResult RunUntilEnd(
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

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(
                initialRuntime.State,
                maxTicks);

        if (endCondition.IsEnded)
        {
            return new MatchSensorRuntimeEndedRunResult(
                initialRuntime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition);
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

            endCondition = MatchEndConditionEvaluator.Evaluate(
                runtime.State,
                maxTicks);
        }

        return new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted,
            nextIndex,
            endCondition);
    }
}
