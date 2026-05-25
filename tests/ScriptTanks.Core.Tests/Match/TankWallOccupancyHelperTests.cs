using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class TankWallOccupancyHelperTests
{
    private static FixedVec2 Vec(int x, int y)
        => FixedVec2.FromInts(x, y);

    private static Fixed Radius(int value)
        => Fixed.FromInt(value);

    private static WallBlock CreateWall(string id, FixedRect bounds)
        => new WallBlock(id, bounds);

    private static FixedRect UnitWallAtTenByTen()
        => FixedRect.FromMinSize(Vec(10, 10), Fixed.FromInt(10), Fixed.FromInt(10));

    private static ArenaDefinition CreateArena(params WallBlock[] wallBlocks)
    {
        return new ArenaDefinition(
            id: "wall_occupancy_test_arena",
            displayName: "Wall Occupancy Test Arena",
            description: "Arena for tank wall occupancy helper tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), Vec(10, 10)),
            },
            wallBlocks: wallBlocks,
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    // -------------------- Validation --------------------

    [Fact]
    public void FindFirstBlockingWallBlockId_NullArena_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
                null!,
                Vec(10, 10),
                Radius(2)));

        Assert.Equal("arena", ex.ParamName);
    }

    [Fact]
    public void IsBlocked_NullArena_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            TankWallOccupancyHelper.IsBlocked(null!, Vec(10, 10), Radius(2)));

        Assert.Equal("arena", ex.ParamName);
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_ZeroRadius_Throws()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
                arena,
                Vec(15, 15),
                Fixed.Zero));

        Assert.Equal("hitboxRadius", ex.ParamName);
    }

    [Fact]
    public void IsBlocked_ZeroRadius_Throws()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TankWallOccupancyHelper.IsBlocked(arena, Vec(15, 15), Fixed.Zero));

        Assert.Equal("hitboxRadius", ex.ParamName);
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_NegativeRadius_Throws()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
                arena,
                Vec(15, 15),
                Fixed.FromRaw(-1)));

        Assert.Equal("hitboxRadius", ex.ParamName);
    }

    [Fact]
    public void IsBlocked_NegativeRadius_Throws()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TankWallOccupancyHelper.IsBlocked(arena, Vec(15, 15), Fixed.FromRaw(-1)));

        Assert.Equal("hitboxRadius", ex.ParamName);
    }

    // -------------------- No walls / no overlap --------------------

    [Fact]
    public void FindFirstBlockingWallBlockId_NoWalls_ReturnsNull()
    {
        ArenaDefinition arena = CreateArena();

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(15, 15),
            Radius(2));

        Assert.Null(result);
    }

    [Fact]
    public void IsBlocked_NoWalls_ReturnsFalse()
    {
        ArenaDefinition arena = CreateArena();

        Assert.False(TankWallOccupancyHelper.IsBlocked(arena, Vec(15, 15), Radius(2)));
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_WallFarAway_ReturnsNull()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(50, 50),
            Radius(2));

        Assert.Null(result);
    }

    [Fact]
    public void IsBlocked_WallFarAway_ReturnsFalse()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        Assert.False(TankWallOccupancyHelper.IsBlocked(arena, Vec(50, 50), Radius(2)));
    }

    // -------------------- Collision behavior --------------------

    [Fact]
    public void FindFirstBlockingWallBlockId_OverlappingWall_ReturnsWallId()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(15, 15),
            Radius(2));

        Assert.Equal("wall_a", result);
    }

    [Fact]
    public void IsBlocked_OverlappingWall_ReturnsTrue()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        Assert.True(TankWallOccupancyHelper.IsBlocked(arena, Vec(15, 15), Radius(2)));
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_TouchingWallEdge_ReturnsWallId()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(8, 15),
            Radius(2));

        Assert.Equal("wall_a", result);
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_TouchingWallCorner_ReturnsWallId()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(8, 8),
            Radius(3));

        Assert.Equal("wall_a", result);
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_JustOutsideCorner_ReturnsNull()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(7, 7),
            Radius(2));

        Assert.Null(result);
    }

    // -------------------- Deterministic ordering --------------------

    [Fact]
    public void FindFirstBlockingWallBlockId_MultipleBlockingWalls_ReturnsFirstWallIdInArenaOrder()
    {
        FixedRect bounds = UnitWallAtTenByTen();
        ArenaDefinition arena = CreateArena(
            CreateWall("first_wall", bounds),
            CreateWall("second_wall", bounds));

        string? result = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            Vec(15, 15),
            Radius(2));

        Assert.Equal("first_wall", result);
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_ReorderedWalls_ReturnsNewFirstWallId()
    {
        FixedRect bounds = UnitWallAtTenByTen();
        ArenaDefinition firstOrder = CreateArena(
            CreateWall("first_wall", bounds),
            CreateWall("second_wall", bounds));
        ArenaDefinition swappedOrder = CreateArena(
            CreateWall("second_wall", bounds),
            CreateWall("first_wall", bounds));

        string? firstResult = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            firstOrder,
            Vec(15, 15),
            Radius(2));
        string? swappedResult = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            swappedOrder,
            Vec(15, 15),
            Radius(2));

        Assert.Equal("first_wall", firstResult);
        Assert.Equal("second_wall", swappedResult);
    }

    // -------------------- Delegation / stability --------------------

    [Fact]
    public void IsBlocked_DelegatesSemanticallyToFindFirstBlockingWallBlockId()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        FixedVec2 center = Vec(15, 15);
        Fixed radius = Radius(2);

        string? blockingId = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
            arena,
            center,
            radius);
        bool isBlocked = TankWallOccupancyHelper.IsBlocked(arena, center, radius);

        Assert.Equal(blockingId is not null, isBlocked);
    }

    [Fact]
    public void FindFirstBlockingWallBlockId_RepeatedCalls_ReturnsSameId()
    {
        ArenaDefinition arena = CreateArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        FixedVec2 center = Vec(15, 15);
        Fixed radius = Radius(2);

        string? first = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(arena, center, radius);
        string? second = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(arena, center, radius);

        Assert.Equal(first, second);
        Assert.Single(arena.WallBlocks);
    }
}
