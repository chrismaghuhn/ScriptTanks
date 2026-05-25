using System;
using System.Collections.Generic;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Pure one-tick composer for scheduled sensor scans: selects and optionally
/// applies at most one scheduled scan for <see cref="MatchSensorRuntimeState"/>,
/// then runs <see cref="MatchTickPipeline.Step(MatchState)"/>.
/// </summary>
/// <remarks>
/// Delegates selection and scan application to
/// <see cref="MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick"/>.
/// Does not validate, sort, queue, or fully process schedules; does not run
/// match loops, evaluate end conditions, log, replay, diagnose, execute
/// AI/script logic, or integrate with Godot.
/// </remarks>
public static class MatchSensorRuntimeScheduledTickPipeline
{
    public static MatchSensorRuntimeScheduledTickOutcome Step(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<MatchScheduledSensorScanRequest> scheduledScanRequests,
        int nextIndex)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        MatchSensorRuntimeScheduledScanOutcome scanOutcome =
            MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(
                runtime,
                scheduledScanRequests,
                nextIndex);

        MatchState steppedState = MatchTickPipeline.Step(
            scanOutcome.UpdatedRuntime.State);

        MatchSensorRuntimeState updatedRuntime =
            scanOutcome.UpdatedRuntime.WithState(steppedState);

        return new MatchSensorRuntimeScheduledTickOutcome(
            scanOutcome.DidScan,
            scanOutcome.Result,
            updatedRuntime,
            scanOutcome.NextIndex);
    }
}
