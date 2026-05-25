using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Bundles a sensor scan result with the updated immutable tank sensor loadout.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not perform scanning, visibility checks,
/// line-of-sight checks, target memory, scripting, match integration, replay
/// integration, logging, serialization, UI, or Godot behavior. Equality is
/// reference-based.
/// </remarks>
public sealed class LoadoutSensorScanOutcome
{
    public SensorScanResult Result { get; }

    public TankSensorLoadout UpdatedLoadout { get; }

    public LoadoutSensorScanOutcome(
        SensorScanResult result,
        TankSensorLoadout updatedLoadout)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(updatedLoadout);

        Result = result;
        UpdatedLoadout = updatedLoadout;
    }
}
