using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Arena;

public sealed class ArenaDefinitionTests
{
    private static ArenaBounds DefaultBounds()
        => new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60));

    private static List<ArenaStartPosition> DefaultStartPositions()
    {
        return new List<ArenaStartPosition>
        {
            new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
            new ArenaStartPosition(new PlayerSlot(1), FixedVec2.FromInts(90, 50)),
        };
    }

    private static List<WallBlock> DefaultWallBlocks()
    {
        return new List<WallBlock>
        {
            new WallBlock(
                "center_wall",
                FixedRect.FromMinSize(
                    FixedVec2.FromInts(40, 25),
                    Fixed.FromInt(20),
                    Fixed.FromInt(5))),
        };
    }

    private static List<FixedVec2> DefaultPatrolPoints()
    {
        return new List<FixedVec2>
        {
            FixedVec2.FromInts(20, 20),
            FixedVec2.FromInts(80, 40),
        };
    }

    private static List<string> DefaultTags()
    {
        return new List<string> { "test", "open" };
    }

    // ----- Preserve tests ------------------------------------------------

    [Fact]
    public void Constructor_Preserves_Id_DisplayName_Description_Bounds()
    {
        ArenaDefinition arena = new ArenaDefinition(
            "test_arena",
            "Test Arena",
            "A test arena description.",
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            DefaultTags());

        Assert.Equal("test_arena", arena.Id);
        Assert.Equal("Test Arena", arena.DisplayName);
        Assert.Equal("A test arena description.", arena.Description);
        Assert.Equal(DefaultBounds(), arena.Bounds);
    }

    [Fact]
    public void Constructor_Preserves_StartPositions_InOrder()
    {
        List<ArenaStartPosition> input = DefaultStartPositions();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            input,
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            DefaultTags());

        Assert.Equal(2, arena.StartPositions.Count);
        Assert.Equal(input[0], arena.StartPositions[0]);
        Assert.Equal(input[1], arena.StartPositions[1]);
    }

    [Fact]
    public void Constructor_Preserves_WallBlocks_InOrder()
    {
        List<WallBlock> input = DefaultWallBlocks();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            input,
            DefaultPatrolPoints(),
            DefaultTags());

        Assert.Single(arena.WallBlocks);
        Assert.Equal(input[0], arena.WallBlocks[0]);
    }

    [Fact]
    public void Constructor_Preserves_PatrolPoints_InOrder()
    {
        List<FixedVec2> input = DefaultPatrolPoints();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            input,
            DefaultTags());

        Assert.Equal(2, arena.PatrolPoints.Count);
        Assert.Equal(input[0], arena.PatrolPoints[0]);
        Assert.Equal(input[1], arena.PatrolPoints[1]);
    }

    [Fact]
    public void Constructor_Preserves_Tags_InOrder()
    {
        List<string> input = DefaultTags();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            input);

        Assert.Equal(2, arena.Tags.Count);
        Assert.Equal("test", arena.Tags[0]);
        Assert.Equal("open", arena.Tags[1]);
    }

    [Fact]
    public void Constructor_AcceptsEmptyDescription()
    {
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            DefaultTags());

        Assert.Equal(string.Empty, arena.Description);
    }

    // ----- String argument rejection -------------------------------------

    [Fact]
    public void Rejects_NullId()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                null!,
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_EmptyId()
    {
        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                string.Empty,
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_WhitespaceId()
    {
        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "   ",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_NullDisplayName()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                "id",
                null!,
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_EmptyDisplayName()
    {
        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                string.Empty,
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_WhitespaceDisplayName()
    {
        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                " \t ",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_NullDescription()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                "id",
                "name",
                null!,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    // ----- Null collection arguments -------------------------------------

    [Fact]
    public void Rejects_NullStartPositions()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                null!,
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_NullWallBlocks()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                null!,
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_NullPatrolPoints()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                null!,
                DefaultTags()));
    }

    [Fact]
    public void Rejects_NullTags()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                null!));
    }

    // ----- Content validation --------------------------------------------

    [Fact]
    public void Rejects_ZeroStartPositions()
    {
        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                Array.Empty<ArenaStartPosition>(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_DuplicatePlayerSlot()
    {
        List<ArenaStartPosition> dupSlots = new List<ArenaStartPosition>
        {
            new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
            new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(90, 50)),
        };

        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                dupSlots,
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_StartPositionOutsideBounds()
    {
        List<ArenaStartPosition> outside = new List<ArenaStartPosition>
        {
            new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(-1, 10)),
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                outside,
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_DuplicateWallBlockId()
    {
        WallBlock wall = DefaultWallBlocks()[0];
        List<WallBlock> dupIds = new List<WallBlock> { wall, wall };

        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                dupIds,
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_WallBlockMinOutsideBounds()
    {
        WallBlock badMin = new WallBlock(
            "bad_min",
            FixedRect.FromMinSize(
                FixedVec2.FromInts(-1, 10),
                Fixed.FromInt(5),
                Fixed.FromInt(5)));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                new List<WallBlock> { badMin },
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_WallBlockMaxOutsideBounds()
    {
        WallBlock outsideWall = new WallBlock(
            "outside_wall",
            FixedRect.FromMinSize(
                FixedVec2.FromInts(90, 50),
                Fixed.FromInt(20),
                Fixed.FromInt(5)));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                new List<WallBlock> { outsideWall },
                DefaultPatrolPoints(),
                DefaultTags()));
    }

    [Fact]
    public void Rejects_PatrolPointOutsideBounds()
    {
        List<FixedVec2> badPatrol = new List<FixedVec2>
        {
            FixedVec2.FromInts(20, 20),
            FixedVec2.FromInts(101, 10),
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                badPatrol,
                DefaultTags()));
    }

    [Fact]
    public void Rejects_NullTag()
    {
        List<string> badTags = new List<string> { "test", null! };

        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                badTags));
    }

    [Fact]
    public void Rejects_EmptyTag()
    {
        List<string> badTags = new List<string> { "test", string.Empty };

        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                badTags));
    }

    [Fact]
    public void Rejects_WhitespaceTag()
    {
        List<string> badTags = new List<string> { "test", "  " };

        Assert.Throws<ArgumentException>(
            () => new ArenaDefinition(
                "id",
                "name",
                string.Empty,
                DefaultBounds(),
                DefaultStartPositions(),
                DefaultWallBlocks(),
                DefaultPatrolPoints(),
                badTags));
    }

    // ----- Defensive copies ----------------------------------------------

    [Fact]
    public void DefensivelyCopies_StartPositions()
    {
        List<ArenaStartPosition> list = DefaultStartPositions();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            list,
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            DefaultTags());

        int countBefore = arena.StartPositions.Count;
        list.Add(new ArenaStartPosition(new PlayerSlot(2), FixedVec2.FromInts(50, 30)));

        Assert.Equal(countBefore, arena.StartPositions.Count);
    }

    [Fact]
    public void DefensivelyCopies_WallBlocks()
    {
        List<WallBlock> list = DefaultWallBlocks();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            list,
            DefaultPatrolPoints(),
            DefaultTags());

        int countBefore = arena.WallBlocks.Count;
        list.Add(
            new WallBlock(
                "extra",
                FixedRect.FromMinSize(
                    FixedVec2.FromInts(5, 5),
                    Fixed.FromInt(3),
                    Fixed.FromInt(3))));

        Assert.Equal(countBefore, arena.WallBlocks.Count);
    }

    [Fact]
    public void DefensivelyCopies_PatrolPoints()
    {
        List<FixedVec2> list = DefaultPatrolPoints();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            list,
            DefaultTags());

        int countBefore = arena.PatrolPoints.Count;
        list.Add(FixedVec2.FromInts(50, 30));

        Assert.Equal(countBefore, arena.PatrolPoints.Count);
    }

    [Fact]
    public void DefensivelyCopies_Tags()
    {
        List<string> list = DefaultTags();
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            list);

        int countBefore = arena.Tags.Count;
        list.Add("extra");

        Assert.Equal(countBefore, arena.Tags.Count);
    }

    [Fact]
    public void Constructor_AcceptsEmptyPatrolPoints()
    {
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            Array.Empty<FixedVec2>(),
            DefaultTags());

        Assert.Empty(arena.PatrolPoints);
    }

    [Fact]
    public void Constructor_AcceptsEmptyTags()
    {
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            DefaultWallBlocks(),
            DefaultPatrolPoints(),
            Array.Empty<string>());

        Assert.Empty(arena.Tags);
    }

    [Fact]
    public void Constructor_AcceptsEmptyWallBlocks()
    {
        ArenaDefinition arena = new ArenaDefinition(
            "id",
            "name",
            string.Empty,
            DefaultBounds(),
            DefaultStartPositions(),
            Array.Empty<WallBlock>(),
            DefaultPatrolPoints(),
            DefaultTags());

        Assert.Empty(arena.WallBlocks);
    }
}
