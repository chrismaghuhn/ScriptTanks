using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Outcome of attempting to apply at most one scheduled sensor scan for the
/// current runtime tick.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not execute scans, validate schedule length
/// against <see cref="NextIndex"/>, or verify index progression.
/// </remarks>
public sealed class MatchSensorRuntimeScheduledScanOutcome
{
    public bool DidScan { get; }

    public SensorScanResult? Result { get; }

    public MatchSensorRuntimeState UpdatedRuntime { get; }

    public int NextIndex { get; }

    public MatchSensorRuntimeScheduledScanOutcome(
        bool didScan,
        SensorScanResult? result,
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
            ArgumentNullException.ThrowIfNull(result);
        }

        DidScan = didScan;
        Result = result;
        UpdatedRuntime = updatedRuntime;
        NextIndex = nextIndex;
    }
}
