using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tanks;

/// <summary>
/// Deterministic numeric gameplay stats for a tank archetype. All values
/// are validated by the constructor: <see cref="MaxHitPoints"/> must be
/// positive, <see cref="ArmorReductionPercent"/> must be in the inclusive
/// range <c>[0, 95]</c> (95 instead of 100 prevents future damage immunity),
/// <see cref="HitboxRadius"/> must be positive, and the per-tick velocity
/// and turn-rate fields must be non-negative. Zero per-tick velocity and
/// zero turn rates are valid for static dummy targets and test scenarios.
/// <para>
/// <see cref="BodyTurnRatePerTick"/> and <see cref="TurretTurnRatePerTick"/>
/// are deterministic scalar values; this task does not yet fix the angle
/// unit (degrees vs. radians vs. fractional turns). The exact interpretation
/// is defined by the upcoming rotation system.
/// </para>
/// <para>
/// Note that <c>default(BasicTankStats)</c> is permitted by the C# struct
/// contract and yields zero values for every field, which is NOT considered
/// a valid gameplay stats object. Always construct
/// <see cref="BasicTankStats"/> through the explicit constructor to obtain
/// validated stats; this task adds no special handling for the default
/// value.
/// </para>
/// </summary>
public readonly struct BasicTankStats : IEquatable<BasicTankStats>
{
    public int MaxHitPoints { get; }

    public int ArmorReductionPercent { get; }

    public Fixed HitboxRadius { get; }

    public Fixed MaxVelocityPerTick { get; }

    public Fixed BodyTurnRatePerTick { get; }

    public Fixed TurretTurnRatePerTick { get; }

    public BasicTankStats(
        int maxHitPoints,
        int armorReductionPercent,
        Fixed hitboxRadius,
        Fixed maxVelocityPerTick,
        Fixed bodyTurnRatePerTick,
        Fixed turretTurnRatePerTick)
    {
        if (maxHitPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxHitPoints),
                maxHitPoints,
                "MaxHitPoints must be positive.");
        }

        if (armorReductionPercent < 0 || armorReductionPercent > 95)
        {
            throw new ArgumentOutOfRangeException(
                nameof(armorReductionPercent),
                armorReductionPercent,
                "ArmorReductionPercent must be in [0, 95].");
        }

        if (hitboxRadius <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hitboxRadius),
                "HitboxRadius must be positive.");
        }

        if (maxVelocityPerTick < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxVelocityPerTick),
                "MaxVelocityPerTick must not be negative.");
        }

        if (bodyTurnRatePerTick < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bodyTurnRatePerTick),
                "BodyTurnRatePerTick must not be negative.");
        }

        if (turretTurnRatePerTick < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(turretTurnRatePerTick),
                "TurretTurnRatePerTick must not be negative.");
        }

        MaxHitPoints = maxHitPoints;
        ArmorReductionPercent = armorReductionPercent;
        HitboxRadius = hitboxRadius;
        MaxVelocityPerTick = maxVelocityPerTick;
        BodyTurnRatePerTick = bodyTurnRatePerTick;
        TurretTurnRatePerTick = turretTurnRatePerTick;
    }

    public bool Equals(BasicTankStats other)
        => MaxHitPoints == other.MaxHitPoints
           && ArmorReductionPercent == other.ArmorReductionPercent
           && HitboxRadius.Equals(other.HitboxRadius)
           && MaxVelocityPerTick.Equals(other.MaxVelocityPerTick)
           && BodyTurnRatePerTick.Equals(other.BodyTurnRatePerTick)
           && TurretTurnRatePerTick.Equals(other.TurretTurnRatePerTick);

    public override bool Equals(object? obj) => obj is BasicTankStats other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(
            MaxHitPoints,
            ArmorReductionPercent,
            HitboxRadius,
            MaxVelocityPerTick,
            BodyTurnRatePerTick,
            TurretTurnRatePerTick);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "BasicTankStats(maxHp={0}, armor={1}, radius={2}, vmax={3}, body={4}, turret={5})",
            MaxHitPoints,
            ArmorReductionPercent,
            HitboxRadius,
            MaxVelocityPerTick,
            BodyTurnRatePerTick,
            TurretTurnRatePerTick);
}
