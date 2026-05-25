using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class SensorDefinitionTests
{
    private static SensorDefinition CreateDefinition(
        string id = "sensor",
        string displayName = "Sensor",
        Fixed? range = null,
        Fixed? coneAngleDegrees = null,
        int cooldownTicks = 5,
        int cpuCost = 3)
    {
        return new SensorDefinition(
            id,
            displayName,
            range ?? Fixed.FromInt(10),
            coneAngleDegrees ?? Fixed.FromInt(90),
            cooldownTicks,
            cpuCost);
    }

    [Fact]
    public void Constructor_PreservesValues()
    {
        SensorDefinition definition = new SensorDefinition(
            "scout_radar",
            "Scout Radar",
            Fixed.FromInt(20),
            Fixed.FromInt(45),
            cooldownTicks: 7,
            cpuCost: 4);

        Assert.Equal("scout_radar", definition.Id);
        Assert.Equal("Scout Radar", definition.DisplayName);
        Assert.Equal(Fixed.FromInt(20), definition.Range);
        Assert.Equal(Fixed.FromInt(45), definition.ConeAngleDegrees);
        Assert.Equal(7, definition.CooldownTicks);
        Assert.Equal(4, definition.CpuCost);
    }

    [Fact]
    public void Constructor_RejectsNullId()
    {
        ArgumentException ex = Assert.Throws<ArgumentNullException>(
            () => CreateDefinition(id: null!));
        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateDefinition(id: string.Empty));
        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsWhitespaceId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateDefinition(id: "   "));
        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullDisplayName()
    {
        ArgumentException ex = Assert.Throws<ArgumentNullException>(
            () => CreateDefinition(displayName: null!));
        Assert.Equal("displayName", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyDisplayName()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateDefinition(displayName: string.Empty));
        Assert.Equal("displayName", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsWhitespaceDisplayName()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateDefinition(displayName: " "));
        Assert.Equal("displayName", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveRange()
    {
        ArgumentOutOfRangeException zero = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(range: Fixed.Zero));
        Assert.Equal("range", zero.ParamName);

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(range: -Fixed.One));
        Assert.Equal("range", negative.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveConeAngle()
    {
        ArgumentOutOfRangeException zero = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(coneAngleDegrees: Fixed.Zero));
        Assert.Equal("coneAngleDegrees", zero.ParamName);

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(coneAngleDegrees: -Fixed.One));
        Assert.Equal("coneAngleDegrees", negative.ParamName);
    }

    [Fact]
    public void Constructor_RejectsConeAngleAbove360()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(coneAngleDegrees: Fixed.FromInt(361)));
        Assert.Equal("coneAngleDegrees", ex.ParamName);

        SensorDefinition boundary = CreateDefinition(coneAngleDegrees: Fixed.FromInt(360));
        Assert.Equal(Fixed.FromInt(360), boundary.ConeAngleDegrees);
    }

    [Fact]
    public void Constructor_RejectsNegativeCooldownTicks()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(cooldownTicks: -1));
        Assert.Equal("cooldownTicks", ex.ParamName);

        SensorDefinition zero = CreateDefinition(cooldownTicks: 0);
        Assert.Equal(0, zero.CooldownTicks);
    }

    [Fact]
    public void Constructor_RejectsNegativeCpuCost()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(cpuCost: -1));
        Assert.Equal("cpuCost", ex.ParamName);

        SensorDefinition zero = CreateDefinition(cpuCost: 0);
        Assert.Equal(0, zero.CpuCost);
    }

    [Fact]
    public void Equality_OperatorsHashCodeAndToString_Work()
    {
        SensorDefinition a = CreateDefinition();
        SensorDefinition b = CreateDefinition();
        SensorDefinition different = CreateDefinition(id: "other");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.NotEqual(a, different);
        Assert.False(a == different);
        Assert.True(a != different);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        string text = a.ToString();
        Assert.False(string.IsNullOrWhiteSpace(text));
    }
}
