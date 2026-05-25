using System;
using System.Collections.Generic;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Composes scheduled sensor scan selection with optional per-tick scan
/// application for the current <see cref="MatchSensorRuntimeState"/>.
/// </summary>
/// <remarks>
/// <para>
/// Applies at most one scheduled scan when the entry at
/// <paramref name="nextIndex"/> has a tick equal to
/// <c>runtime.State.CurrentTick</c>. The next index is incremented only when a
/// request is selected. Schedule validation is expected to happen elsewhere.
/// </para>
/// <para>
/// This type does not sort, mutate, or fully process the schedule, advance
/// simulation ticks, or integrate with AI/script runtimes, logging, replay,
/// diagnostics, or Godot.
/// </para>
/// </remarks>
public static class MatchSensorRuntimeScheduledScanPipeline
{
    public static MatchSensorRuntimeScheduledScanOutcome ApplyScheduledScanForCurrentTick(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<MatchScheduledSensorScanRequest> scheduledScanRequests,
        int nextIndex)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            scheduledScanRequests,
            runtime.State.CurrentTick,
            nextIndex,
            out MatchSensorScanRequest request);

        if (!selected)
        {
            return new MatchSensorRuntimeScheduledScanOutcome(
                didScan: false,
                result: null,
                updatedRuntime: runtime,
                nextIndex: nextIndex);
        }

        MatchSensorRuntimeTickScanOutcome scanOutcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(
                runtime,
                request);

        return new MatchSensorRuntimeScheduledScanOutcome(
            didScan: scanOutcome.DidScan,
            result: scanOutcome.Result,
            updatedRuntime: scanOutcome.UpdatedRuntime,
            nextIndex: nextIndex + 1);
    }
}
