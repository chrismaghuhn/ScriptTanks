using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Tests.Ids;

public sealed class ProjectileIdTests
{
    [Fact]
    public void Constructor_PreservesValue()
    {
        ProjectileId id = new ProjectileId(42);

        Assert.Equal(42, id.Value);
    }

    [Fact]
    public void Constructor_AcceptsZero()
    {
        ProjectileId id = new ProjectileId(0);

        Assert.Equal(0, id.Value);
    }

    [Fact]
    public void Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectileId(-1));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        ProjectileId a = new ProjectileId(7);
        ProjectileId b = new ProjectileId(7);
        ProjectileId c = new ProjectileId(8);

        Assert.True(a == b);
        Assert.False(a == c);
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        ProjectileId a = new ProjectileId(7);
        ProjectileId b = new ProjectileId(7);
        ProjectileId c = new ProjectileId(8);

        Assert.False(a != b);
        Assert.True(a != c);
    }

    [Fact]
    public void LessThanOperator_Works()
    {
        ProjectileId a = new ProjectileId(1);
        ProjectileId aCopy = new ProjectileId(1);
        ProjectileId b = new ProjectileId(2);

        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < aCopy);
    }

    [Fact]
    public void GreaterThanOperator_Works()
    {
        ProjectileId a = new ProjectileId(1);
        ProjectileId aCopy = new ProjectileId(1);
        ProjectileId b = new ProjectileId(2);

        Assert.True(b > a);
        Assert.False(a > b);
        Assert.False(a > aCopy);
    }

    [Fact]
    public void LessOrEqualOperator_Works()
    {
        ProjectileId a = new ProjectileId(1);
        ProjectileId b = new ProjectileId(2);
        ProjectileId aCopy = new ProjectileId(1);

        Assert.True(a <= b);
        Assert.True(a <= aCopy);
        Assert.False(b <= a);
    }

    [Fact]
    public void GreaterOrEqualOperator_Works()
    {
        ProjectileId a = new ProjectileId(1);
        ProjectileId b = new ProjectileId(2);
        ProjectileId bCopy = new ProjectileId(2);

        Assert.True(b >= a);
        Assert.True(b >= bCopy);
        Assert.False(a >= b);
    }

    [Fact]
    public void Equals_T_Works()
    {
        ProjectileId a = new ProjectileId(7);
        ProjectileId b = new ProjectileId(7);
        ProjectileId c = new ProjectileId(8);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void Equals_Object_HandlesNullAndWrongType()
    {
        ProjectileId a = new ProjectileId(7);
        ProjectileId b = new ProjectileId(7);

        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("not a ProjectileId"));
        // A different ID type with the same numeric value must not equal a ProjectileId.
        Assert.False(a.Equals((object)new TankId(7)));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        ProjectileId a = new ProjectileId(42);
        ProjectileId b = new ProjectileId(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CompareTo_Less_Equal_Greater()
    {
        ProjectileId small = new ProjectileId(1);
        ProjectileId same = new ProjectileId(1);
        ProjectileId big = new ProjectileId(5);

        Assert.True(small.CompareTo(big) < 0);
        Assert.Equal(0, small.CompareTo(same));
        Assert.True(big.CompareTo(small) > 0);
    }

    [Fact]
    public void ToString_ReturnsExactNumericValue()
    {
        Assert.Equal("42", new ProjectileId(42).ToString());
        Assert.Equal("0", new ProjectileId(0).ToString());
    }
}
