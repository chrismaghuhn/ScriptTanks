using System;
using System.Collections.Generic;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Minimal fixed-tick runner for sensor-runtime snapshots (<see cref="MatchSensorRuntimeState"/>).
/// </summary>
/// <remarks>
/// Validates scheduled scan request shape once before running via
/// <see cref="MatchScheduledSensorScanRequestValidator.Validate(ScriptTanks.Core.Simulation.SimTick, IReadOnlyList{MatchScheduledSensorScanRequest})"/>
/// using <see cref="Match.MatchState.CurrentTick"/> from <paramref name="initialRuntime"/>.
/// Scheduled request consumption starts at index 0 and runs exactly <paramref name="maxTicks"/>
/// iterations unless validation fails. This type does not evaluate match end conditions,
/// record replay frames, log combat events, run AI or script logic, sort or mutate
/// schedules, or integrate diagnostics or Godot.
/// </remarks>
public static class MatchSensorRuntimeFixedTickRunner
{
    public static MatchSensorRuntimeRunResult RunForTicks(
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

        MatchSensorRuntimeState runtime = initialRuntime;
        int nextIndex = 0;
        int ticksExecuted = 0;

        while (ticksExecuted < maxTicks)
        {
            MatchSensorRuntimeScheduledTickOutcome tickOutcome =
                MatchSensorRuntimeScheduledTickPipeline.Step(
                    runtime,
                    scheduledScanRequests,
                    nextIndex);

            runtime = tickOutcome.UpdatedRuntime;
            nextIndex = tickOutcome.NextIndex;
            ticksExecuted++;
        }

        return new MatchSensorRuntimeRunResult(
            runtime,
            ticksExecuted,
            nextIndex);
    }
}
