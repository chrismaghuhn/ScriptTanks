using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tanks;

/// <summary>
/// Built-in catalog of deterministic <see cref="TankDefinition"/> instances
/// used by spawn logic, match setup, hitbox creation, movement limits,
/// armor/damage logic, tests, and replay/debug output. The catalog exposes
/// a stable, deterministic order of tanks through <see cref="All"/> and a
/// strict ordinal id lookup through <see cref="GetById"/>. No file/JSON
/// loading, no procedural generation, and no simulation behavior is
/// performed here.
/// </summary>
public static class TankCatalog
{
    public static TankDefinition BasicTank { get; } = CreateBasicTank();

    public static TankDefinition LightTank { get; } = CreateLightTank();

    public static TankDefinition HeavyTank { get; } = CreateHeavyTank();

    public static IReadOnlyList<TankDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            BasicTank,
            LightTank,
            HeavyTank,
        });

    public static TankDefinition GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        foreach (TankDefinition tank in All)
        {
            if (string.Equals(tank.Id, id, StringComparison.Ordinal))
            {
                return tank;
            }
        }

        throw new KeyNotFoundException($"No tank definition found for id '{id}'.");
    }

    private static TankDefinition CreateBasicTank()
    {
        return new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: "Baseline deterministic test tank.",
            stats: new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)),
            tags: new[] { "test", "basic" });
    }

    private static TankDefinition CreateLightTank()
    {
        return new TankDefinition(
            id: "light_tank",
            displayName: "Light Tank",
            description: "Fast low-armor test tank for validating movement-heavy behavior.",
            stats: new BasicTankStats(
                maxHitPoints: 70,
                armorReductionPercent: 10,
                hitboxRadius: Fixed.FromRatio(7, 4),
                maxVelocityPerTick: Fixed.FromRatio(16, 100),
                bodyTurnRatePerTick: Fixed.FromRatio(8, 100),
                turretTurnRatePerTick: Fixed.FromRatio(12, 100)),
            tags: new[] { "test", "light", "fast" });
    }

    private static TankDefinition CreateHeavyTank()
    {
        return new TankDefinition(
            id: "heavy_tank",
            displayName: "Heavy Tank",
            description: "Slow durable test tank for validating armor-heavy behavior.",
            stats: new BasicTankStats(
                maxHitPoints: 160,
                armorReductionPercent: 35,
                hitboxRadius: Fixed.FromRatio(5, 2),
                maxVelocityPerTick: Fixed.FromRatio(6, 100),
                bodyTurnRatePerTick: Fixed.FromRatio(3, 100),
                turretTurnRatePerTick: Fixed.FromRatio(7, 100)),
            tags: new[] { "test", "heavy", "armored" });
    }
}
