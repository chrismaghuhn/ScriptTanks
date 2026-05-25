using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Bundles a sensor scan result with the updated immutable sensor state.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not perform scanning, visibility checks,
/// line-of-sight checks, target memory, scripting, match integration, replay
/// integration, logging, serialization, UI, or Godot behavior. Equality is
/// reference-based.
/// </remarks>
public sealed class SensorScanOutcome
{
    public SensorScanResult Result { get; }

    public SensorState UpdatedSensorState { get; }

    public SensorScanOutcome(
        SensorScanResult result,
        SensorState updatedSensorState)
    {
        ArgumentNullException.ThrowIfNull(result);

        Result = result;
        UpdatedSensorState = updatedSensorState;
    }
}
