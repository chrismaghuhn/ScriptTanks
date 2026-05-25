using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Tanks;

public sealed class TankCatalogTests
{
    private static void AssertStats(
        int hp,
        int armor,
        Fixed radius,
        Fixed maxVelocity,
        Fixed bodyTurnRate,
        Fixed turretTurnRate,
        BasicTankStats actual)
    {
        Assert.Equal(hp, actual.MaxHitPoints);
        Assert.Equal(armor, actual.ArmorReductionPercent);
        Assert.Equal(radius, actual.HitboxRadius);
        Assert.Equal(maxVelocity, actual.MaxVelocityPerTick);
        Assert.Equal(bodyTurnRate, actual.BodyTurnRatePerTick);
        Assert.Equal(turretTurnRate, actual.TurretTurnRatePerTick);
    }

    [Fact]
    public void BasicTank_IsNotNull()
    {
        Assert.NotNull(TankCatalog.BasicTank);
    }

    [Fact]
    public void BasicTank_HasExpectedId()
    {
        Assert.Equal("basic_tank", TankCatalog.BasicTank.Id);
    }

    [Fact]
    public void BasicTank_HasExpectedDisplayName()
    {
        Assert.Equal("Basic Tank", TankCatalog.BasicTank.DisplayName);
    }

    [Fact]
    public void BasicTank_HasExpectedDescription()
    {
        Assert.Equal(
            "Baseline deterministic test tank.",
            TankCatalog.BasicTank.Description);
    }

    [Fact]
    public void BasicTank_HasExpectedStats()
    {
        AssertStats(
            hp: 100,
            armor: 20,
            radius: Fixed.FromInt(2),
            maxVelocity: Fixed.FromRatio(1, 10),
            bodyTurnRate: Fixed.FromRatio(1, 20),
            turretTurnRate: Fixed.FromRatio(1, 10),
            actual: TankCatalog.BasicTank.Stats);
    }

    [Fact]
    public void BasicTank_TagsPreserveOrder()
    {
        IReadOnlyList<string> tags = TankCatalog.BasicTank.Tags;

        Assert.Equal(2, tags.Count);
        Assert.Equal("test", tags[0]);
        Assert.Equal("basic", tags[1]);
    }

    [Fact]
    public void LightTank_IsNotNull()
    {
        Assert.NotNull(TankCatalog.LightTank);
    }

    [Fact]
    public void LightTank_HasExpectedId()
    {
        Assert.Equal("light_tank", TankCatalog.LightTank.Id);
    }

    [Fact]
    public void LightTank_HasExpectedDisplayName()
    {
        Assert.Equal("Light Tank", TankCatalog.LightTank.DisplayName);
    }

    [Fact]
    public void LightTank_HasExpectedDescription()
    {
        Assert.Equal(
            "Fast low-armor test tank for validating movement-heavy behavior.",
            TankCatalog.LightTank.Description);
    }

    [Fact]
    public void LightTank_HasExpectedStats()
    {
        AssertStats(
            hp: 70,
            armor: 10,
            radius: Fixed.FromRatio(7, 4),
            maxVelocity: Fixed.FromRatio(16, 100),
            bodyTurnRate: Fixed.FromRatio(8, 100),
            turretTurnRate: Fixed.FromRatio(12, 100),
            actual: TankCatalog.LightTank.Stats);
    }

    [Fact]
    public void LightTank_TagsPreserveOrder()
    {
        IReadOnlyList<string> tags = TankCatalog.LightTank.Tags;

        Assert.Equal(3, tags.Count);
        Assert.Equal("test", tags[0]);
        Assert.Equal("light", tags[1]);
        Assert.Equal("fast", tags[2]);
    }

    [Fact]
    public void HeavyTank_IsNotNull()
    {
        Assert.NotNull(TankCatalog.HeavyTank);
    }

    [Fact]
    public void HeavyTank_HasExpectedId()
    {
        Assert.Equal("heavy_tank", TankCatalog.HeavyTank.Id);
    }

    [Fact]
    public void HeavyTank_HasExpectedDisplayName()
    {
        Assert.Equal("Heavy Tank", TankCatalog.HeavyTank.DisplayName);
    }

    [Fact]
    public void HeavyTank_HasExpectedDescription()
    {
        Assert.Equal(
            "Slow durable test tank for validating armor-heavy behavior.",
            TankCatalog.HeavyTank.Description);
    }

    [Fact]
    public void HeavyTank_HasExpectedStats()
    {
        AssertStats(
            hp: 160,
            armor: 35,
            radius: Fixed.FromRatio(5, 2),
            maxVelocity: Fixed.FromRatio(6, 100),
            bodyTurnRate: Fixed.FromRatio(3, 100),
            turretTurnRate: Fixed.FromRatio(7, 100),
            actual: TankCatalog.HeavyTank.Stats);
    }

    [Fact]
    public void HeavyTank_TagsPreserveOrder()
    {
        IReadOnlyList<string> tags = TankCatalog.HeavyTank.Tags;

        Assert.Equal(3, tags.Count);
        Assert.Equal("test", tags[0]);
        Assert.Equal("heavy", tags[1]);
        Assert.Equal("armored", tags[2]);
    }

    [Fact]
    public void All_HasExpectedCount()
    {
        Assert.Equal(3, TankCatalog.All.Count);
    }

    [Fact]
    public void All_FirstEntry_IsBasicTank()
    {
        Assert.Same(TankCatalog.BasicTank, TankCatalog.All[0]);
    }

    [Fact]
    public void All_SecondEntry_IsLightTank()
    {
        Assert.Same(TankCatalog.LightTank, TankCatalog.All[1]);
    }

    [Fact]
    public void All_ThirdEntry_IsHeavyTank()
    {
        Assert.Same(TankCatalog.HeavyTank, TankCatalog.All[2]);
    }

    [Fact]
    public void All_IsNotMutableArray()
    {
        Assert.IsNotType<TankDefinition[]>(TankCatalog.All);
    }

    [Fact]
    public void GetById_BasicTank_ReturnsSameInstance()
    {
        Assert.Same(TankCatalog.BasicTank, TankCatalog.GetById("basic_tank"));
    }

    [Fact]
    public void GetById_LightTank_ReturnsSameInstance()
    {
        Assert.Same(TankCatalog.LightTank, TankCatalog.GetById("light_tank"));
    }

    [Fact]
    public void GetById_HeavyTank_ReturnsSameInstance()
    {
        Assert.Same(TankCatalog.HeavyTank, TankCatalog.GetById("heavy_tank"));
    }

    [Fact]
    public void GetById_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TankCatalog.GetById(null!));
    }

    [Fact]
    public void GetById_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TankCatalog.GetById(string.Empty));
    }

    [Fact]
    public void GetById_WhitespaceId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TankCatalog.GetById("   "));
    }

    [Fact]
    public void GetById_UpperCaseId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => TankCatalog.GetById("BASIC_TANK"));
    }

    [Fact]
    public void GetById_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => TankCatalog.GetById("unknown"));
    }
}
