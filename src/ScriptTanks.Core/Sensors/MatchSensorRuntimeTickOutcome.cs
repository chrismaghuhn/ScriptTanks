using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Outcome of running one composed tick: optional sensor scan plus match
/// simulation step on <see cref="MatchSensorRuntimeState"/>.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not execute scans or advance ticks itself.
/// </remarks>
public sealed class MatchSensorRuntimeTickOutcome
{
    public bool DidScan { get; }

    public SensorScanResult? ScanResult { get; }

    public MatchSensorRuntimeState UpdatedRuntime { get; }

    public MatchSensorRuntimeTickOutcome(
        bool didScan,
        SensorScanResult? scanResult,
        MatchSensorRuntimeState updatedRuntime)
    {
        ArgumentNullException.ThrowIfNull(updatedRuntime);

        if (didScan)
        {
            ArgumentNullException.ThrowIfNull(scanResult);
        }

        DidScan = didScan;
        ScanResult = scanResult;
        UpdatedRuntime = updatedRuntime;
    }
}
