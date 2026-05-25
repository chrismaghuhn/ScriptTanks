using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Bundles a sensor scan result with the updated immutable
/// <see cref="MatchSensorRuntimeState"/>.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not perform scanning, visibility checks,
/// line-of-sight checks, target memory, scripting, AI integration, scan
/// scheduling, runner integration, replay integration, logging, diagnostics,
/// serialization, UI, or Godot behavior. Equality is reference-based.
/// </remarks>
public sealed class MatchSensorScanOutcome
{
    public SensorScanResult Result { get; }

    public MatchSensorRuntimeState UpdatedRuntime { get; }

    public MatchSensorScanOutcome(
        SensorScanResult result,
        MatchSensorRuntimeState updatedRuntime)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(updatedRuntime);

        Result = result;
        UpdatedRuntime = updatedRuntime;
    }
}
