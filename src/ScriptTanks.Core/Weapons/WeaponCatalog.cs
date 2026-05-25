using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Weapons;

/// <summary>
/// Built-in catalog of deterministic <see cref="WeaponDefinition"/>
/// instances used by later projectile spawning, cooldown ticking, hit
/// resolution, replay/debug output, and scripting conditions. The catalog
/// exposes a stable, deterministic order of weapons through
/// <see cref="All"/> and a strict ordinal id lookup through
/// <see cref="GetById"/>. No file/JSON loading, no procedural generation,
/// and no simulation behavior is performed here.
/// </summary>
public static class WeaponCatalog
{
    public static WeaponDefinition StandardCannon { get; } = CreateStandardCannon();

    public static WeaponDefinition ShotgunCannon { get; } = CreateShotgunCannon();

    public static WeaponDefinition Railgun { get; } = CreateRailgun();

    public static IReadOnlyList<WeaponDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            StandardCannon,
            ShotgunCannon,
            Railgun,
        });

    public static WeaponDefinition GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        foreach (WeaponDefinition weapon in All)
        {
            if (string.Equals(weapon.Id, id, StringComparison.Ordinal))
            {
                return weapon;
            }
        }

        throw new KeyNotFoundException($"No weapon definition found for id '{id}'.");
    }

    private static WeaponDefinition CreateStandardCannon()
    {
        return new WeaponDefinition(
            id: "standard_cannon",
            displayName: "Standard Cannon",
            description: "Balanced baseline cannon for deterministic weapon tests.",
            rawDamage: 25,
            cooldownTicks: 30,
            projectileSpeedPerTick: Fixed.FromInt(1),
            projectileRange: Fixed.FromInt(40),
            projectileRadius: Fixed.FromRatio(1, 4),
            muzzleOffsetFromCenter: Fixed.FromInt(1),
            tags: new[] { "test", "cannon", "standard" });
    }

    private static WeaponDefinition CreateShotgunCannon()
    {
        return new WeaponDefinition(
            id: "shotgun_cannon",
            displayName: "Shotgun Cannon",
            description: "Short-range high-impact cannon for close-distance weapon tests.",
            rawDamage: 18,
            cooldownTicks: 24,
            projectileSpeedPerTick: Fixed.FromRatio(8, 10),
            projectileRange: Fixed.FromInt(18),
            projectileRadius: Fixed.FromRatio(35, 100),
            muzzleOffsetFromCenter: Fixed.FromInt(1),
            tags: new[] { "test", "cannon", "close_range" });
    }

    private static WeaponDefinition CreateRailgun()
    {
        return new WeaponDefinition(
            id: "railgun",
            displayName: "Railgun",
            description: "Long-range high-damage cannon for precision weapon tests.",
            rawDamage: 45,
            cooldownTicks: 60,
            projectileSpeedPerTick: Fixed.FromInt(2),
            projectileRange: Fixed.FromInt(70),
            projectileRadius: Fixed.FromRatio(15, 100),
            muzzleOffsetFromCenter: Fixed.FromInt(2),
            tags: new[] { "test", "cannon", "precision" });
    }
}
