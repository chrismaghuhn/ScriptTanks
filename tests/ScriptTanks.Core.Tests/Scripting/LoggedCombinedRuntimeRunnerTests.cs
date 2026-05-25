using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class LoggedCombinedRuntimeRunnerTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankWeaponLoadout CreateWeaponLoadout()
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });
    }

    private static MatchState CreateMatchState(
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            weaponLoadouts,
            Array.Empty<ProjectileState>());
    }

    private static TankSensorLoadout CreateSensorLoadout(
        SensorState first,
        SensorState? second = null)
    {
        return second.HasValue
            ? new TankSensorLoadout(new[] { first, second.Value })
            : new TankSensorLoadout(new[] { first });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    private static ScriptProgram ScanWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "scan",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.ScanEnemy, "default")),
            });
    }

    private static ScriptProgram FireWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Fire, argument: string.Empty)),
            });
    }

    private static ScriptProgram MoveToPatrolPointWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "patrol",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, "next")),
            });
    }

    private static FixedVec2 ExpectedPatrolVelocity()
    {
        Fixed maxSpeed = TankCatalog.BasicTank.Stats.MaxVelocityPerTick;
        return FixedVec2.FromInts(1, 0) * maxSpeed;
    }

    private static ScriptProgram NoOpWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "noop",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.NoOp, string.Empty)),
            });
    }

    private static TankState CreateTankWithMovement(
        int id,
        int ownerSlot,
        FixedVec2 position,
        FixedVec2 velocityPerTick)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position, velocityPerTick),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static MatchSensorRuntimeState CreateRuntimeAtRightEdgeWithEastVelocity(SimTick tick)
    {
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        MatchState state = CreateMatchState(
            tick,
            new[] { CreateWeaponLoadout() },
            CreateTankWithMovement(
                0,
                0,
                new FixedVec2(maxX, Fixed.FromInt(20)),
                FixedVec2.FromInts(1, 0)));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static ProjectileState CreateSeededProjectile(
        FixedVec2 position,
        SimTick spawnTick,
        int ownerTankId = 1)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        return new ProjectileState(
            new ProjectileId(0),
            definition,
            new TankId(ownerTankId),
            new WeaponSlot(0),
            spawnTick,
            position,
            FixedVec2.Zero,
            definition.MaxRange,
            isActive: true);
    }

    private static MatchSensorRuntimeState CreateBoundsProjectileHitInitialRuntime(SimTick tick)
    {
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        MatchState state = CreateMatchState(
            tick,
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            CreateTankWithMovement(
                0,
                0,
                new FixedVec2(maxX, Fixed.FromInt(20)),
                FixedVec2.FromInts(1, 0)),
            new TankState(
                new TankId(1),
                new PlayerSlot(1),
                TankCatalog.BasicTank,
                new MovementState(FixedVec2.FromInts(90, 20), FixedVec2.Zero),
                TankCatalog.BasicTank.Stats.MaxHitPoints,
                Fixed.Zero,
                Fixed.Zero));

        SimTick spawnTick = tick.Value > 0
            ? new SimTick(tick.Value - 1)
            : new SimTick(0);
        ProjectileState[] projectiles =
        {
            CreateSeededProjectile(
                FixedVec2.FromInts(100, 20),
                spawnTick,
                ownerTankId: 1),
        };

        return CreateRuntime(
            new MatchState(
                state.Arena,
                state.CurrentTick,
                state.Tanks,
                state.Loadouts,
                projectiles),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[] { CreateWeaponLoadout() },
            CreateTank(0, 0));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static void AssertMvpMovementLogEvents(CombatLog log)
    {
        Assert.Equal(2, log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, log.Entries[1].EventType);
        Assert.DoesNotContain(
            log.Entries,
            entry => entry.EventType == CombatLogEventTypes.ScriptTick);
        Assert.DoesNotContain(
            log.Entries,
            entry => entry.EventType == CombatLogEventTypes.FireRequestApplied);
        Assert.DoesNotContain(
            log.Entries,
            entry => entry.EventType == CombatLogEventTypes.FireRejected);
    }

    private static void AssertRichMovementLogSequence(CombatLog log, int ticksExecuted)
    {
        Assert.Equal(2 + ticksExecuted, log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, log.Entries[0].EventType);

        for (int i = 0; i < ticksExecuted; i++)
        {
            Assert.Equal(CombatLogEventTypes.ScriptTick, log.Entries[i + 1].EventType);
        }

        Assert.Equal(CombatLogEventTypes.MatchEnded, log.Entries[^1].EventType);
        Assert.All(
            log.Entries,
            entry =>
            {
                Assert.True(
                    entry.EventType == CombatLogEventTypes.MatchStarted
                    || entry.EventType == CombatLogEventTypes.ScriptTick
                    || entry.EventType == CombatLogEventTypes.MatchEnded);
            });
        Assert.DoesNotContain(
            log.Entries,
            entry => entry.EventType == CombatLogEventTypes.FireRequestApplied);
        Assert.DoesNotContain(
            log.Entries,
            entry => entry.EventType == CombatLogEventTypes.FireRejected);
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(),
                CreateWeaponLoadout(),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static List<ScriptProgram> TwoTankScanPrograms()
    {
        return new List<ScriptProgram>
        {
            ScanWhenAlways(),
            ScanWhenAlways(),
        };
    }

    private static TankWeaponLoadout CreateFiredLoadout(SimTick lastFireTick)
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon).MarkFired(lastFireTick),
        });
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntimeFirstFired(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateFiredLoadout(tick),
                CreateWeaponLoadout(),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    #region Validation

    [Fact]
    public void RunUntilEnd_NullInitialRuntime_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            LoggedCombinedRuntimeRunner.RunUntilEnd(
                null!,
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_NegativeMaxTicks_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoggedCombinedRuntimeRunner.RunUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: -1));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_NullPrograms_WithRunningFixture_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            LoggedCombinedRuntimeRunner.RunUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                null!,
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_MaxTicksZero_InvalidPrograms_DoesNotThrow()
    {
        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            CreateTwoTankRuntime(new SimTick(0)),
            null!,
            new ProjectileIdSequence(0),
            maxTicks: 0);

        Assert.Equal(0, result.RunResult.RunResult.TicksExecuted);
        Assert.Equal(2, result.Log.Entries.Count);
    }

    #endregion

    #region Zero-tick / multi-tick

    [Fact]
    public void RunUntilEnd_MaxTicksZero_ReturnsLoggedResultWithoutTicking()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 0);

        Assert.Equal(0, result.RunResult.RunResult.TicksExecuted);
        Assert.False(result.RunResult.HasLastTickResult);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
        Assert.Equal(new SimTick(0), result.Log.Entries[0].Tick);
        Assert.Equal(new SimTick(0), result.Log.Entries[1].Tick);
    }

    [Fact]
    public void RunUntilEnd_ScanOnly_LogsStartAndEnd()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(2, result.RunResult.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(2), result.RunResult.FinalRuntime.State.CurrentTick);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(new SimTick(0), result.Log.Entries[0].Tick);
        Assert.Equal(new SimTick(2), result.Log.Entries[1].Tick);
    }

    [Fact]
    public void RunUntilEnd_FireProgram_DelegatesToCombinedRuntimeRunner()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(7);

        CombinedRuntimeRunResult plain = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 1);

        LoggedCombinedRuntimeRunResult logged = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 1);

        Assert.Equal(
            plain.FinalRuntime.State.CurrentTick,
            logged.RunResult.FinalRuntime.State.CurrentTick);
        Assert.Equal(plain.FinalProjectileIdSequence, logged.RunResult.FinalProjectileIdSequence);
        Assert.Equal(plain.RunResult.EndCondition.Reason, logged.RunResult.RunResult.EndCondition.Reason);
        Assert.Equal(plain.RunResult.TicksExecuted, logged.RunResult.RunResult.TicksExecuted);
    }

    #endregion

    #region Log content / determinism / purity

    [Fact]
    public void RunUntilEnd_LogContainsOnlyMatchStartedAndMatchEnded()
    {
        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            CreateTwoTankRuntime(new SimTick(0)),
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(2, result.Log.Entries.Count);
        Assert.All(
            result.Log.Entries,
            entry =>
            {
                Assert.True(
                    entry.EventType == CombatLogEventTypes.MatchStarted
                    || entry.EventType == CombatLogEventTypes.MatchEnded);
            });
    }

    [Fact]
    public void RunUntilEnd_LogEntriesAreInNonDecreasingTickOrder()
    {
        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            CreateTwoTankRuntime(new SimTick(0)),
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.True(
            result.Log.Entries[0].Tick.Value <= result.Log.Entries[1].Tick.Value);
    }

    [Fact]
    public void RunUntilEnd_Deterministic_repeated_calls()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        List<ScriptProgram> programs = TwoTankScanPrograms();
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        LoggedCombinedRuntimeRunResult a = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 2);
        LoggedCombinedRuntimeRunResult b = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 2);

        Assert.Equal(a.RunResult.RunResult.TicksExecuted, b.RunResult.RunResult.TicksExecuted);
        Assert.Equal(a.RunResult.RunResult.EndCondition.Reason, b.RunResult.RunResult.EndCondition.Reason);
        Assert.Equal(a.Log.Entries.Count, b.Log.Entries.Count);
        Assert.Equal(a.Log.Entries[0].EventType, b.Log.Entries[0].EventType);
        Assert.Equal(a.Log.Entries[0].Tick, b.Log.Entries[0].Tick);
        Assert.Equal(a.Log.Entries[0].Message, b.Log.Entries[0].Message);
        Assert.Equal(a.Log.Entries[1].EventType, b.Log.Entries[1].EventType);
        Assert.Equal(a.Log.Entries[1].Tick, b.Log.Entries[1].Tick);
        Assert.Equal(a.Log.Entries[1].Message, b.Log.Entries[1].Message);
    }

    [Fact]
    public void RunUntilEnd_DoesNotMutateInitialRuntime()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        SimTick tickBefore = initialRuntime.State.CurrentTick;
        TankState tank0Before = initialRuntime.State.Tanks[0];
        MatchSensorLoadoutState loadoutsBefore = initialRuntime.SensorLoadouts;

        _ = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(tickBefore, initialRuntime.State.CurrentTick);
        Assert.Equal(tank0Before, initialRuntime.State.Tanks[0]);
        Assert.Same(loadoutsBefore, initialRuntime.SensorLoadouts);
    }

    #endregion

    #region RunUntilEndWithTickLogs

    [Fact]
    public void RunUntilEnd_StillReturnsOnlyStartAndEnd()
    {
        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            CreateTwoTankRuntime(new SimTick(0)),
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_MaxTicksZero_ReturnsOnlyStartAndEnd()
    {
        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 0);

        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
        Assert.DoesNotContain(
            result.Log.Entries,
            entry => entry.EventType == CombatLogEventTypes.ScriptTick);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_ScanOnly_IncludesScriptTickPerExecutedTick()
    {
        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 2);

        Assert.Equal(4, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[2].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[3].EventType);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_Fire_IncludesFireRequestApplied()
    {
        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(42),
                maxTicks: 1);

        Assert.Equal(4, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, result.Log.Entries[2].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[3].EventType);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_Fire_SpawnTickPolicyPreservesProjectile()
    {
        SimTick startTick = new SimTick(0);
        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(startTick),
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(42),
                maxTicks: 1);

        Assert.Equal(4, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, result.Log.Entries[2].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[3].EventType);

        MatchState finalState = result.RunResult.FinalRuntime.State;
        ProjectileState projectile = finalState.Projectiles[0];

        Assert.Single(finalState.Projectiles);
        Assert.True(projectile.IsActive);
        Assert.Equal(startTick, projectile.SpawnTick);
        Assert.Equal(new SimTick(1), finalState.CurrentTick);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, finalState.Tanks[1].CurrentHitPoints);
        Assert.Equal(new TankId(1), projectile.OwnerTankId);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_MixedRejectedApplied_RecordIndexOrderInMergedLog()
    {
        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntimeFirstFired(new SimTick(0)),
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0),
                maxTicks: 1);

        Assert.Equal(5, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.FireRejected, result.Log.Entries[2].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, result.Log.Entries[3].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[4].EventType);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_LogTicksAreNonDecreasing()
    {
        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 2);

        int previousTick = -1;
        foreach (CombatLogEntry entry in result.Log.Entries)
        {
            Assert.True(entry.Tick.Value >= previousTick);
            previousTick = entry.Tick.Value;
        }
    }

    [Fact]
    public void RunUntilEndWithTickLogs_Deterministic_RepeatedCalls()
    {
        var programs = TwoTankScanPrograms();
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        LoggedCombinedRuntimeRunResult a =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                programs,
                sequence,
                maxTicks: 2);
        LoggedCombinedRuntimeRunResult b =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                programs,
                sequence,
                maxTicks: 2);

        Assert.Equal(a.Log.Entries.Count, b.Log.Entries.Count);
        for (int i = 0; i < a.Log.Entries.Count; i++)
        {
            Assert.Equal(a.Log.Entries[i].Tick, b.Log.Entries[i].Tick);
            Assert.Equal(a.Log.Entries[i].EventType, b.Log.Entries[i].EventType);
            Assert.Equal(a.Log.Entries[i].Message, b.Log.Entries[i].Message);
        }
    }

    [Fact]
    public void RunUntilEndWithTickLogs_RunResult_MatchesUnloggedCombinedRuntimeRunner()
    {
        MatchSensorRuntimeState runtimeForUnlogged = CreateTwoTankRuntime(new SimTick(0));
        MatchSensorRuntimeState runtimeForLogged = CreateTwoTankRuntime(new SimTick(0));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(7);

        CombinedRuntimeRunResult unlogged = CombinedRuntimeRunner.RunUntilEnd(
            runtimeForUnlogged,
            programs,
            sequence,
            maxTicks: 1);

        LoggedCombinedRuntimeRunResult logged =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                runtimeForLogged,
                programs,
                sequence,
                maxTicks: 1);

        Assert.Equal(
            unlogged.RunResult.TicksExecuted,
            logged.RunResult.RunResult.TicksExecuted);
        Assert.Equal(
            unlogged.RunResult.EndCondition.Reason,
            logged.RunResult.RunResult.EndCondition.Reason);
        Assert.Equal(
            unlogged.FinalRuntime.State.CurrentTick,
            logged.RunResult.FinalRuntime.State.CurrentTick);
        Assert.Equal(
            unlogged.FinalProjectileIdSequence,
            logged.RunResult.FinalProjectileIdSequence);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_NegativeMaxTicks_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: -1));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    #endregion

    #region Movement application regression (5.106 / 5.116)

    [Fact]
    public void RunUntilEnd_MovementCommand_LogsOnlyStartEnd_AndStateHasVelocity()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;

        LoggedCombinedRuntimeRunResult result = LoggedCombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        AssertMvpMovementLogEvents(result.Log);

        TankState tank = result.RunResult.FinalRuntime.State.Tanks[0];
        Assert.Equal(ExpectedPatrolVelocity(), tank.Movement.VelocityPerTick);
        Assert.Equal(initialPosition + ExpectedPatrolVelocity(), tank.Movement.Position);
        Assert.Equal(new SimTick(1), result.RunResult.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_MovementCommand_IntegratesPosition_LogsScriptTickOnly()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedPatrolVelocity();
        FixedVec2 expectedPosition = initialPosition + velocity + velocity;

        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                initialRuntime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 2);

        AssertRichMovementLogSequence(result.Log, ticksExecuted: 2);

        TankState tank = result.RunResult.FinalRuntime.State.Tanks[0];
        Assert.Equal(velocity, tank.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank.Movement.Position);
        Assert.Equal(new SimTick(2), result.RunResult.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_MovementBoundsProjectileHit_LogsUnchanged()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateBoundsProjectileHitInitialRuntime(startTick);
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;

        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                initialRuntime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1);

        AssertRichMovementLogSequence(result.Log, ticksExecuted: 1);
        Assert.Equal(maxX, result.RunResult.FinalRuntime.State.Tanks[0].Movement.Position.X);
        Assert.Equal(80, result.RunResult.FinalRuntime.State.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void RunUntilEndWithTickLogs_MovementPastArenaBounds_ClampsState_LogsUnchanged()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime =
            CreateRuntimeAtRightEdgeWithEastVelocity(startTick);
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;

        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                initialRuntime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1);

        AssertRichMovementLogSequence(result.Log, ticksExecuted: 1);
        Assert.Equal(maxX, result.RunResult.FinalRuntime.State.Tanks[0].Movement.Position.X);
        Assert.Equal(FixedVec2.FromInts(1, 0), result.RunResult.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    #endregion

    #region Wall obstacle integration regression (5.130)

    private static FixedVec2 WallRegressionPrePosition()
        => FixedVec2.FromInts(5, 15);

    private static FixedVec2 WallRegressionVelocity()
        => FixedVec2.FromInts(3, 0);

    private static WallBlock CreateWall(string id, FixedRect bounds)
        => new WallBlock(id, bounds);

    private static FixedRect UnitWallAtTenByTen()
        => FixedRect.FromMinSize(
            FixedVec2.FromInts(10, 10),
            Fixed.FromInt(10),
            Fixed.FromInt(10));

    private static ArenaDefinition CreateWallArena(params WallBlock[] wallBlocks)
    {
        return new ArenaDefinition(
            id: "logged_wall_regression_arena",
            displayName: "Logged Wall Regression Arena",
            description: "Arena for logged combined runner wall regression tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
            },
            wallBlocks: wallBlocks,
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    private static MatchState CreateMatchState(
        ArenaDefinition arena,
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return new MatchState(arena, tick, tanks, weaponLoadouts, Array.Empty<ProjectileState>());
    }

    private static MatchSensorRuntimeState CreateWallBlockedMovementTwoTankRuntime(SimTick tick)
    {
        ArenaDefinition arena = CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        MatchState state = CreateMatchState(
            arena,
            tick,
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            CreateTankWithMovement(
                0,
                0,
                WallRegressionPrePosition(),
                WallRegressionVelocity()),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void RunUntilEndWithTickLogs_WallBlockedMovement_LogsUnchanged()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 prePosition = WallRegressionPrePosition();
        MatchSensorRuntimeState initialRuntime = CreateWallBlockedMovementTwoTankRuntime(startTick);

        LoggedCombinedRuntimeRunResult result =
            LoggedCombinedRuntimeRunner.RunUntilEndWithTickLogs(
                initialRuntime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1);

        AssertRichMovementLogSequence(result.Log, ticksExecuted: 1);
        Assert.Equal(prePosition, result.RunResult.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.All(
            result.Log.Entries,
            entry =>
            {
                Assert.True(
                    entry.EventType == CombatLogEventTypes.MatchStarted
                    || entry.EventType == CombatLogEventTypes.ScriptTick
                    || entry.EventType == CombatLogEventTypes.MatchEnded);
            });
    }

    #endregion
}
