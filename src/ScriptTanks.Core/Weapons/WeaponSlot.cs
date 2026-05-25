using System;
using System.Globalization;

namespace ScriptTanks.Core.Weapons;

/// <summary>
/// Strongly typed weapon slot index for a tank loadout. Wraps a
/// non-negative <see cref="int"/> to prevent confusion with tank ids,
/// projectile ids, player slots, or other integer counters. No upper
/// bound is enforced in this primitive — concrete loadout sizes restrict
/// the valid range. Ordering is exposed through <see cref="IComparable{T}"/>
/// and the comparison operators for deterministic sorting and stable
/// log/replay output; the order has no gameplay meaning.
/// </summary>
public readonly struct WeaponSlot : IEquatable<WeaponSlot>, IComparable<WeaponSlot>
{
    public int Value { get; }

    public WeaponSlot(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "WeaponSlot value must not be negative.");
        }

        Value = value;
    }

    public bool Equals(WeaponSlot other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is WeaponSlot other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(WeaponSlot other) => Value.CompareTo(other.Value);

    public override string ToString()
        => Value.ToString("D", CultureInfo.InvariantCulture);

    public static bool operator ==(WeaponSlot left, WeaponSlot right) => left.Value == right.Value;

    public static bool operator !=(WeaponSlot left, WeaponSlot right) => left.Value != right.Value;

    public static bool operator <(WeaponSlot left, WeaponSlot right) => left.Value < right.Value;

    public static bool operator >(WeaponSlot left, WeaponSlot right) => left.Value > right.Value;

    public static bool operator <=(WeaponSlot left, WeaponSlot right) => left.Value <= right.Value;

    public static bool operator >=(WeaponSlot left, WeaponSlot right) => left.Value >= right.Value;
}
