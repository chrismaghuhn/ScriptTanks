using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Tests.Ids;

public sealed class TankIdTests
{
    [Fact]
    public void Constructor_PreservesValue()
    {
        TankId id = new TankId(42);

        Assert.Equal(42, id.Value);
    }

    [Fact]
    public void Constructor_AcceptsZero()
    {
        TankId id = new TankId(0);

        Assert.Equal(0, id.Value);
    }

    [Fact]
    public void Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TankId(-1));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        TankId a = new TankId(7);
        TankId b = new TankId(7);
        TankId c = new TankId(8);

        Assert.True(a == b);
        Assert.False(a == c);
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        TankId a = new TankId(7);
        TankId b = new TankId(7);
        TankId c = new TankId(8);

        Assert.False(a != b);
        Assert.True(a != c);
    }

    [Fact]
    public void LessThanOperator_Works()
    {
        TankId a = new TankId(1);
        TankId aCopy = new TankId(1);
        TankId b = new TankId(2);

        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < aCopy);
    }

    [Fact]
    public void GreaterThanOperator_Works()
    {
        TankId a = new TankId(1);
        TankId aCopy = new TankId(1);
        TankId b = new TankId(2);

        Assert.True(b > a);
        Assert.False(a > b);
        Assert.False(a > aCopy);
    }

    [Fact]
    public void LessOrEqualOperator_Works()
    {
        TankId a = new TankId(1);
        TankId b = new TankId(2);
        TankId aCopy = new TankId(1);

        Assert.True(a <= b);
        Assert.True(a <= aCopy);
        Assert.False(b <= a);
    }

    [Fact]
    public void GreaterOrEqualOperator_Works()
    {
        TankId a = new TankId(1);
        TankId b = new TankId(2);
        TankId bCopy = new TankId(2);

        Assert.True(b >= a);
        Assert.True(b >= bCopy);
        Assert.False(a >= b);
    }

    [Fact]
    public void Equals_T_Works()
    {
        TankId a = new TankId(7);
        TankId b = new TankId(7);
        TankId c = new TankId(8);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void Equals_Object_HandlesNullAndWrongType()
    {
        TankId a = new TankId(7);
        TankId b = new TankId(7);

        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("not a TankId"));
        // A different ID type with the same numeric value must not equal a TankId.
        Assert.False(a.Equals((object)new ProjectileId(7)));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        TankId a = new TankId(42);
        TankId b = new TankId(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CompareTo_Less_Equal_Greater()
    {
        TankId small = new TankId(1);
        TankId same = new TankId(1);
        TankId big = new TankId(5);

        Assert.True(small.CompareTo(big) < 0);
        Assert.Equal(0, small.CompareTo(same));
        Assert.True(big.CompareTo(small) > 0);
    }

    [Fact]
    public void ToString_ReturnsExactNumericValue()
    {
        Assert.Equal("42", new TankId(42).ToString());
        Assert.Equal("0", new TankId(0).ToString());
    }
}
