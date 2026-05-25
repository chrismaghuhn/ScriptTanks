using System;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class SensorSlotTests
{
    [Fact]
    public void Constructor_PreservesValue()
    {
        SensorSlot slot = new SensorSlot(7);

        Assert.Equal(7, slot.Value);
    }

    [Fact]
    public void Zero_ReturnsSlotZero()
    {
        Assert.Equal(0, SensorSlot.Zero.Value);
    }

    [Fact]
    public void Constructor_RejectsNegativeValue()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SensorSlot(-1));

        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameValue()
    {
        SensorSlot a = new SensorSlot(3);
        SensorSlot b = new SensorSlot(3);

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentValue()
    {
        SensorSlot a = new SensorSlot(3);
        SensorSlot b = new SensorSlot(4);

        Assert.NotEqual(a, b);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Operators_MatchEquals()
    {
        SensorSlot a = new SensorSlot(3);
        SensorSlot b = new SensorSlot(3);
        SensorSlot different = new SensorSlot(5);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.False(a == different);
        Assert.True(a != different);
    }

    [Fact]
    public void ToString_ReturnsInvariantNumericValue()
    {
        Assert.Equal("3", new SensorSlot(3).ToString());
    }
}
