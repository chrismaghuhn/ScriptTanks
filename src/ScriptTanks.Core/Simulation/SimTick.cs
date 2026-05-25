using System;
using System.Globalization;

namespace ScriptTanks.Core.Simulation;

/// <summary>
/// Strongly typed simulation tick value. Wraps a non-negative <see cref="int"/>
/// to prevent accidental confusion with HP, damage or other integer counters.
/// </summary>
public readonly struct SimTick : IEquatable<SimTick>, IComparable<SimTick>
{
    public int Value { get; }

    public static SimTick Zero { get; } = new SimTick(0);

    public SimTick(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "SimTick value must not be negative.");
        }

        Value = value;
    }

    public SimTick Next() => new SimTick(Value + 1);

    public bool IsAfterOrEqual(SimTick other) => Value >= other.Value;

    public static bool operator ==(SimTick left, SimTick right) => left.Value == right.Value;

    public static bool operator !=(SimTick left, SimTick right) => left.Value != right.Value;

    public static bool operator <(SimTick left, SimTick right) => left.Value < right.Value;

    public static bool operator >(SimTick left, SimTick right) => left.Value > right.Value;

    public static bool operator <=(SimTick left, SimTick right) => left.Value <= right.Value;

    public static bool operator >=(SimTick left, SimTick right) => left.Value >= right.Value;

    public bool Equals(SimTick other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is SimTick other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(SimTick other) => Value.CompareTo(other.Value);

    public override string ToString()
        => Value.ToString("D", CultureInfo.InvariantCulture);
}
