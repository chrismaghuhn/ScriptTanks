using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Outcome of one composed tick that may apply a scheduled sensor scan for the
/// current match tick, then steps match simulation.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not execute scans or validate schedules.
/// </remarks>
public sealed class MatchSensorRuntimeScheduledTickOutcome
{
    public bool DidScan { get; }

    public SensorScanResult? ScanResult { get; }

    public MatchSensorRuntimeState UpdatedRuntime { get; }

    public int NextIndex { get; }

    public MatchSensorRuntimeScheduledTickOutcome(
        bool didScan,
        SensorScanResult? scanResult,
        MatchSensorRuntimeState updatedRuntime,
        int nextIndex)
    {
        ArgumentNullException.ThrowIfNull(updatedRuntime);

        if (nextIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextIndex),
                nextIndex,
                "Next scheduled sensor scan request index must not be negative.");
        }

        if (didScan)
        {
            ArgumentNullException.ThrowIfNull(scanResult);
        }

        DidScan = didScan;
        ScanResult = scanResult;
        UpdatedRuntime = updatedRuntime;
        NextIndex = nextIndex;
    }
}
