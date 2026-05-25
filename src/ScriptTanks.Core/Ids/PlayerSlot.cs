using System;
using System.Globalization;

namespace ScriptTanks.Core.Ids;

/// <summary>
/// Strongly typed identifier for a player slot in a match (e.g. seat 0
/// or seat 1 in a duel). Wraps a non-negative <see cref="int"/> to prevent
/// confusion with tank ids, projectile ids, or other integer counters.
/// No upper bound is enforced in this primitive — concrete match
/// configurations may later restrict the valid range. The struct
/// intentionally exposes ordering through <see cref="IComparable{T}"/>
/// and the comparison operators for deterministic sorting, stable tests,
/// and reproducible log/replay output; the order has no gameplay meaning.
/// </summary>
public readonly struct PlayerSlot : IEquatable<PlayerSlot>, IComparable<PlayerSlot>
{
    public int Value { get; }

    public PlayerSlot(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "PlayerSlot value must not be negative.");
        }

        Value = value;
    }

    public bool Equals(PlayerSlot other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is PlayerSlot other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(PlayerSlot other) => Value.CompareTo(other.Value);

    public override string ToString()
        => Value.ToString("D", CultureInfo.InvariantCulture);

    public static bool operator ==(PlayerSlot left, PlayerSlot right) => left.Value == right.Value;

    public static bool operator !=(PlayerSlot left, PlayerSlot right) => left.Value != right.Value;

    public static bool operator <(PlayerSlot left, PlayerSlot right) => left.Value < right.Value;

    public static bool operator >(PlayerSlot left, PlayerSlot right) => left.Value > right.Value;

    public static bool operator <=(PlayerSlot left, PlayerSlot right) => left.Value <= right.Value;

    public static bool operator >=(PlayerSlot left, PlayerSlot right) => left.Value >= right.Value;
}
