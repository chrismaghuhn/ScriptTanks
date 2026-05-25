using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Arena;
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

namespace ScriptTanks.Core.Tests.Replay;

public sealed class LoggedMatchReplayRecorderTests
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

    [Fact]
    public void RecordUntilEnd_RejectsNullInitialState()
    {
        Assert.Throws<ArgumentNullException>(
            () => LoggedMatchReplayRecorder.RecordUntilEnd(null!, maxTicks: 100));
    }

    [Fact]
    public void RecordUntilEnd_RejectsNegativeMaxTicks()
    {
        MatchState state = CreateState(new SimTick(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => LoggedMatchReplayRecorder.RecordUntilEnd(state, maxTicks: -1));
        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_WhenInitialStateAlreadyEnded_RecordsOnlyInitialFrameAndLogsLifecycle()
    {
        MatchState initialState = CreateState(new SimTick(0));

        LoggedMatchRecordedRunResult result = LoggedMatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 0);

        MatchRecordedRunResult recorded = result.RecordedRunResult;
        Assert.True(recorded.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, recorded.RunResult.EndCondition.Reason);
        Assert.Equal(0, recorded.RunResult.TicksExecuted);
        Assert.Single(recorded.Recording.Frames);
        Assert.Equal(0, recorded.Recording.Frames[0].FrameIndex);
        Assert.Same(initialState, recorded.Recording.Frames[0].State);
        Assert.Same(initialState, recorded.RunResult.FinalState);

        IReadOnlyList<CombatLogEntry> entries = result.Log.Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, entries[1].EventType);
        Assert.Equal(new SimTick(0), entries[0].Tick);
        Assert.Equal(new SimTick(0), entries[1].Tick);
    }

    [Fact]
    public void RecordUntilEnd_RecordsFrameForEachExecutedTickUntilTimeoutAndLogsLifecycle()
    {
        MatchState initialState = CreateState(new SimTick(0));

        LoggedMatchRecordedRunResult result = LoggedMatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 3);

        MatchRecordedRunResult recorded = result.RecordedRunResult;
        Assert.True(recorded.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, recorded.RunResult.EndCondition.Reason);
        Assert.Equal(3, recorded.RunResult.TicksExecuted);
        Assert.Equal(4, recorded.Recording.Frames.Count);

        for (int i = 0; i <= 3; i++)
        {
            Assert.Equal(i, recorded.Recording.Frames[i].FrameIndex);
            Assert.Equal(new SimTick(i), recorded.Recording.Frames[i].State.CurrentTick);
        }

        Assert.Same(
            recorded.RunResult.FinalState,
            recorded.Recording.Frames[^1].State);

        IReadOnlyList<CombatLogEntry> entries = result.Log.Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, entries[0].EventType);
        Assert.Equal(new SimTick(0), entries[0].Tick);
        Assert.Equal(CombatLogEventTypes.MatchEnded, entries[1].EventType);
        Assert.Equal(new SimTick(3), entries[1].Tick);
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

        LoggedMatchRecordedRunResult result = LoggedMatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 2);

        MatchReplayRecording recording = result.RecordedRunResult.Recording;
        Assert.Equal(3, recording.Frames.Count);

        Assert.Single(recording.Frames[0].State.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(50, 50),
            recording.Frames[0].State.Projectiles[0].Position);

        Assert.Single(recording.Frames[1].State.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(51, 50),
            recording.Frames[1].State.Projectiles[0].Position);

        Assert.Single(recording.Frames[2].State.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(52, 50),
            recording.Frames[2].State.Projectiles[0].Position);
    }

    [Fact]
    public void RecordUntilEnd_FinalStateIsLastRecordedFrame()
    {
        MatchState initialState = CreateState(new SimTick(0));

        LoggedMatchRecordedRunResult result = LoggedMatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 3);

        Assert.Same(
            result.RecordedRunResult.RunResult.FinalState,
            result.RecordedRunResult.Recording.Frames[^1].State);
    }

    [Fact]
    public void RecordUntilEnd_LogEntriesUseExpectedCategoriesAndEventTypes()
    {
        MatchState initialState = CreateState(new SimTick(0));

        LoggedMatchRecordedRunResult result = LoggedMatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 3);

        IReadOnlyList<CombatLogEntry> entries = result.Log.Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(CombatLogCategory.Match, entries[0].Category);
        Assert.Equal(CombatLogEventTypes.MatchStarted, entries[0].EventType);
        Assert.Equal(CombatLogCategory.Match, entries[1].Category);
        Assert.Equal(CombatLogEventTypes.MatchEnded, entries[1].EventType);
    }

    [Fact]
    public void RecordUntilEnd_LogOrderIsNonDecreasing()
    {
        MatchState initialState = CreateState(new SimTick(0));

        LoggedMatchRecordedRunResult result = LoggedMatchReplayRecorder.RecordUntilEnd(
            initialState,
            maxTicks: 3);

        IReadOnlyList<CombatLogEntry> entries = result.Log.Entries;
        for (int i = 1; i < entries.Count; i++)
        {
            Assert.True(entries[i - 1].Tick.Value <= entries[i].Tick.Value);
        }
    }

    [Fact]
    public void RecordUntilEnd_MatchesPlainRecorderSemantics()
    {
        MatchState plainState = CreateState(new SimTick(0));
        MatchState loggedState = CreateState(new SimTick(0));

        MatchRecordedRunResult plain = MatchReplayRecorder.RecordUntilEnd(
            plainState,
            maxTicks: 3);
        LoggedMatchRecordedRunResult logged = LoggedMatchReplayRecorder.RecordUntilEnd(
            loggedState,
            maxTicks: 3);

        Assert.Equal(
            plain.RunResult.TicksExecuted,
            logged.RecordedRunResult.RunResult.TicksExecuted);
        Assert.Equal(
            plain.RunResult.EndCondition.Reason,
            logged.RecordedRunResult.RunResult.EndCondition.Reason);
        Assert.Equal(
            plain.RunResult.FinalState.CurrentTick,
            logged.RecordedRunResult.RunResult.FinalState.CurrentTick);
        Assert.Equal(
            plain.Recording.Frames.Count,
            logged.RecordedRunResult.Recording.Frames.Count);
    }

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

        LoggedMatchReplayRecorder.RecordUntilEnd(state, maxTicks: 3);

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
