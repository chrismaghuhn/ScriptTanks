using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Replay;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Replay;

public sealed class MatchReplayFrameTests
{
    private static MatchState CreateState()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        TankWeaponLoadout loadout0 = CreateLoadout();
        TankWeaponLoadout loadout1 = CreateLoadout();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(0),
            new[] { tank0, tank1 },
            new[] { loadout0, loadout1 },
            Array.Empty<ProjectileState>());
    }

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position)
    {
        MovementState movement = new MovementState(position, FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    [Fact]
    public void Constructor_PreservesValues()
    {
        MatchState state = CreateState();

        MatchReplayFrame frame = new MatchReplayFrame(7, state);

        Assert.Equal(7, frame.FrameIndex);
        Assert.Same(state, frame.State);
    }

    [Fact]
    public void Constructor_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchReplayFrame(0, null!));
    }

    [Fact]
    public void Constructor_RejectsNegativeFrameIndex()
    {
        MatchState state = CreateState();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchReplayFrame(-1, state));
        Assert.Equal("frameIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsFrameIndexZero()
    {
        MatchState state = CreateState();

        MatchReplayFrame frame = new MatchReplayFrame(0, state);

        Assert.Equal(0, frame.FrameIndex);
        Assert.Same(state, frame.State);
    }

    [Fact]
    public void Constructor_PreservesStateReference()
    {
        MatchState state = CreateState();

        MatchReplayFrame frame = new MatchReplayFrame(3, state);

        Assert.Same(state, frame.State);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchState state = CreateState();
        MatchReplayFrame a = new MatchReplayFrame(5, state);
        MatchReplayFrame b = new MatchReplayFrame(5, state);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
