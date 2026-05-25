using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchInitialStateTests
{
    private static TankState CreateTank(int id, int startIndex)
    {
        ArenaStartPosition start = ArenaCatalog.OpenTestArena.StartPositions[startIndex];
        return TankSpawnFactory.Create(
            new TankId(id),
            TankCatalog.BasicTank,
            start);
    }

    [Fact]
    public void Constructor_PreservesArenaReference()
    {
        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { CreateTank(0, 0) });

        Assert.Same(ArenaCatalog.OpenTestArena, match.Arena);
    }

    [Fact]
    public void Constructor_SetsStartTickToZero()
    {
        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { CreateTank(0, 0) });

        Assert.Equal(SimTick.Zero, match.StartTick);
    }

    [Fact]
    public void Constructor_PreservesTankCount()
    {
        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { CreateTank(0, 0), CreateTank(1, 1) });

        Assert.Equal(2, match.Tanks.Count);
    }

    [Fact]
    public void Constructor_PreservesTankOrder()
    {
        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { CreateTank(0, 0), CreateTank(1, 1) });

        Assert.Equal(new TankId(0), match.Tanks[0].Id);
        Assert.Equal(new TankId(1), match.Tanks[1].Id);
    }

    [Fact]
    public void Constructor_AcceptsOneTank()
    {
        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { CreateTank(0, 0) });

        Assert.Single(match.Tanks);
    }

    [Fact]
    public void Constructor_AcceptsTwoTanks()
    {
        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { CreateTank(0, 0), CreateTank(1, 1) });

        Assert.Equal(2, match.Tanks.Count);
    }

    [Fact]
    public void Constructor_RejectsNullArena()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchInitialState(
                arena: null!,
                tanks: new[] { CreateTank(0, 0) }));
    }

    [Fact]
    public void Constructor_RejectsNullTanks()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchInitialState(
                arena: ArenaCatalog.OpenTestArena,
                tanks: null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyTanks()
    {
        Assert.Throws<ArgumentException>(
            () => new MatchInitialState(
                arena: ArenaCatalog.OpenTestArena,
                tanks: Array.Empty<TankState>()));
    }

    [Fact]
    public void Constructor_DefensivelyCopiesTanks()
    {
        List<TankState> tanks = new List<TankState>
        {
            CreateTank(0, 0),
        };

        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            tanks);

        tanks.Add(CreateTank(1, 1));

        Assert.Single(match.Tanks);
    }

    [Fact]
    public void Constructor_AcceptsDuplicateTankId()
    {
        TankState a = CreateTank(7, 0);
        TankState b = CreateTank(7, 1);

        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { a, b });

        Assert.Equal(2, match.Tanks.Count);
        Assert.Equal(new TankId(7), match.Tanks[0].Id);
        Assert.Equal(new TankId(7), match.Tanks[1].Id);
    }

    [Fact]
    public void Constructor_AcceptsDuplicatePlayerSlot()
    {
        ArenaStartPosition start = ArenaCatalog.OpenTestArena.StartPositions[0];
        TankState a = TankSpawnFactory.Create(
            new TankId(0),
            TankCatalog.BasicTank,
            start);
        TankState b = TankSpawnFactory.Create(
            new TankId(1),
            TankCatalog.BasicTank,
            start);

        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { a, b });

        Assert.Equal(2, match.Tanks.Count);
        Assert.Equal(match.Tanks[0].OwnerSlot, match.Tanks[1].OwnerSlot);
    }

    [Fact]
    public void Constructor_AcceptsDestroyedTank()
    {
        TankState destroyed = CreateTank(0, 0).WithCurrentHitPoints(0);

        MatchInitialState match = new MatchInitialState(
            ArenaCatalog.OpenTestArena,
            new[] { destroyed });

        Assert.True(match.Tanks[0].IsDestroyed);
    }
}
