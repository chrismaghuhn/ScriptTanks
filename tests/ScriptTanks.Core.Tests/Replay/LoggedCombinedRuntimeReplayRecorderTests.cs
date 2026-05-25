using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Replay;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Replay;

public sealed class LoggedCombinedRuntimeReplayRecorderTests
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
    public void RecordUntilEnd_NullInitialRuntime_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                null!,
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_NegativeMaxTicks_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: -1));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_NullPrograms_WithRunningFixture_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                null!,
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_MaxTicksZero_WithNullPrograms_DoesNotThrow()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                null!,
                new ProjectileIdSequence(0),
                maxTicks: 0);

        Assert.Equal(0, result.RecordedRunResult.RunResult.TicksExecuted);
        Assert.Single(result.RecordedRunResult.Recording.Frames);
        Assert.Equal(2, result.Log.Entries.Count);
    }

    #endregion

    #region Zero-tick / multi-tick

    [Fact]
    public void RecordUntilEnd_MaxTicksZero_ReturnsLoggedRecordedResult()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                initialRuntime,
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 0);

        Assert.Equal(0, result.RecordedRunResult.RunResult.TicksExecuted);
        Assert.Single(result.RecordedRunResult.Recording.Frames);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RecordedRunResult.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
        Assert.Equal(new SimTick(0), result.Log.Entries[0].Tick);
        Assert.Equal(new SimTick(0), result.Log.Entries[1].Tick);
        Assert.True(
            result.Log.Entries[0].Tick.Value <= result.Log.Entries[1].Tick.Value);
    }

    [Fact]
    public void RecordUntilEnd_ScanOnly_RecordsFramesAndLogsStartEnd()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                initialRuntime,
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 2);

        Assert.Equal(2, result.RecordedRunResult.RunResult.TicksExecuted);
        Assert.Equal(3, result.RecordedRunResult.Recording.Frames.Count);
        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(new SimTick(0), result.Log.Entries[0].Tick);
        Assert.Equal(new SimTick(2), result.Log.Entries[1].Tick);
        Assert.Equal(new SimTick(0), result.RecordedRunResult.Recording.Frames[0].State.CurrentTick);
        Assert.Equal(new SimTick(2), result.RecordedRunResult.Recording.Frames[^1].State.CurrentTick);
    }

    [Fact]
    public void RecordUntilEnd_FireProgram_MatchesUnloggedRecorder()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(7);

        MatchRecordedRunResult plain = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 1);

        LoggedCombinedRuntimeRecordedRunResult logged =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                initialRuntime,
                programs,
                sequence,
                maxTicks: 1);

        Assert.Equal(plain.Recording.Frames.Count, logged.RecordedRunResult.Recording.Frames.Count);
        Assert.Equal(
            plain.RunResult.FinalState.CurrentTick,
            logged.RecordedRunResult.RunResult.FinalState.CurrentTick);
        Assert.Equal(plain.RunResult.EndCondition.Reason, logged.RecordedRunResult.RunResult.EndCondition.Reason);
        Assert.Equal(plain.RunResult.TicksExecuted, logged.RecordedRunResult.RunResult.TicksExecuted);

        for (int i = 0; i < plain.Recording.Frames.Count; i++)
        {
            Assert.Equal(
                plain.Recording.Frames[i].State.CurrentTick,
                logged.RecordedRunResult.Recording.Frames[i].State.CurrentTick);
        }
    }

    #endregion

    #region Log content / determinism / purity / replay policy

    [Fact]
    public void RecordUntilEnd_LogContainsOnlyMatchStartedAndMatchEnded()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
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
    public void RecordUntilEnd_LogEntriesAreInNonDecreasingTickOrder()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 2);

        Assert.True(
            result.Log.Entries[0].Tick.Value <= result.Log.Entries[1].Tick.Value);
    }

    [Fact]
    public void RecordUntilEnd_Deterministic_repeated_calls()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        List<ScriptProgram> programs = TwoTankScanPrograms();
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        LoggedCombinedRuntimeRecordedRunResult a =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                initialRuntime,
                programs,
                sequence,
                maxTicks: 2);
        LoggedCombinedRuntimeRecordedRunResult b =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                initialRuntime,
                programs,
                sequence,
                maxTicks: 2);

        Assert.Equal(
            a.RecordedRunResult.RunResult.TicksExecuted,
            b.RecordedRunResult.RunResult.TicksExecuted);
        Assert.Equal(
            a.RecordedRunResult.RunResult.EndCondition.Reason,
            b.RecordedRunResult.RunResult.EndCondition.Reason);
        Assert.Equal(
            a.RecordedRunResult.Recording.Frames.Count,
            b.RecordedRunResult.Recording.Frames.Count);
        Assert.Equal(a.Log.Entries[0].EventType, b.Log.Entries[0].EventType);
        Assert.Equal(a.Log.Entries[0].Tick, b.Log.Entries[0].Tick);
        Assert.Equal(a.Log.Entries[0].Message, b.Log.Entries[0].Message);
        Assert.Equal(a.Log.Entries[1].EventType, b.Log.Entries[1].EventType);
        Assert.Equal(a.Log.Entries[1].Tick, b.Log.Entries[1].Tick);
        Assert.Equal(a.Log.Entries[1].Message, b.Log.Entries[1].Message);
    }

    [Fact]
    public void RecordUntilEnd_DoesNotMutateInitialRuntime()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        SimTick tickBefore = initialRuntime.State.CurrentTick;
        TankState tank0Before = initialRuntime.State.Tanks[0];
        MatchSensorLoadoutState loadoutsBefore = initialRuntime.SensorLoadouts;

        _ = LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            TwoTankScanPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(tickBefore, initialRuntime.State.CurrentTick);
        Assert.Equal(tank0Before, initialRuntime.State.Tanks[0]);
        Assert.Same(loadoutsBefore, initialRuntime.SensorLoadouts);
    }

    [Fact]
    public void RecordUntilEnd_RecordingFramesRemainMatchStateOnly()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 1);

        Assert.All(
            result.RecordedRunResult.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    #endregion

    #region RecordUntilEndWithTickLogs

    [Fact]
    public void RecordUntilEnd_StillReturnsOnlyStartAndEnd()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 2);

        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[1].EventType);
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_MaxTicksZero_ReturnsOnlyStartAndEndAndInitialFrame()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
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
        Assert.Single(result.RecordedRunResult.Recording.Frames);
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_ScanOnly_IncludesScriptTickPerExecutedTick()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 2);

        Assert.Equal(4, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.MatchStarted, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[2].EventType);
        Assert.Equal(CombatLogEventTypes.MatchEnded, result.Log.Entries[3].EventType);
        Assert.Equal(3, result.RecordedRunResult.Recording.Frames.Count);
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_Fire_IncludesFireRequestApplied()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
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
        Assert.All(
            result.RecordedRunResult.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_Fire_SpawnTickPolicyPreservesProjectile()
    {
        SimTick startTick = new SimTick(0);
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
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

        Assert.All(
            result.RecordedRunResult.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));

        MatchState finalState = result.RecordedRunResult.Recording.Frames[^1].State;
        ProjectileState projectile = finalState.Projectiles[0];

        Assert.Single(finalState.Projectiles);
        Assert.True(projectile.IsActive);
        Assert.Equal(startTick, projectile.SpawnTick);
        Assert.Equal(new SimTick(1), finalState.CurrentTick);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, finalState.Tanks[1].CurrentHitPoints);
        Assert.Equal(new TankId(1), projectile.OwnerTankId);
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_MixedRejectedApplied_RecordIndexOrderInMergedLog()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
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
    public void RecordUntilEndWithTickLogs_LogTicksAreNonDecreasing()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
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
    public void RecordUntilEndWithTickLogs_RecordedRun_MatchesUnloggedRecorder()
    {
        MatchSensorRuntimeState runtimeForUnlogged = CreateTwoTankRuntime(new SimTick(0));
        MatchSensorRuntimeState runtimeForLogged = CreateTwoTankRuntime(new SimTick(0));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(7);

        MatchRecordedRunResult unlogged = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            runtimeForUnlogged,
            programs,
            sequence,
            maxTicks: 1);

        LoggedCombinedRuntimeRecordedRunResult logged =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
                runtimeForLogged,
                programs,
                sequence,
                maxTicks: 1);

        Assert.Equal(
            unlogged.Recording.Frames.Count,
            logged.RecordedRunResult.Recording.Frames.Count);
        Assert.Equal(
            unlogged.RunResult.TicksExecuted,
            logged.RecordedRunResult.RunResult.TicksExecuted);
        Assert.Equal(
            unlogged.RunResult.EndCondition.Reason,
            logged.RecordedRunResult.RunResult.EndCondition.Reason);
        Assert.Equal(
            unlogged.RunResult.FinalState.CurrentTick,
            logged.RecordedRunResult.RunResult.FinalState.CurrentTick);

        for (int i = 0; i < unlogged.Recording.Frames.Count; i++)
        {
            Assert.Equal(
                unlogged.Recording.Frames[i].State.CurrentTick,
                logged.RecordedRunResult.Recording.Frames[i].State.CurrentTick);
        }
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_Deterministic_RepeatedCalls()
    {
        var programs = TwoTankScanPrograms();
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        LoggedCombinedRuntimeRecordedRunResult a =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                programs,
                sequence,
                maxTicks: 2);
        LoggedCombinedRuntimeRecordedRunResult b =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
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

        Assert.Equal(
            a.RecordedRunResult.Recording.Frames.Count,
            b.RecordedRunResult.Recording.Frames.Count);
        for (int i = 0; i < a.RecordedRunResult.Recording.Frames.Count; i++)
        {
            Assert.Equal(
                a.RecordedRunResult.Recording.Frames[i].State.CurrentTick,
                b.RecordedRunResult.Recording.Frames[i].State.CurrentTick);
        }
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_FramesRemainMatchStateOnly()
    {
        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: 1);

        Assert.All(
            result.RecordedRunResult.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_NegativeMaxTicks_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
                CreateTwoTankRuntime(new SimTick(0)),
                TwoTankScanPrograms(),
                new ProjectileIdSequence(0),
                maxTicks: -1));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    #endregion

    #region Movement application regression (5.106 / 5.116)

    [Fact]
    public void RecordUntilEnd_MovementCommand_RecordsMovingFrames_LogsOnlyStartEnd()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedPatrolVelocity();

        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEnd(
                initialRuntime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1);

        AssertMvpMovementLogEvents(result.Log);
        Assert.Equal(2, result.RecordedRunResult.Recording.Frames.Count);
        Assert.Equal(initialPosition, result.RecordedRunResult.Recording.Frames[0].State.Tanks[0].Movement.Position);

        MatchState finalState = result.RecordedRunResult.Recording.Frames[^1].State;
        Assert.Equal(velocity, finalState.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(initialPosition + velocity, finalState.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(1), finalState.CurrentTick);
        Assert.Same(result.RecordedRunResult.RunResult.FinalState, finalState);
        Assert.All(
            result.RecordedRunResult.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    [Fact]
    public void RecordUntilEndWithTickLogs_MovementCommand_RecordsMovingFrames_LogsScriptTicksOnly()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedPatrolVelocity();

        LoggedCombinedRuntimeRecordedRunResult result =
            LoggedCombinedRuntimeReplayRecorder.RecordUntilEndWithTickLogs(
                initialRuntime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 2);

        AssertRichMovementLogSequence(result.Log, ticksExecuted: 2);
        Assert.Equal(3, result.RecordedRunResult.Recording.Frames.Count);
        Assert.Equal(initialPosition, result.RecordedRunResult.Recording.Frames[0].State.Tanks[0].Movement.Position);
        Assert.Equal(
            initialPosition + velocity,
            result.RecordedRunResult.Recording.Frames[1].State.Tanks[0].Movement.Position);
        Assert.Equal(
            initialPosition + velocity + velocity,
            result.RecordedRunResult.Recording.Frames[2].State.Tanks[0].Movement.Position);

        MatchState finalState = result.RecordedRunResult.Recording.Frames[^1].State;
        Assert.Equal(velocity, finalState.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(new SimTick(2), finalState.CurrentTick);
        Assert.Same(result.RecordedRunResult.RunResult.FinalState, finalState);
        Assert.All(
            result.RecordedRunResult.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    #endregion
}
