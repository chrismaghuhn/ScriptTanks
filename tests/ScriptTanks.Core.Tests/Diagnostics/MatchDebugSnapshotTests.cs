using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Diagnostics;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Replay;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Diagnostics;

public sealed class MatchDebugSnapshotTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(
                FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });
    }

    private static MatchState CreateState()
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(0),
            new[]
            {
                CreateTank(0, 0),
                CreateTank(1, 1),
            },
            new[]
            {
                CreateLoadout(),
                CreateLoadout(),
            },
            Array.Empty<ProjectileState>());
    }

    private static CombatLog CreateLog()
    {
        return new CombatLogBuilder()
            .Add(
                new SimTick(0),
                CombatLogCategory.Match,
                CombatLogEventTypes.MatchStarted,
                "Match started.")
            .Build();
    }

    private static MatchReplayFrame CreateReplayFrame()
    {
        return new MatchReplayFrame(0, CreateState());
    }

    [Fact]
    public void Constructor_PreservesStateLogAndReplayFrameReferences()
    {
        MatchState state = CreateState();
        CombatLog log = CreateLog();
        MatchReplayFrame frame = new MatchReplayFrame(7, state);

        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(state, log, frame);

        Assert.Same(state, snapshot.State);
        Assert.Same(log, snapshot.Log);
        Assert.Same(frame, snapshot.ReplayFrame);
    }

    [Fact]
    public void Constructor_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchDebugSnapshot(null!));
    }

    [Fact]
    public void Constructor_AllowsNullLogAndNullReplayFrame()
    {
        MatchState state = CreateState();

        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(state);

        Assert.Same(state, snapshot.State);
        Assert.Null(snapshot.Log);
        Assert.Null(snapshot.ReplayFrame);
    }

    [Fact]
    public void HasReplayFrame_ReturnsFalse_WhenReplayFrameIsNull()
    {
        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(CreateState());

        Assert.False(snapshot.HasReplayFrame);
    }

    [Fact]
    public void HasReplayFrame_ReturnsTrue_WhenReplayFrameExists()
    {
        MatchState state = CreateState();
        MatchReplayFrame frame = new MatchReplayFrame(0, state);

        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(
            state,
            replayFrame: frame);

        Assert.True(snapshot.HasReplayFrame);
    }

    [Fact]
    public void FromState_CreatesSnapshotWithoutLogOrReplayFrame()
    {
        MatchState state = CreateState();

        MatchDebugSnapshot snapshot = MatchDebugSnapshot.FromState(state);

        Assert.Same(state, snapshot.State);
        Assert.Null(snapshot.Log);
        Assert.Null(snapshot.ReplayFrame);
        Assert.False(snapshot.HasReplayFrame);
    }

    [Fact]
    public void FromStateAndLog_CreatesSnapshotWithLogAndRejectsNullLog()
    {
        MatchState state = CreateState();
        CombatLog log = CreateLog();

        MatchDebugSnapshot snapshot = MatchDebugSnapshot.FromStateAndLog(state, log);

        Assert.Same(state, snapshot.State);
        Assert.Same(log, snapshot.Log);
        Assert.Null(snapshot.ReplayFrame);

        Assert.Throws<ArgumentNullException>(
            () => MatchDebugSnapshot.FromStateAndLog(state, null!));
    }

    [Fact]
    public void FromReplayFrame_UsesFrameStateAndStoresFrame()
    {
        MatchReplayFrame frame = CreateReplayFrame();

        MatchDebugSnapshot snapshot = MatchDebugSnapshot.FromReplayFrame(frame);

        Assert.Same(frame.State, snapshot.State);
        Assert.Same(frame, snapshot.ReplayFrame);
        Assert.Null(snapshot.Log);
        Assert.True(snapshot.HasReplayFrame);
    }

    [Fact]
    public void FromReplayFrame_AllowsOptionalLog()
    {
        MatchReplayFrame frame = CreateReplayFrame();
        CombatLog log = CreateLog();

        MatchDebugSnapshot snapshot = MatchDebugSnapshot.FromReplayFrame(frame, log);

        Assert.Same(frame.State, snapshot.State);
        Assert.Same(frame, snapshot.ReplayFrame);
        Assert.Same(log, snapshot.Log);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchState state = CreateState();
        CombatLog log = CreateLog();

        MatchDebugSnapshot a = new MatchDebugSnapshot(state, log);
        MatchDebugSnapshot b = new MatchDebugSnapshot(state, log);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
