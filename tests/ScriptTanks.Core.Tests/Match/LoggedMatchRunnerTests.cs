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
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class LoggedMatchRunnerTests
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
    public void RunUntilEnd_RejectsNullInitialState()
    {
        Assert.Throws<ArgumentNullException>(
            () => LoggedMatchRunner.RunUntilEnd(null!, maxTicks: 100));
    }

    [Fact]
    public void RunUntilEnd_RejectsNegativeMaxTicks()
    {
        MatchState state = CreateState(new SimTick(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => LoggedMatchRunner.RunUntilEnd(state, maxTicks: -1));
        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_WhenInitialStateAlreadyEnded_LogsStartedAndEndedAtInitialTick()
    {
        MatchState state = CreateState(new SimTick(0));

        LoggedMatchRunResult result = LoggedMatchRunner.RunUntilEnd(state, maxTicks: 0);

        Assert.Equal(0, result.RunResult.TicksExecuted);
        Assert.Same(state, result.RunResult.FinalState);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
        Assert.Equal(new SimTick(0), result.Log.Entries[0].Tick);
        Assert.Equal(new SimTick(0), result.Log.Entries[1].Tick);
    }

    [Fact]
    public void RunUntilEnd_AdvancesUntilTimeoutDraw_ReturnsRunResultAndLogs()
    {
        MatchState state = CreateState(new SimTick(0));

        LoggedMatchRunResult result = LoggedMatchRunner.RunUntilEnd(state, maxTicks: 3);

        Assert.Equal(3, result.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(3), result.RunResult.FinalState.CurrentTick);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(new SimTick(0), result.Log.Entries[0].Tick);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
        Assert.Equal(new SimTick(3), result.Log.Entries[1].Tick);
    }

    [Fact]
    public void RunUntilEnd_StopsWhenProjectileDestroysTank_LogsTankDestroyedEnd()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20), hp: 100);
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20), hp: 20);
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(87, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            ownerTankId: 0);
        MatchState state = CreateState(
            new SimTick(0),
            tanks: new[] { tank0, tank1 },
            projectiles: new[] { projectile });

        LoggedMatchRunResult result = LoggedMatchRunner.RunUntilEnd(state, maxTicks: 100);

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(MatchEndReason.TankDestroyed, result.RunResult.EndCondition.Reason);
        Assert.True(result.RunResult.EndCondition.WinnerTankId.HasValue);
        Assert.Equal(new TankId(0), result.RunResult.EndCondition.WinnerTankId!.Value);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
        Assert.Equal(
            "Match ended: tank 0 won by tank destruction.",
            result.Log.Entries[1].Message);
    }

    [Fact]
    public void RunUntilEnd_LogEntriesUseExpectedCategoriesAndEventTypes()
    {
        MatchState state = CreateState(new SimTick(0));

        LoggedMatchRunResult result = LoggedMatchRunner.RunUntilEnd(state, maxTicks: 3);

        Assert.Equal(CombatLogCategory.Match, result.Log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogCategory.Match, result.Log.Entries[1].Category);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
    }

    [Fact]
    public void RunUntilEnd_LogOrderIsNonDecreasing()
    {
        MatchState state = CreateState(new SimTick(0));

        LoggedMatchRunResult result = LoggedMatchRunner.RunUntilEnd(state, maxTicks: 3);

        Assert.True(
            result.Log.Entries[0].Tick.Value <= result.Log.Entries[1].Tick.Value);
    }

    [Fact]
    public void RunUntilEnd_FinalStateMatchesRunResultSemantics()
    {
        MatchState plainState = CreateState(new SimTick(0));
        MatchState loggedState = CreateState(new SimTick(0));

        MatchRunResult plain = MatchRunner.RunUntilEnd(plainState, maxTicks: 3);
        LoggedMatchRunResult logged = LoggedMatchRunner.RunUntilEnd(loggedState, maxTicks: 3);

        Assert.Equal(plain.FinalState.CurrentTick, logged.RunResult.FinalState.CurrentTick);
        Assert.Equal(plain.TicksExecuted, logged.RunResult.TicksExecuted);
        Assert.Equal(plain.EndCondition.Reason, logged.RunResult.EndCondition.Reason);
    }

    [Fact]
    public void RunUntilEnd_DoesNotMutateInitialState()
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

        LoggedMatchRunner.RunUntilEnd(state, maxTicks: 3);

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

    [Fact]
    public void LoggedRunResult_LogIsImmutable()
    {
        MatchState state = CreateState(new SimTick(0));
        LoggedMatchRunResult result = LoggedMatchRunner.RunUntilEnd(state, maxTicks: 3);

        IList<CombatLogEntry> entries = (IList<CombatLogEntry>)result.Log.Entries;

        Assert.True(entries.IsReadOnly);
        CombatLogEntry extra = new CombatLogEntry(
            new SimTick(0),
            CombatLogCategory.Match,
            "event",
            "message");
        Assert.Throws<NotSupportedException>(() => entries.Add(extra));
    }
}
