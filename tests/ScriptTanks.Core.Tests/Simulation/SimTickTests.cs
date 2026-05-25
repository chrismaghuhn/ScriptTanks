using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Tests.Simulation;

public sealed class SimTickTests
{
    [Fact]
    public void Zero_HasValueZero()
    {
        Assert.Equal(0, SimTick.Zero.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void Constructor_PreservesValidValue(int value)
    {
        Assert.Equal(value, new SimTick(value).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_RejectsNegativeValue(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimTick(value));
    }

    [Fact]
    public void Next_IncrementsValueByOne()
    {
        Assert.Equal(new SimTick(6), new SimTick(5).Next());
        Assert.Equal(new SimTick(1), SimTick.Zero.Next());
    }

    [Fact]
    public void IsAfterOrEqual_TrueWhenGreater()
    {
        Assert.True(new SimTick(6).IsAfterOrEqual(new SimTick(5)));
    }

    [Fact]
    public void IsAfterOrEqual_TrueWhenEqual()
    {
        Assert.True(new SimTick(5).IsAfterOrEqual(new SimTick(5)));
    }

    [Fact]
    public void IsAfterOrEqual_FalseWhenLess()
    {
        Assert.False(new SimTick(4).IsAfterOrEqual(new SimTick(5)));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        Assert.True(new SimTick(5) == new SimTick(5));
        Assert.False(new SimTick(5) == new SimTick(6));
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        Assert.True(new SimTick(5) != new SimTick(6));
        Assert.False(new SimTick(5) != new SimTick(5));
    }

    [Fact]
    public void ComparisonOperators_Work()
    {
        SimTick five = new SimTick(5);
        SimTick six = new SimTick(6);
        SimTick alsoFive = new SimTick(5);

        Assert.True(five < six);
        Assert.False(six < five);
        Assert.True(six > five);
        Assert.False(five > six);

        Assert.True(five <= alsoFive);
        Assert.True(five <= six);
        Assert.False(six <= five);

        Assert.True(five >= alsoFive);
        Assert.True(six >= five);
        Assert.False(five >= six);
    }

    [Fact]
    public void Equals_SimTick_AndObject_Work()
    {
        SimTick a = new SimTick(7);
        SimTick b = new SimTick(7);
        SimTick c = new SimTick(8);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals("not a SimTick"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        Assert.Equal(new SimTick(42).GetHashCode(), new SimTick(42).GetHashCode());
    }

    [Fact]
    public void CompareTo_ReturnsExpectedSign()
    {
        SimTick five = new SimTick(5);
        SimTick six = new SimTick(6);
        SimTick alsoFive = new SimTick(5);

        Assert.True(five.CompareTo(six) < 0);
        Assert.True(six.CompareTo(five) > 0);
        Assert.Equal(0, five.CompareTo(alsoFive));
    }

    [Fact]
    public void ToString_ContainsValue_SmokeOnly()
    {
        string text = new SimTick(5).ToString();

        Assert.Contains("5", text);
    }
}
