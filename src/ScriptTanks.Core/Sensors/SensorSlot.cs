using System;
using System.Globalization;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Zero-based slot index for a tank sensor loadout.
/// </summary>
/// <remarks>
/// Pure value object only. This type does not perform scanning, loadout
/// mutation, match integration, scripting, replay integration, logging,
/// serialization, UI, or Godot behavior.
/// </remarks>
public readonly struct SensorSlot : IEquatable<SensorSlot>
{
    public int Value { get; }

    public static SensorSlot Zero => new SensorSlot(0);

    public SensorSlot(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Sensor slot must not be negative.");
        }

        Value = value;
    }

    public bool Equals(SensorSlot other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SensorSlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(SensorSlot left, SensorSlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SensorSlot left, SensorSlot right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return Value.ToString("D", CultureInfo.InvariantCulture);
    }
}
