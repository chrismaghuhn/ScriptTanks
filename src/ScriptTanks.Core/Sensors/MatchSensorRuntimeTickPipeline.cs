using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Pure one-tick composer for <see cref="MatchSensorRuntimeState"/>: optionally
/// applies one sensor scan, then runs <see cref="MatchTickPipeline.Step(MatchState)"/>
/// and folds the stepped <see cref="MatchState"/> back into the runtime snapshot.
/// </summary>
/// <remarks>
/// Does not run match loops, evaluate end conditions, record replay, log,
/// schedule scans, execute AI or scripts, or integrate with Godot.
/// </remarks>
public static class MatchSensorRuntimeTickPipeline
{
    public static MatchSensorRuntimeTickOutcome Step(
        MatchSensorRuntimeState runtime,
        MatchSensorScanRequest? scanRequest)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        MatchSensorRuntimeTickScanOutcome scanOutcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(
                runtime,
                scanRequest);

        MatchState steppedState = MatchTickPipeline.Step(
            scanOutcome.UpdatedRuntime.State);

        MatchSensorRuntimeState updatedRuntime =
            scanOutcome.UpdatedRuntime.WithState(steppedState);

        return new MatchSensorRuntimeTickOutcome(
            scanOutcome.DidScan,
            scanOutcome.Result,
            updatedRuntime);
    }
}
