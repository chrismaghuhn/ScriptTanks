using System;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Geometry;

public sealed class CircleShapeTests
{
    private static CircleShape DefaultCircle()
        => new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(5));

    [Fact]
    public void Constructor_PreservesCenterAndRadius()
    {
        CircleShape circle = new CircleShape(FixedVec2.FromInts(3, 4), Fixed.FromInt(2));

        Assert.Equal(FixedVec2.FromInts(3, 4), circle.Center);
        Assert.Equal(Fixed.FromInt(2), circle.Radius);
    }

    [Fact]
    public void Constructor_RejectsZeroRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CircleShape(FixedVec2.Zero, Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CircleShape(FixedVec2.Zero, Fixed.FromInt(-1)));
    }

    [Fact]
    public void Contains_Center()
    {
        CircleShape circle = DefaultCircle();

        Assert.True(circle.Contains(circle.Center));
    }

    [Fact]
    public void Contains_PointOnRadius_OnX()
    {
        CircleShape circle = DefaultCircle();

        // (5, 0) lies exactly on the rim of a circle centered at origin with r=5.
        Assert.True(circle.Contains(FixedVec2.FromInts(5, 0)));
    }

    [Fact]
    public void Contains_PointOnRadius_DiagonalIntegerCase()
    {
        CircleShape circle = DefaultCircle();

        // (3, 4) -> distance² = 25 == radius² = 25, exact integer tangent.
        Assert.True(circle.Contains(FixedVec2.FromInts(3, 4)));
    }

    [Fact]
    public void Contains_InteriorPoint()
    {
        CircleShape circle = DefaultCircle();

        Assert.True(circle.Contains(FixedVec2.FromInts(1, 1)));
    }

    [Fact]
    public void Contains_PointOutside()
    {
        CircleShape circle = DefaultCircle();

        // (5, 1) -> distance² = 26 > 25.
        Assert.False(circle.Contains(FixedVec2.FromInts(5, 1)));
    }

    [Fact]
    public void Contains_RejectsRawOneUnitOutside()
    {
        CircleShape circle = DefaultCircle();

        // Radius.Raw = 5_000. Point at raw (5_001, 0) is 1 raw unit outside the rim.
        Assert.False(circle.Contains(FixedVec2.FromRaw(5_001, 0)));
    }

    [Fact]
    public void Equals_CircleShape_AndObject_Work()
    {
        CircleShape a = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(5));
        CircleShape b = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(5));
        CircleShape differentCenter = new CircleShape(FixedVec2.FromInts(1, 0), Fixed.FromInt(5));
        CircleShape differentRadius = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(6));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(differentCenter));
        Assert.False(a.Equals(differentRadius));
        Assert.False(a.Equals("not a CircleShape"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        CircleShape a = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(5));
        CircleShape b = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(5));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsCenterAndRadius_SmokeOnly()
    {
        CircleShape circle = DefaultCircle();

        string text = circle.ToString();

        Assert.Contains("center=", text);
        Assert.Contains("radius=", text);
    }
}
