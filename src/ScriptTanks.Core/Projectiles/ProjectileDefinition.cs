using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Immutable projectile archetype data. Contains only deterministic
/// projectile parameters (damage, speed-per-tick, max range, radius) and
/// performs strict input validation. The class does not move projectiles,
/// detect hits, resolve collisions, apply damage, integrate with weapons,
/// generate combat logs, or interact with match state.
/// <para>
/// <see cref="RawDamage"/> is bounded by <c>int.MaxValue / 100</c> so the
/// integer armor math in <see cref="ScriptTanks.Core.Combat.DamageApplication"/>
/// cannot overflow when this damage is later applied.
/// </para>
/// </summary>
public sealed class ProjectileDefinition
{
    public int RawDamage { get; }

    public Fixed SpeedPerTick { get; }

    public Fixed MaxRange { get; }

    public Fixed Radius { get; }

    public ProjectileDefinition(
        int rawDamage,
        Fixed speedPerTick,
        Fixed maxRange,
        Fixed radius)
    {
        if (rawDamage <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawDamage),
                rawDamage,
                "RawDamage must be positive.");
        }

        if (rawDamage > int.MaxValue / 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawDamage),
                rawDamage,
                "RawDamage is too large for deterministic int-based damage calculation.");
        }

        if (speedPerTick <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speedPerTick),
                "SpeedPerTick must be positive.");
        }

        if (maxRange <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRange),
                "MaxRange must be positive.");
        }

        if (radius <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Radius must be positive.");
        }

        RawDamage = rawDamage;
        SpeedPerTick = speedPerTick;
        MaxRange = maxRange;
        Radius = radius;
    }
}
