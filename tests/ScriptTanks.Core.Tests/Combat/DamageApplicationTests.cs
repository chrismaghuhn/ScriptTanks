using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class DamageApplicationTests
{
    private static TankState CreateTank(TankDefinition? definition = null, int? hp = null)
    {
        TankDefinition tankDefinition = definition ?? TankCatalog.BasicTank;
        TankState state = TankSpawnFactory.Create(
            new TankId(0),
            tankDefinition,
            ArenaCatalog.OpenTestArena.StartPositions[0]);

        if (hp.HasValue)
        {
            state = state.WithCurrentHitPoints(hp.Value);
        }

        return state;
    }

    private static TankDefinition CreateTankDefinitionWithArmor(int armorReductionPercent)
    {
        return new TankDefinition(
            id: $"armor_{armorReductionPercent}",
            displayName: $"Armor {armorReductionPercent}",
            description: "Test tank definition with custom armor.",
            stats: new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: armorReductionPercent,
                hitboxRadius: TankCatalog.BasicTank.Stats.HitboxRadius,
                maxVelocityPerTick: TankCatalog.BasicTank.Stats.MaxVelocityPerTick,
                bodyTurnRatePerTick: TankCatalog.BasicTank.Stats.BodyTurnRatePerTick,
                turretTurnRatePerTick: TankCatalog.BasicTank.Stats.TurretTurnRatePerTick),
            tags: new[] { "test" });
    }

    [Fact]
    public void Apply_RejectsNegativeRawDamage()
    {
        TankState tank = CreateTank();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => DamageApplication.Apply(tank, -1));
    }

    [Fact]
    public void Apply_RejectsRawDamageThatCouldOverflowIntCalculation()
    {
        TankState tank = CreateTank();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => DamageApplication.Apply(tank, int.MaxValue / 100 + 1));
    }

    [Fact]
    public void Apply_ZeroDamage_DoesNotChangeHitPoints()
    {
        TankState tank = CreateTank();

        TankState result = DamageApplication.Apply(tank, 0);

        Assert.Equal(tank.CurrentHitPoints, result.CurrentHitPoints);
    }

    [Fact]
    public void Apply_UsesArmorReduction()
    {
        TankState tank = CreateTank();

        TankState result = DamageApplication.Apply(tank, 10);

        Assert.Equal(92, result.CurrentHitPoints);
    }

    [Fact]
    public void Apply_UsesIntegerTruncation()
    {
        TankState tank = CreateTank();

        TankState result = DamageApplication.Apply(tank, 7);

        Assert.Equal(95, result.CurrentHitPoints);
    }

    [Fact]
    public void Apply_WithZeroArmor_AppliesFullDamage()
    {
        TankState tank = CreateTank(definition: CreateTankDefinitionWithArmor(0));

        TankState result = DamageApplication.Apply(tank, 10);

        Assert.Equal(90, result.CurrentHitPoints);
    }

    [Fact]
    public void Apply_WithHighArmor_AppliesReducedDamage()
    {
        TankState tank = CreateTank(definition: CreateTankDefinitionWithArmor(95));

        TankState result = DamageApplication.Apply(tank, 100);

        Assert.Equal(95, result.CurrentHitPoints);
    }

    [Fact]
    public void Apply_WhenEffectiveDamageTruncatesToZero_DoesNotChangeHitPoints()
    {
        TankState tank = CreateTank(definition: CreateTankDefinitionWithArmor(95));

        TankState result = DamageApplication.Apply(tank, 1);

        Assert.Equal(tank.CurrentHitPoints, result.CurrentHitPoints);
    }

    [Fact]
    public void Apply_DoesNotReduceHitPointsBelowZero()
    {
        TankState tank = CreateTank();

        TankState result = DamageApplication.Apply(tank, 1000);

        Assert.Equal(0, result.CurrentHitPoints);
        Assert.True(result.IsDestroyed);
    }

    [Fact]
    public void Apply_ToDestroyedTank_KeepsHitPointsAtZero()
    {
        TankState tank = CreateTank(hp: 0);

        TankState result = DamageApplication.Apply(tank, 10);

        Assert.Equal(0, result.CurrentHitPoints);
        Assert.True(result.IsDestroyed);
    }

    [Fact]
    public void Apply_PreservesTankId()
    {
        TankState original = CreateTank();
        TankState result = DamageApplication.Apply(original, 10);

        Assert.Equal(original.Id, result.Id);
    }

    [Fact]
    public void Apply_PreservesOwnerSlot()
    {
        TankState original = CreateTank();
        TankState result = DamageApplication.Apply(original, 10);

        Assert.Equal(original.OwnerSlot, result.OwnerSlot);
    }

    [Fact]
    public void Apply_PreservesDefinitionReference()
    {
        TankState original = CreateTank();
        TankState result = DamageApplication.Apply(original, 10);

        Assert.Same(original.Definition, result.Definition);
    }

    [Fact]
    public void Apply_PreservesMovement()
    {
        TankState original = CreateTank();
        TankState result = DamageApplication.Apply(original, 10);

        Assert.Equal(original.Movement, result.Movement);
    }

    [Fact]
    public void Apply_PreservesBodyRotation()
    {
        TankState original = CreateTank();
        TankState result = DamageApplication.Apply(original, 10);

        Assert.Equal(original.BodyRotation, result.BodyRotation);
    }

    [Fact]
    public void Apply_PreservesTurretRotation()
    {
        TankState original = CreateTank();
        TankState result = DamageApplication.Apply(original, 10);

        Assert.Equal(original.TurretRotation, result.TurretRotation);
    }
}
