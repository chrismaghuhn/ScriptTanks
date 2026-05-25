using System;
using System.Globalization;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Weapons;

/// <summary>
/// Immutable runtime cooldown state for a weapon. Tracks whether the weapon
/// has fired and the tick at which it last fired. The struct does not create
/// fire requests, spawn projectiles, apply damage, validate fire permissions,
/// or interact with match state. Valid gameplay values must be created
/// through the constructor or <see cref="Ready(WeaponDefinition)"/>;
/// <c>default(WeaponState)</c> is permitted by the C# struct contract but is
/// NOT considered a valid gameplay state (e.g. <see cref="Definition"/> would
/// be <c>null</c>).
/// <para>
/// Equality uses <see cref="object.ReferenceEquals(object?, object?)"/> for
/// <see cref="Definition"/>, consistent with the deliberate absence of an
/// equality override on <see cref="WeaponDefinition"/>. Two
/// <see cref="WeaponState"/> values referencing different
/// <see cref="WeaponDefinition"/> instances therefore compare as unequal even
/// if those definitions carry identical data.
/// </para>
/// </summary>
public readonly struct WeaponState : IEquatable<WeaponState>
{
    public WeaponDefinition Definition { get; }

    public SimTick LastFireTick { get; }

    public bool HasFired { get; }

    public WeaponState(
        WeaponDefinition definition,
        SimTick lastFireTick,
        bool hasFired)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        LastFireTick = lastFireTick;
        HasFired = hasFired;
    }

    public static WeaponState Ready(WeaponDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new WeaponState(definition, SimTick.Zero, hasFired: false);
    }

    public bool IsReady(SimTick currentTick)
    {
        if (!HasFired)
        {
            return true;
        }

        return currentTick.Value - LastFireTick.Value >= Definition.CooldownTicks;
    }

    public WeaponState MarkFired(SimTick fireTick)
        => new WeaponState(Definition, fireTick, hasFired: true);

    public bool Equals(WeaponState other)
        => ReferenceEquals(Definition, other.Definition)
           && LastFireTick.Equals(other.LastFireTick)
           && HasFired == other.HasFired;

    public override bool Equals(object? obj) => obj is WeaponState other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Definition, LastFireTick, HasFired);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "WeaponState(def={0}, lastFireTick={1}, hasFired={2})",
            Definition.Id,
            LastFireTick,
            HasFired);
}
