using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable runtime sensor-state loadout for one tank.
/// </summary>
/// <remarks>
/// Pure loadout data and replacement helper only. This type does not execute
/// scans, perform visibility checks, integrate with match systems, scripting,
/// replay systems, logging, serialization, UI, or Godot. Equality is
/// reference-based.
/// </remarks>
public sealed class TankSensorLoadout
{
    public IReadOnlyList<SensorState> Sensors { get; }

    public int Count => Sensors.Count;

    public TankSensorLoadout(IEnumerable<SensorState> sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);

        SensorState[] copiedSensors = sensors.ToArray();

        if (copiedSensors.Length == 0)
        {
            throw new ArgumentException(
                "A tank sensor loadout must contain at least one sensor.",
                nameof(sensors));
        }

        Sensors = Array.AsReadOnly(copiedSensors);
    }

    public SensorState GetSensor(SensorSlot slot)
    {
        ValidateSlot(slot);
        return Sensors[slot.Value];
    }

    public TankSensorLoadout WithSensor(
        SensorSlot slot,
        SensorState sensorState)
    {
        ValidateSlot(slot);

        SensorState[] copiedSensors = Sensors.ToArray();
        copiedSensors[slot.Value] = sensorState;

        return new TankSensorLoadout(copiedSensors);
    }

    private void ValidateSlot(SensorSlot slot)
    {
        if (slot.Value >= Sensors.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Sensor slot is outside the sensor loadout range.");
        }
    }
}
