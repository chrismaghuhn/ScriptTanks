using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Tests.Ids;

public sealed class PlayerSlotTests
{
    [Fact]
    public void Constructor_PreservesValue()
    {
        PlayerSlot slot = new PlayerSlot(42);

        Assert.Equal(42, slot.Value);
    }

    [Fact]
    public void Constructor_AcceptsZero()
    {
        PlayerSlot slot = new PlayerSlot(0);

        Assert.Equal(0, slot.Value);
    }

    [Fact]
    public void Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerSlot(-1));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        PlayerSlot a = new PlayerSlot(1);
        PlayerSlot b = new PlayerSlot(1);
        PlayerSlot c = new PlayerSlot(2);

        Assert.True(a == b);
        Assert.False(a == c);
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        PlayerSlot a = new PlayerSlot(1);
        PlayerSlot b = new PlayerSlot(1);
        PlayerSlot c = new PlayerSlot(2);

        Assert.False(a != b);
        Assert.True(a != c);
    }

    [Fact]
    public void LessThanOperator_Works()
    {
        PlayerSlot a = new PlayerSlot(0);
        PlayerSlot aCopy = new PlayerSlot(0);
        PlayerSlot b = new PlayerSlot(1);

        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < aCopy);
    }

    [Fact]
    public void GreaterThanOperator_Works()
    {
        PlayerSlot a = new PlayerSlot(0);
        PlayerSlot aCopy = new PlayerSlot(0);
        PlayerSlot b = new PlayerSlot(1);

        Assert.True(b > a);
        Assert.False(a > b);
        Assert.False(a > aCopy);
    }

    [Fact]
    public void LessOrEqualOperator_Works()
    {
        PlayerSlot a = new PlayerSlot(0);
        PlayerSlot b = new PlayerSlot(1);
        PlayerSlot aCopy = new PlayerSlot(0);

        Assert.True(a <= b);
        Assert.True(a <= aCopy);
        Assert.False(b <= a);
    }

    [Fact]
    public void GreaterOrEqualOperator_Works()
    {
        PlayerSlot a = new PlayerSlot(0);
        PlayerSlot b = new PlayerSlot(1);
        PlayerSlot bCopy = new PlayerSlot(1);

        Assert.True(b >= a);
        Assert.True(b >= bCopy);
        Assert.False(a >= b);
    }

    [Fact]
    public void Equals_T_Works()
    {
        PlayerSlot a = new PlayerSlot(1);
        PlayerSlot b = new PlayerSlot(1);
        PlayerSlot c = new PlayerSlot(2);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void Equals_Object_HandlesNullAndWrongType()
    {
        PlayerSlot a = new PlayerSlot(1);
        PlayerSlot b = new PlayerSlot(1);

        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("not a PlayerSlot"));
        // A different ID type with the same numeric value must not equal a PlayerSlot.
        Assert.False(a.Equals((object)new TankId(1)));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        PlayerSlot a = new PlayerSlot(42);
        PlayerSlot b = new PlayerSlot(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CompareTo_Less_Equal_Greater()
    {
        PlayerSlot small = new PlayerSlot(0);
        PlayerSlot same = new PlayerSlot(0);
        PlayerSlot big = new PlayerSlot(3);

        Assert.True(small.CompareTo(big) < 0);
        Assert.Equal(0, small.CompareTo(same));
        Assert.True(big.CompareTo(small) > 0);
    }

    [Fact]
    public void ToString_ReturnsExactNumericValue()
    {
        Assert.Equal("42", new PlayerSlot(42).ToString());
        Assert.Equal("0", new PlayerSlot(0).ToString());
    }
}
