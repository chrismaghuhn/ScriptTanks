using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Tanks;

public sealed class BasicTankStatsTests
{
    private static BasicTankStats DefaultStats()
        => new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 20,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.FromRatio(1, 10),
            bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
            turretTurnRatePerTick: Fixed.FromRatio(1, 10));

    [Fact]
    public void Constructor_PreservesMaxHitPoints()
    {
        BasicTankStats stats = DefaultStats();
        Assert.Equal(100, stats.MaxHitPoints);
    }

    [Fact]
    public void Constructor_PreservesArmorReductionPercent()
    {
        BasicTankStats stats = DefaultStats();
        Assert.Equal(20, stats.ArmorReductionPercent);
    }

    [Fact]
    public void Constructor_PreservesHitboxRadius()
    {
        BasicTankStats stats = DefaultStats();
        Assert.Equal(Fixed.FromInt(2), stats.HitboxRadius);
    }

    [Fact]
    public void Constructor_PreservesMaxVelocityPerTick()
    {
        BasicTankStats stats = DefaultStats();
        Assert.Equal(Fixed.FromRatio(1, 10), stats.MaxVelocityPerTick);
    }

    [Fact]
    public void Constructor_PreservesBodyTurnRatePerTick()
    {
        BasicTankStats stats = DefaultStats();
        Assert.Equal(Fixed.FromRatio(1, 20), stats.BodyTurnRatePerTick);
    }

    [Fact]
    public void Constructor_PreservesTurretTurnRatePerTick()
    {
        BasicTankStats stats = DefaultStats();
        Assert.Equal(Fixed.FromRatio(1, 10), stats.TurretTurnRatePerTick);
    }

    [Fact]
    public void Constructor_RejectsZeroMaxHitPoints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 0,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_RejectsNegativeMaxHitPoints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: -1,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_AcceptsArmorReductionPercentZero()
    {
        BasicTankStats stats = new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 0,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.FromRatio(1, 10),
            bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
            turretTurnRatePerTick: Fixed.FromRatio(1, 10));

        Assert.Equal(0, stats.ArmorReductionPercent);
    }

    [Fact]
    public void Constructor_AcceptsArmorReductionPercentNinetyFive()
    {
        BasicTankStats stats = new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 95,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.FromRatio(1, 10),
            bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
            turretTurnRatePerTick: Fixed.FromRatio(1, 10));

        Assert.Equal(95, stats.ArmorReductionPercent);
    }

    [Fact]
    public void Constructor_RejectsNegativeArmorReductionPercent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: -1,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Theory]
    [InlineData(96)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Constructor_RejectsArmorReductionPercentAbove95(int armor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: armor,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_RejectsZeroHitboxRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.Zero,
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_RejectsNegativeHitboxRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromRaw(-1),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_AcceptsZeroMaxVelocityPerTick()
    {
        BasicTankStats stats = new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 20,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.Zero,
            bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
            turretTurnRatePerTick: Fixed.FromRatio(1, 10));

        Assert.Equal(Fixed.Zero, stats.MaxVelocityPerTick);
    }

    [Fact]
    public void Constructor_RejectsNegativeMaxVelocityPerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRaw(-1),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_AcceptsZeroBodyTurnRatePerTick()
    {
        BasicTankStats stats = new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 20,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.FromRatio(1, 10),
            bodyTurnRatePerTick: Fixed.Zero,
            turretTurnRatePerTick: Fixed.FromRatio(1, 10));

        Assert.Equal(Fixed.Zero, stats.BodyTurnRatePerTick);
    }

    [Fact]
    public void Constructor_RejectsNegativeBodyTurnRatePerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRaw(-1),
                turretTurnRatePerTick: Fixed.FromRatio(1, 10)));
    }

    [Fact]
    public void Constructor_AcceptsZeroTurretTurnRatePerTick()
    {
        BasicTankStats stats = new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 20,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.FromRatio(1, 10),
            bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
            turretTurnRatePerTick: Fixed.Zero);

        Assert.Equal(Fixed.Zero, stats.TurretTurnRatePerTick);
    }

    [Fact]
    public void Constructor_RejectsNegativeTurretTurnRatePerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BasicTankStats(
                maxHitPoints: 100,
                armorReductionPercent: 20,
                hitboxRadius: Fixed.FromInt(2),
                maxVelocityPerTick: Fixed.FromRatio(1, 10),
                bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
                turretTurnRatePerTick: Fixed.FromRaw(-1)));
    }

    [Fact]
    public void Equals_BasicTankStats_AndObject_Work()
    {
        BasicTankStats a = DefaultStats();
        BasicTankStats b = DefaultStats();

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals((object?)null));
        Assert.False(a.Equals("not a stats"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Equals_ReturnsFalse_ForChangedField(int fieldIndex)
    {
        BasicTankStats baseStats = DefaultStats();

        int maxHp = 100;
        int armor = 20;
        Fixed radius = Fixed.FromInt(2);
        Fixed vmax = Fixed.FromRatio(1, 10);
        Fixed body = Fixed.FromRatio(1, 20);
        Fixed turret = Fixed.FromRatio(1, 10);

        switch (fieldIndex)
        {
            case 0: maxHp = 101; break;
            case 1: armor = 21; break;
            case 2: radius = Fixed.FromInt(3); break;
            case 3: vmax = Fixed.FromRatio(2, 10); break;
            case 4: body = Fixed.FromRatio(2, 20); break;
            case 5: turret = Fixed.FromRatio(2, 10); break;
        }

        BasicTankStats changed = new BasicTankStats(maxHp, armor, radius, vmax, body, turret);

        Assert.False(baseStats.Equals(changed));
        Assert.False(baseStats.Equals((object)changed));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        BasicTankStats a = DefaultStats();
        BasicTankStats b = DefaultStats();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsValues_SmokeOnly()
    {
        BasicTankStats stats = DefaultStats();
        string text = stats.ToString();

        Assert.Contains("100", text);
        Assert.Contains("20", text);
    }
}
