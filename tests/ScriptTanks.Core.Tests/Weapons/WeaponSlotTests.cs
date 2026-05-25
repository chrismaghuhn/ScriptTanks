using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Weapons;

public sealed class WeaponSlotTests
{
    [Fact]
    public void Constructor_PreservesValue()
    {
        WeaponSlot slot = new WeaponSlot(42);

        Assert.Equal(42, slot.Value);
    }

    [Fact]
    public void Constructor_AcceptsZero()
    {
        WeaponSlot slot = new WeaponSlot(0);

        Assert.Equal(0, slot.Value);
    }

    [Fact]
    public void Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponSlot(-1));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        WeaponSlot a = new WeaponSlot(1);
        WeaponSlot b = new WeaponSlot(1);
        WeaponSlot c = new WeaponSlot(2);

        Assert.True(a == b);
        Assert.False(a == c);
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        WeaponSlot a = new WeaponSlot(1);
        WeaponSlot b = new WeaponSlot(1);
        WeaponSlot c = new WeaponSlot(2);

        Assert.False(a != b);
        Assert.True(a != c);
    }

    [Fact]
    public void LessThanOperator_Works()
    {
        WeaponSlot a = new WeaponSlot(0);
        WeaponSlot aCopy = new WeaponSlot(0);
        WeaponSlot b = new WeaponSlot(1);

        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < aCopy);
    }

    [Fact]
    public void GreaterThanOperator_Works()
    {
        WeaponSlot a = new WeaponSlot(0);
        WeaponSlot aCopy = new WeaponSlot(0);
        WeaponSlot b = new WeaponSlot(1);

        Assert.True(b > a);
        Assert.False(a > b);
        Assert.False(a > aCopy);
    }

    [Fact]
    public void LessOrEqualOperator_Works()
    {
        WeaponSlot a = new WeaponSlot(0);
        WeaponSlot b = new WeaponSlot(1);
        WeaponSlot aCopy = new WeaponSlot(0);

        Assert.True(a <= b);
        Assert.True(a <= aCopy);
        Assert.False(b <= a);
    }

    [Fact]
    public void GreaterOrEqualOperator_Works()
    {
        WeaponSlot a = new WeaponSlot(0);
        WeaponSlot b = new WeaponSlot(1);
        WeaponSlot bCopy = new WeaponSlot(1);

        Assert.True(b >= a);
        Assert.True(b >= bCopy);
        Assert.False(a >= b);
    }

    [Fact]
    public void Equals_T_Works()
    {
        WeaponSlot a = new WeaponSlot(1);
        WeaponSlot b = new WeaponSlot(1);
        WeaponSlot c = new WeaponSlot(2);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void Equals_Object_HandlesNullAndWrongType()
    {
        WeaponSlot a = new WeaponSlot(1);
        WeaponSlot b = new WeaponSlot(1);

        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("not a WeaponSlot"));
        // Different ID/slot types with the same numeric value must not equal a WeaponSlot.
        Assert.False(a.Equals((object)new TankId(1)));
        Assert.False(a.Equals((object)new PlayerSlot(1)));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        WeaponSlot a = new WeaponSlot(42);
        WeaponSlot b = new WeaponSlot(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CompareTo_Less_Equal_Greater()
    {
        WeaponSlot small = new WeaponSlot(0);
        WeaponSlot same = new WeaponSlot(0);
        WeaponSlot big = new WeaponSlot(3);

        Assert.True(small.CompareTo(big) < 0);
        Assert.Equal(0, small.CompareTo(same));
        Assert.True(big.CompareTo(small) > 0);
    }

    [Fact]
    public void ToString_ReturnsExactNumericValue()
    {
        Assert.Equal("42", new WeaponSlot(42).ToString());
        Assert.Equal("0", new WeaponSlot(0).ToString());
    }
}
