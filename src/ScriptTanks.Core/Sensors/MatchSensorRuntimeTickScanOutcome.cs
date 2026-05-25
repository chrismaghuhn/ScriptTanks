using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Describes the result of optionally applying a single sensor scan to a
/// <see cref="MatchSensorRuntimeState"/> snapshot within a tick pipeline.
/// </summary>
/// <remarks>
/// Carries either a no-scan pass-through (<see cref="DidScan"/> false,
/// <see cref="Result"/> null) or the outcome of one delegated scan
/// (<see cref="DidScan"/> true). This type does not execute scans itself.
/// </remarks>
public sealed class MatchSensorRuntimeTickScanOutcome
{
    public bool DidScan { get; }

    public SensorScanResult? Result { get; }

    public MatchSensorRuntimeState UpdatedRuntime { get; }

    private MatchSensorRuntimeTickScanOutcome(
        bool didScan,
        SensorScanResult? result,
        MatchSensorRuntimeState updatedRuntime)
    {
        DidScan = didScan;
        Result = result;
        UpdatedRuntime = updatedRuntime;
    }

    public static MatchSensorRuntimeTickScanOutcome NoScan(
        MatchSensorRuntimeState runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        return new MatchSensorRuntimeTickScanOutcome(
            didScan: false,
            result: null,
            updatedRuntime: runtime);
    }

    public static MatchSensorRuntimeTickScanOutcome Scanned(
        SensorScanResult result,
        MatchSensorRuntimeState updatedRuntime)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(updatedRuntime);

        return new MatchSensorRuntimeTickScanOutcome(
            didScan: true,
            result: result,
            updatedRuntime: updatedRuntime);
    }
}
