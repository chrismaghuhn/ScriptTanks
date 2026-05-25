using System;
using System.Linq;
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

public sealed class MatchReplayRecorderTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null)
    {
        MovementState movement = new MovementState(
            position ?? FixedVec2.FromInts(10 + id, 20),
            FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileDefinition CreateProjectileDefinition(
        int rawDamage = 25,
        Fixed? speedPerTick = null,
        Fixed? maxRange = null,
        Fixed? radius = null)
        => new ProjectileDefinition(
            rawDamage,
            speedPerTick ?? Fixed.FromInt(1),
            maxRange ?? Fixed.FromInt(40),
            radius ?? Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(
        int id,
        FixedVec2? position = null,
        FixedVec2? velocityPerTick = null,
        Fixed? remainingRange = null,
        bool isActive = true,
        ProjectileDefinition? definition = null,
        int ownerTankId = 99)
    {
        ProjectileDefinition projectileDefinition =
            definition ?? CreateProjectileDefinition();

        return new ProjectileState(
            new ProjectileId(id),
            projectileDefinition,
            new TankId(ownerTankId),
            new WeaponSlot(0),
            new SimTick(0),
            position ?? FixedVec2.FromInts(50, 50),
            velocityPerTick ?? FixedVec2.FromInts(1, 0),
            remainingRange ?? projectileDefinition.MaxRange,
            isActive);
    }

    private static MatchState CreateState(
        SimTick tick,
        TankState[]? tanks = null,
        ProjectileState[]? projectiles = null)
    {
        TankState[] actualTanks = tanks ?? new[]
        {
            CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
        };
        TankWeaponLoadout[] loadouts = actualTanks
            .Select(_ => CreateLoadout())
            .ToArray();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            actualTanks,
            loadouts,
            projectiles ?? Array.Empty<ProjectileState>());
    }

    // -------------------- Validation --------------------

    [Fact]
    public void MatchRecordedRunResult_RejectsNullRunResult()
    {
        MatchReplayRecording recording = new MatchReplayRecording(new[]
        {
            new MatchReplayFrame(0, CreateState(new SimTick(0))),
        });

        Assert.Throws<ArgumentNullException>(
            () => new MatchRecordedRunResult(null!, recording));
    }

    [Fact]
    public void MatchRecordedRunResult_RejectsNullRecording()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchRunResult runResult = new MatchRunResult(
            state,
            MatchEndConditionResult.Running(),
            0);

        Assert.Throws<ArgumentNullException>(
            () => new MatchRecordedRunResult(runResult, null!));
    }

    [Fact]
    public void RecordUntilEnd_RejectsNullInitialState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchReplayRecorder.RecordUntilEnd(null!, maxTicks: 100));
    }

    [Fact]
    public void RecordUntilEnd_RejectsNegativeMaxTicks()
    {
        MatchState state = CreateState(new SimTick(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchReplayRecorder.RecordUntilEnd(state, maxTicks: -1));
        Assert.Equal("maxTicks", ex.ParamName);
    }

    // -------------------- Initial-state termination --------------------

    [Fact]
    public void RecordUntilEnd_WhenInitialStateAlreadyEnded_RecordsOnlyInitialFrame()
    {
        MatchState initialState = CreateState(new SimTick(0));

        MatchRecordedRunResult result = MatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 0);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(0, result.RunResult.TicksExecuted);
        Assert.Single(result.Recording.Frames);
        Assert.Equal(0, result.Recording.Frames[0].FrameIndex);
        Assert.Same(initialState, result.Recording.Frames[0].State);
        Assert.Same(initialState, result.RunResult.FinalState);
    }

    // -------------------- Loop behavior --------------------

    [Fact]
    public void RecordUntilEnd_RecordsFrameForEachExecutedTickUntilTimeout()
    {
        MatchState initialState = CreateState(new SimTick(0));

        MatchRecordedRunResult result = MatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 3);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(3, result.RunResult.TicksExecuted);
        Assert.Equal(4, result.Recording.Frames.Count);

        for (int i = 0; i <= 3; i++)
        {
            Assert.Equal(i, result.Recording.Frames[i].FrameIndex);
            Assert.Equal(new SimTick(i), result.Recording.Frames[i].State.CurrentTick);
        }

        Assert.Same(
            result.RunResult.FinalState,
            result.Recording.Frames[^1].State);
    }

    [Fact]
    public void RecordUntilEnd_RecordsProjectileStateChanges()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        MatchState initialState = CreateState(
            new SimTick(0),
            projectiles: new[] { projectile });

        MatchRecordedRunResult result = MatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 2);

        Assert.Equal(3, result.Recording.Frames.Count);

        Assert.Single(result.Recording.Frames[0].State.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(50, 50),
            result.Recording.Frames[0].State.Projectiles[0].Position);

        Assert.Single(result.Recording.Frames[1].State.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(51, 50),
            result.Recording.Frames[1].State.Projectiles[0].Position);

        Assert.Single(result.Recording.Frames[2].State.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(52, 50),
            result.Recording.Frames[2].State.Projectiles[0].Position);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void RecordUntilEnd_DoesNotMutateInitialState()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        MatchState state = CreateState(
            new SimTick(0),
            tanks: new[] { tank0, tank1 },
            projectiles: new[] { projectile });

        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        ProjectileState[] originalProjectiles = state.Projectiles.ToArray();
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];
        int originalProjectileCount = state.Projectiles.Count;

        MatchReplayRecorder.RecordUntilEnd(state, maxTicks: 3);

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTanks.Length, state.Tanks.Count);
        for (int i = 0; i < originalTanks.Length; i++)
        {
            Assert.Equal(originalTanks[i], state.Tanks[i]);
        }
        Assert.Equal(originalProjectileCount, state.Projectiles.Count);
        for (int i = 0; i < originalProjectiles.Length; i++)
        {
            Assert.Equal(originalProjectiles[i], state.Projectiles[i]);
        }
        Assert.Same(originalLoadout0, state.Loadouts[0]);
        Assert.Same(originalLoadout1, state.Loadouts[1]);
    }
}
