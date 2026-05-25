using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Arena;

public sealed class WallBlockTests
{
    private static FixedRect DefaultBounds()
        => FixedRect.FromMinSize(
            FixedVec2.FromInts(40, 25),
            Fixed.FromInt(20),
            Fixed.FromInt(5));

    [Fact]
    public void Constructor_PreservesId()
    {
        WallBlock wall = new WallBlock("center_wall", DefaultBounds());

        Assert.Equal("center_wall", wall.Id);
    }

    [Fact]
    public void Constructor_PreservesBounds()
    {
        FixedRect bounds = DefaultBounds();

        WallBlock wall = new WallBlock("center_wall", bounds);

        Assert.Equal(bounds, wall.Bounds);
    }

    [Fact]
    public void Constructor_RejectsNullId()
    {
        Assert.Throws<ArgumentNullException>(
            () => new WallBlock(null!, DefaultBounds()));
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(
            () => new WallBlock(string.Empty, DefaultBounds()));
    }

    [Fact]
    public void Constructor_RejectsWhitespaceId()
    {
        Assert.Throws<ArgumentException>(
            () => new WallBlock("   ", DefaultBounds()));
    }

    [Fact]
    public void Equals_WallBlock_AndObject_Work()
    {
        FixedRect bounds = DefaultBounds();
        WallBlock a = new WallBlock("center_wall", bounds);
        WallBlock b = new WallBlock("center_wall", bounds);
        WallBlock differentId = new WallBlock("other_wall", bounds);
        WallBlock differentBounds = new WallBlock(
            "center_wall",
            FixedRect.FromMinSize(FixedVec2.FromInts(0, 0), Fixed.FromInt(5), Fixed.FromInt(5)));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(differentId));
        Assert.False(a.Equals(differentBounds));
        Assert.False(a.Equals("not a WallBlock"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        FixedRect bounds = DefaultBounds();
        WallBlock a = new WallBlock("center_wall", bounds);
        WallBlock b = new WallBlock("center_wall", bounds);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsId_SmokeOnly()
    {
        WallBlock wall = new WallBlock("center_wall", DefaultBounds());

        string text = wall.ToString();

        Assert.Contains("center_wall", text);
    }
}
