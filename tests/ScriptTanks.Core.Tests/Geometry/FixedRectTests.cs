using System;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Geometry;

public sealed class FixedRectTests
{
    private static FixedRect DefaultRect()
        => new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(10, 10));

    [Fact]
    public void Constructor_PreservesMinAndMax()
    {
        FixedRect rect = DefaultRect();

        Assert.Equal(FixedVec2.FromInts(0, 0), rect.Min);
        Assert.Equal(FixedVec2.FromInts(10, 10), rect.Max);
    }

    [Fact]
    public void Constructor_DerivesWidthAndHeight()
    {
        FixedRect rect = new FixedRect(FixedVec2.FromInts(2, 3), FixedVec2.FromInts(12, 9));

        Assert.Equal(Fixed.FromInt(10), rect.Width);
        Assert.Equal(Fixed.FromInt(6), rect.Height);
    }

    [Fact]
    public void Constructor_RejectsMaxXEqualToMinX()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FixedRect(FixedVec2.FromInts(5, 0), FixedVec2.FromInts(5, 10)));
    }

    [Fact]
    public void Constructor_RejectsMaxXBelowMinX()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FixedRect(FixedVec2.FromInts(5, 0), FixedVec2.FromInts(4, 10)));
    }

    [Fact]
    public void Constructor_RejectsMaxYEqualToMinY()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FixedRect(FixedVec2.FromInts(0, 5), FixedVec2.FromInts(10, 5)));
    }

    [Fact]
    public void Constructor_RejectsMaxYBelowMinY()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FixedRect(FixedVec2.FromInts(0, 5), FixedVec2.FromInts(10, 4)));
    }

    [Fact]
    public void FromMinSize_SetsMinAndDerivesMax()
    {
        FixedRect rect = FixedRect.FromMinSize(
            FixedVec2.FromInts(2, 3),
            Fixed.FromInt(8),
            Fixed.FromInt(5));

        Assert.Equal(FixedVec2.FromInts(2, 3), rect.Min);
        Assert.Equal(FixedVec2.FromInts(10, 8), rect.Max);
        Assert.Equal(Fixed.FromInt(8), rect.Width);
        Assert.Equal(Fixed.FromInt(5), rect.Height);
    }

    [Fact]
    public void FromMinSize_RejectsZeroWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FixedRect.FromMinSize(FixedVec2.Zero, Fixed.Zero, Fixed.FromInt(5)));
    }

    [Fact]
    public void FromMinSize_RejectsNegativeWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FixedRect.FromMinSize(FixedVec2.Zero, Fixed.FromInt(-1), Fixed.FromInt(5)));
    }

    [Fact]
    public void FromMinSize_RejectsZeroHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FixedRect.FromMinSize(FixedVec2.Zero, Fixed.FromInt(5), Fixed.Zero));
    }

    [Fact]
    public void FromMinSize_RejectsNegativeHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FixedRect.FromMinSize(FixedVec2.Zero, Fixed.FromInt(5), Fixed.FromInt(-1)));
    }

    [Fact]
    public void Contains_CenterPoint()
    {
        FixedRect rect = DefaultRect();

        Assert.True(rect.Contains(FixedVec2.FromInts(5, 5)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 0)]
    [InlineData(0, 10)]
    [InlineData(10, 10)]
    public void Contains_CornerPoints_Inside(int x, int y)
    {
        FixedRect rect = DefaultRect();

        Assert.True(rect.Contains(FixedVec2.FromInts(x, y)));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(10, 5)]
    [InlineData(5, 0)]
    [InlineData(5, 10)]
    public void Contains_EdgePoints_Inside(int x, int y)
    {
        FixedRect rect = DefaultRect();

        Assert.True(rect.Contains(FixedVec2.FromInts(x, y)));
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(11, 5)]
    [InlineData(5, -1)]
    [InlineData(5, 11)]
    public void Contains_OutsidePoints_Excluded(int x, int y)
    {
        FixedRect rect = DefaultRect();

        Assert.False(rect.Contains(FixedVec2.FromInts(x, y)));
    }

    [Fact]
    public void Contains_RejectsRawJustBelowMinX()
    {
        FixedRect rect = DefaultRect();

        Assert.False(rect.Contains(FixedVec2.FromRaw(-1, 5_000)));
    }

    [Fact]
    public void Contains_RejectsRawJustAboveMaxX()
    {
        FixedRect rect = DefaultRect();

        // Max.X.Raw = 10_000 (10 * Scale). 10_001 is exactly 1 raw unit beyond.
        Assert.False(rect.Contains(FixedVec2.FromRaw(10_001, 5_000)));
    }

    [Fact]
    public void Equals_FixedRect_AndObject_Work()
    {
        FixedRect a = new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(10, 10));
        FixedRect b = new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(10, 10));
        FixedRect differentMin = new FixedRect(FixedVec2.FromInts(1, 0), FixedVec2.FromInts(10, 10));
        FixedRect differentMax = new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(11, 10));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(differentMin));
        Assert.False(a.Equals(differentMax));
        Assert.False(a.Equals("not a FixedRect"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        FixedRect a = new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(10, 10));
        FixedRect b = new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(10, 10));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsMinAndMax_SmokeOnly()
    {
        FixedRect rect = DefaultRect();

        string text = rect.ToString();

        // Min/Max raw values are 0 and 10_000 respectively under Fixed.Scale = 1000.
        Assert.Contains("min=", text);
        Assert.Contains("max=", text);
        Assert.Contains("10", text);
    }
}
