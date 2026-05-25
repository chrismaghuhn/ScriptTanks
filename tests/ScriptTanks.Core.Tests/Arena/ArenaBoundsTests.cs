using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Arena;

public sealed class ArenaBoundsTests
{
    private static ArenaBounds DefaultBounds()
        => new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60));

    [Fact]
    public void Constructor_PreservesWidthAndHeight()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.Equal(Fixed.FromInt(100), bounds.Width);
        Assert.Equal(Fixed.FromInt(60), bounds.Height);
    }

    [Fact]
    public void Min_IsAlwaysZero()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.Equal(FixedVec2.Zero, bounds.Min);
    }

    [Fact]
    public void Max_IsWidthAndHeight()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.Equal(FixedVec2.FromInts(100, 60), bounds.Max);
    }

    [Fact]
    public void Constructor_RejectsZeroWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaBounds(Fixed.Zero, Fixed.FromInt(60)));
    }

    [Fact]
    public void Constructor_RejectsNegativeWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaBounds(Fixed.FromInt(-1), Fixed.FromInt(60)));
    }

    [Fact]
    public void Constructor_RejectsZeroHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaBounds(Fixed.FromInt(100), Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(-1)));
    }

    [Fact]
    public void Contains_CenterPoint()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.True(bounds.Contains(FixedVec2.FromInts(50, 30)));
    }

    [Fact]
    public void Contains_MinCorner()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.True(bounds.Contains(FixedVec2.Zero));
    }

    [Fact]
    public void Contains_MaxCorner()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.True(bounds.Contains(FixedVec2.FromInts(100, 60)));
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(0, 60)]
    [InlineData(100, 30)]
    [InlineData(50, 60)]
    public void Contains_EdgePoints_Inside(int x, int y)
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.True(bounds.Contains(FixedVec2.FromInts(x, y)));
    }

    [Fact]
    public void Contains_RejectsXBelowMin()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.False(bounds.Contains(FixedVec2.FromInts(-1, 30)));
    }

    [Fact]
    public void Contains_RejectsYBelowMin()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.False(bounds.Contains(FixedVec2.FromInts(50, -1)));
    }

    [Fact]
    public void Contains_RejectsXAboveMax()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.False(bounds.Contains(FixedVec2.FromInts(101, 30)));
    }

    [Fact]
    public void Contains_RejectsYAboveMax()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.False(bounds.Contains(FixedVec2.FromInts(50, 61)));
    }

    [Fact]
    public void Contains_RejectsRawJustBelowZero_OnX()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.False(bounds.Contains(FixedVec2.FromRaw(-1, 30_000)));
    }

    [Fact]
    public void Contains_RejectsRawJustBelowZero_OnY()
    {
        ArenaBounds bounds = DefaultBounds();

        Assert.False(bounds.Contains(FixedVec2.FromRaw(50_000, -1)));
    }

    [Fact]
    public void Contains_RejectsRawJustAboveWidth()
    {
        ArenaBounds bounds = DefaultBounds();

        // Width.Raw = 100_000 (100 * Scale). 100_001 is exactly 1 raw unit beyond.
        Assert.False(bounds.Contains(FixedVec2.FromRaw(100_001, 30_000)));
    }

    [Fact]
    public void Contains_RejectsRawJustAboveHeight()
    {
        ArenaBounds bounds = DefaultBounds();

        // Height.Raw = 60_000 (60 * Scale). 60_001 is exactly 1 raw unit beyond.
        Assert.False(bounds.Contains(FixedVec2.FromRaw(50_000, 60_001)));
    }

    [Fact]
    public void Equals_ArenaBounds_AndObject_Work()
    {
        ArenaBounds a = new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60));
        ArenaBounds b = new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60));
        ArenaBounds differentWidth = new ArenaBounds(Fixed.FromInt(101), Fixed.FromInt(60));
        ArenaBounds differentHeight = new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(61));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(differentWidth));
        Assert.False(a.Equals(differentHeight));
        Assert.False(a.Equals("not an ArenaBounds"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        ArenaBounds a = new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60));
        ArenaBounds b = new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsWidthAndHeight_SmokeOnly()
    {
        ArenaBounds bounds = DefaultBounds();

        string text = bounds.ToString();

        // Width.Raw = 100_000, Height.Raw = 60_000 — Fixed.ToString prints raw.
        Assert.Contains("100", text);
        Assert.Contains("60", text);
    }
}
