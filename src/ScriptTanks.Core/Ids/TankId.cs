using System;
using System.Globalization;

namespace ScriptTanks.Core.Ids;

/// <summary>
/// Strongly typed identifier for a tank instance. Wraps a non-negative
/// <see cref="int"/> to prevent accidental confusion with other integer
/// identifiers (e.g. projectile id, player slot) or counters (HP, damage,
/// tick index). The struct intentionally exposes ordering through
/// <see cref="IComparable{T}"/> and the comparison operators for
/// deterministic sorting, stable tests, and reproducible log/replay output;
/// the order has no gameplay meaning.
/// </summary>
public readonly struct TankId : IEquatable<TankId>, IComparable<TankId>
{
    public int Value { get; }

    public TankId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "TankId value must not be negative.");
        }

        Value = value;
    }

    public bool Equals(TankId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is TankId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(TankId other) => Value.CompareTo(other.Value);

    public override string ToString()
        => Value.ToString("D", CultureInfo.InvariantCulture);

    public static bool operator ==(TankId left, TankId right) => left.Value == right.Value;

    public static bool operator !=(TankId left, TankId right) => left.Value != right.Value;

    public static bool operator <(TankId left, TankId right) => left.Value < right.Value;

    public static bool operator >(TankId left, TankId right) => left.Value > right.Value;

    public static bool operator <=(TankId left, TankId right) => left.Value <= right.Value;

    public static bool operator >=(TankId left, TankId right) => left.Value >= right.Value;
}
