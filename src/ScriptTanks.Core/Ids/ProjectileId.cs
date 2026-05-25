using System;
using System.Globalization;

namespace ScriptTanks.Core.Ids;

/// <summary>
/// Strongly typed identifier for a projectile instance. Wraps a non-negative
/// <see cref="int"/> to prevent accidental confusion with other integer
/// identifiers (e.g. tank id, player slot) or counters (HP, damage,
/// tick index). The struct intentionally exposes ordering through
/// <see cref="IComparable{T}"/> and the comparison operators for
/// deterministic sorting, stable tests, and reproducible log/replay output;
/// the order has no gameplay meaning.
/// </summary>
public readonly struct ProjectileId : IEquatable<ProjectileId>, IComparable<ProjectileId>
{
    public int Value { get; }

    public ProjectileId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "ProjectileId value must not be negative.");
        }

        Value = value;
    }

    public bool Equals(ProjectileId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ProjectileId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(ProjectileId other) => Value.CompareTo(other.Value);

    public override string ToString()
        => Value.ToString("D", CultureInfo.InvariantCulture);

    public static bool operator ==(ProjectileId left, ProjectileId right) => left.Value == right.Value;

    public static bool operator !=(ProjectileId left, ProjectileId right) => left.Value != right.Value;

    public static bool operator <(ProjectileId left, ProjectileId right) => left.Value < right.Value;

    public static bool operator >(ProjectileId left, ProjectileId right) => left.Value > right.Value;

    public static bool operator <=(ProjectileId left, ProjectileId right) => left.Value <= right.Value;

    public static bool operator >=(ProjectileId left, ProjectileId right) => left.Value >= right.Value;
}
