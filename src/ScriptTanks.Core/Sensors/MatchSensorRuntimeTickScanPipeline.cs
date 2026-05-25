using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Optional single-scan pipeline that applies at most one
/// <see cref="MatchSensorScanRequest"/> to a
/// <see cref="MatchSensorRuntimeState"/> snapshot.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure optional single-scan composition. It applies zero or one
/// <see cref="MatchSensorScanRequest"/>; a null request means no scan. It
/// returns a <see cref="MatchSensorRuntimeTickScanOutcome"/> with the
/// (possibly unchanged) runtime snapshot.
/// </para>
/// <para>
/// It does not advance simulation ticks, process schedules or scan lists,
/// queue, sort, log, record replay, run diagnostics, execute AI or script
/// logic, or integrate with Godot.
/// </para>
/// </remarks>
public static class MatchSensorRuntimeTickScanPipeline
{
    public static MatchSensorRuntimeTickScanOutcome ApplyOptionalScan(
        MatchSensorRuntimeState runtime,
        MatchSensorScanRequest? scanRequest)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        if (!scanRequest.HasValue)
        {
            return MatchSensorRuntimeTickScanOutcome.NoScan(runtime);
        }

        MatchSensorScanOutcome scanOutcome = MatchSensorScanSystem.Scan(
            runtime,
            scanRequest.Value);

        return MatchSensorRuntimeTickScanOutcome.Scanned(
            scanOutcome.Result,
            scanOutcome.UpdatedRuntime);
    }
}
