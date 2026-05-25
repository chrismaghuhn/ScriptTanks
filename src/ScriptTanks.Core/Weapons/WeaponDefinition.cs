using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Weapons;

/// <summary>
/// Immutable weapon archetype data. Aggregates metadata
/// (<see cref="Id"/>, <see cref="DisplayName"/>, <see cref="Description"/>,
/// <see cref="Tags"/>) with deterministic damage, cooldown, and projectile
/// stats. The class is a pure data carrier: no cooldown ticking, fire
/// requests, projectile spawning, hit detection, or damage integration is
/// performed here. Tags are defensively copied on construction and exposed
/// only as <see cref="IReadOnlyList{T}"/>.
/// <para>
/// <see cref="RawDamage"/> is bounded by <c>int.MaxValue / 100</c> so the
/// integer armor math in <see cref="ScriptTanks.Core.Combat.DamageApplication"/>
/// cannot overflow when this damage is later applied.
/// </para>
/// </summary>
public sealed class WeaponDefinition
{
    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public int RawDamage { get; }

    public int CooldownTicks { get; }

    public Fixed ProjectileSpeedPerTick { get; }

    public Fixed ProjectileRange { get; }

    public Fixed ProjectileRadius { get; }

    public Fixed MuzzleOffsetFromCenter { get; }

    public IReadOnlyList<string> Tags { get; }

    public WeaponDefinition(
        string id,
        string displayName,
        string description,
        int rawDamage,
        int cooldownTicks,
        Fixed projectileSpeedPerTick,
        Fixed projectileRange,
        Fixed projectileRadius,
        Fixed muzzleOffsetFromCenter,
        IEnumerable<string> tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(tags);

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

        if (cooldownTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cooldownTicks),
                cooldownTicks,
                "CooldownTicks must be positive.");
        }

        if (projectileSpeedPerTick <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectileSpeedPerTick),
                "ProjectileSpeedPerTick must be positive.");
        }

        if (projectileRange <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectileRange),
                "ProjectileRange must be positive.");
        }

        if (projectileRadius <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectileRadius),
                "ProjectileRadius must be positive.");
        }

        if (muzzleOffsetFromCenter <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(muzzleOffsetFromCenter),
                muzzleOffsetFromCenter,
                "MuzzleOffsetFromCenter must be positive.");
        }

        string[] tagArray = tags.ToArray();

        foreach (string tag in tagArray)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException(
                    "Tag must not be null, empty, or whitespace.",
                    nameof(tags));
            }
        }

        Id = id;
        DisplayName = displayName;
        Description = description;
        RawDamage = rawDamage;
        CooldownTicks = cooldownTicks;
        ProjectileSpeedPerTick = projectileSpeedPerTick;
        ProjectileRange = projectileRange;
        ProjectileRadius = projectileRadius;
        MuzzleOffsetFromCenter = muzzleOffsetFromCenter;
        Tags = tagArray;
    }
}
