using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Arena;

public sealed class ArenaCatalogTests
{
    private static void AssertVec(int expectedX, int expectedY, FixedVec2 actual)
        => Assert.Equal(FixedVec2.FromInts(expectedX, expectedY), actual);

    private static void AssertWall(
        string id,
        int minX,
        int minY,
        int maxX,
        int maxY,
        WallBlock actual)
    {
        Assert.Equal(id, actual.Id);
        Assert.Equal(FixedVec2.FromInts(minX, minY), actual.Bounds.Min);
        Assert.Equal(FixedVec2.FromInts(maxX, maxY), actual.Bounds.Max);
    }

    // ----- OpenTestArena --------------------------------------------------

    [Fact]
    public void OpenTestArena_NotNull()
    {
        Assert.NotNull(ArenaCatalog.OpenTestArena);
    }

    [Fact]
    public void OpenTestArena_HasExpectedMetadata()
    {
        ArenaDefinition arena = ArenaCatalog.OpenTestArena;

        Assert.Equal("open_test_arena", arena.Id);
        Assert.Equal("Open Test Arena", arena.DisplayName);
        Assert.Equal(Fixed.FromInt(100), arena.Bounds.Width);
        Assert.Equal(Fixed.FromInt(60), arena.Bounds.Height);
    }

    [Fact]
    public void OpenTestArena_StartPositions_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.OpenTestArena;

        Assert.Equal(2, arena.StartPositions.Count);
        Assert.Equal(new PlayerSlot(0), arena.StartPositions[0].Slot);
        AssertVec(10, 30, arena.StartPositions[0].Position);
        Assert.Equal(new PlayerSlot(1), arena.StartPositions[1].Slot);
        AssertVec(90, 30, arena.StartPositions[1].Position);
    }

    [Fact]
    public void OpenTestArena_HasNoWallBlocks()
    {
        Assert.Empty(ArenaCatalog.OpenTestArena.WallBlocks);
    }

    [Fact]
    public void OpenTestArena_PatrolPoints_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.OpenTestArena;

        Assert.Equal(3, arena.PatrolPoints.Count);
        AssertVec(25, 30, arena.PatrolPoints[0]);
        AssertVec(50, 30, arena.PatrolPoints[1]);
        AssertVec(75, 30, arena.PatrolPoints[2]);
    }

    [Fact]
    public void OpenTestArena_Tags_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.OpenTestArena;

        Assert.Equal(2, arena.Tags.Count);
        Assert.Equal("test", arena.Tags[0]);
        Assert.Equal("open", arena.Tags[1]);
    }

    // ----- ObstacleTestArena ----------------------------------------------

    [Fact]
    public void ObstacleTestArena_NotNull()
    {
        Assert.NotNull(ArenaCatalog.ObstacleTestArena);
    }

    [Fact]
    public void ObstacleTestArena_HasExpectedMetadata()
    {
        ArenaDefinition arena = ArenaCatalog.ObstacleTestArena;

        Assert.Equal("obstacle_test_arena", arena.Id);
        Assert.Equal("Obstacle Test Arena", arena.DisplayName);
        Assert.Equal(Fixed.FromInt(100), arena.Bounds.Width);
        Assert.Equal(Fixed.FromInt(60), arena.Bounds.Height);
    }

    [Fact]
    public void ObstacleTestArena_StartPositions_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.ObstacleTestArena;

        Assert.Equal(2, arena.StartPositions.Count);
        Assert.Equal(new PlayerSlot(0), arena.StartPositions[0].Slot);
        AssertVec(10, 10, arena.StartPositions[0].Position);
        Assert.Equal(new PlayerSlot(1), arena.StartPositions[1].Slot);
        AssertVec(90, 50, arena.StartPositions[1].Position);
    }

    [Fact]
    public void ObstacleTestArena_WallBlocks_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.ObstacleTestArena;

        Assert.Equal(3, arena.WallBlocks.Count);
        AssertWall("center_wall", 45, 25, 55, 35, arena.WallBlocks[0]);
        AssertWall("north_block", 25, 45, 40, 50, arena.WallBlocks[1]);
        AssertWall("south_block", 60, 10, 75, 15, arena.WallBlocks[2]);
    }

    [Fact]
    public void ObstacleTestArena_PatrolPoints_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.ObstacleTestArena;

        Assert.Equal(3, arena.PatrolPoints.Count);
        AssertVec(20, 20, arena.PatrolPoints[0]);
        AssertVec(50, 15, arena.PatrolPoints[1]);
        AssertVec(80, 40, arena.PatrolPoints[2]);
    }

    [Fact]
    public void ObstacleTestArena_Tags_InOrder()
    {
        ArenaDefinition arena = ArenaCatalog.ObstacleTestArena;

        Assert.Equal(2, arena.Tags.Count);
        Assert.Equal("test", arena.Tags[0]);
        Assert.Equal("obstacle", arena.Tags[1]);
    }

    // ----- All ------------------------------------------------------------

    [Fact]
    public void All_HasTwoArenas_InDeterministicOrder()
    {
        Assert.Equal(2, ArenaCatalog.All.Count);
        Assert.Same(ArenaCatalog.OpenTestArena, ArenaCatalog.All[0]);
        Assert.Same(ArenaCatalog.ObstacleTestArena, ArenaCatalog.All[1]);
    }

    [Fact]
    public void All_IsNotCastableToMutableArray()
    {
        Assert.IsNotType<ArenaDefinition[]>(ArenaCatalog.All);
    }

    // ----- GetById --------------------------------------------------------

    [Fact]
    public void GetById_OpenTestArena()
    {
        Assert.Same(
            ArenaCatalog.OpenTestArena,
            ArenaCatalog.GetById("open_test_arena"));
    }

    [Fact]
    public void GetById_ObstacleTestArena()
    {
        Assert.Same(
            ArenaCatalog.ObstacleTestArena,
            ArenaCatalog.GetById("obstacle_test_arena"));
    }

    [Fact]
    public void GetById_RejectsNullId()
    {
        Assert.Throws<ArgumentNullException>(
            () => ArenaCatalog.GetById(null!));
    }

    [Fact]
    public void GetById_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(
            () => ArenaCatalog.GetById(string.Empty));
    }

    [Fact]
    public void GetById_RejectsWhitespaceId()
    {
        Assert.Throws<ArgumentException>(
            () => ArenaCatalog.GetById(" "));
    }

    [Fact]
    public void GetById_IsCaseSensitive()
    {
        Assert.Throws<KeyNotFoundException>(
            () => ArenaCatalog.GetById("OPEN_TEST_ARENA"));
    }

    [Fact]
    public void GetById_RejectsUnknownId()
    {
        Assert.Throws<KeyNotFoundException>(
            () => ArenaCatalog.GetById("unknown"));
    }
}
