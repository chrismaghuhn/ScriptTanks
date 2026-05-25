using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using Xunit;

namespace ScriptTanks.Core.Tests.Projectiles;

public sealed class ProjectileDefinitionTests
{
    private static ProjectileDefinition CreateValidDefinition(
        int rawDamage = 25,
        Fixed? speedPerTick = null,
        Fixed? maxRange = null,
        Fixed? radius = null)
    {
        return new ProjectileDefinition(
            rawDamage,
            speedPerTick ?? Fixed.FromInt(5),
            maxRange ?? Fixed.FromInt(40),
            radius ?? Fixed.FromRatio(1, 4));
    }

    [Fact]
    public void Constructor_PreservesRawDamage()
    {
        ProjectileDefinition definition = CreateValidDefinition(rawDamage: 37);

        Assert.Equal(37, definition.RawDamage);
    }

    [Fact]
    public void Constructor_PreservesSpeedPerTick()
    {
        Fixed speed = Fixed.FromRatio(7, 2);

        ProjectileDefinition definition = CreateValidDefinition(speedPerTick: speed);

        Assert.Equal(speed, definition.SpeedPerTick);
    }

    [Fact]
    public void Constructor_PreservesMaxRange()
    {
        Fixed range = Fixed.FromInt(123);

        ProjectileDefinition definition = CreateValidDefinition(maxRange: range);

        Assert.Equal(range, definition.MaxRange);
    }

    [Fact]
    public void Constructor_PreservesRadius()
    {
        Fixed radius = Fixed.FromRatio(3, 8);

        ProjectileDefinition definition = CreateValidDefinition(radius: radius);

        Assert.Equal(radius, definition.Radius);
    }

    [Fact]
    public void Constructor_RejectsZeroRawDamage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(rawDamage: 0));
    }

    [Fact]
    public void Constructor_RejectsNegativeRawDamage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(rawDamage: -1));
    }

    [Fact]
    public void Constructor_RejectsRawDamageAboveOverflowBound()
    {
        int aboveBound = (int.MaxValue / 100) + 1;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(rawDamage: aboveBound));
    }

    [Fact]
    public void Constructor_AcceptsRawDamageAtOverflowBound()
    {
        int atBound = int.MaxValue / 100;

        ProjectileDefinition definition = CreateValidDefinition(rawDamage: atBound);

        Assert.Equal(atBound, definition.RawDamage);
    }

    [Fact]
    public void Constructor_RejectsZeroSpeedPerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(speedPerTick: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeSpeedPerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(speedPerTick: Fixed.FromInt(-1)));
    }

    [Fact]
    public void Constructor_RejectsZeroMaxRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(maxRange: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeMaxRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(maxRange: Fixed.FromInt(-1)));
    }

    [Fact]
    public void Constructor_RejectsZeroRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(radius: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(radius: Fixed.FromInt(-1)));
    }
}
