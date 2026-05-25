using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
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

public sealed class LoggedCombinedRuntimeTickPipelineTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankWeaponLoadout CreateWeaponLoadout(params WeaponState[] weapons)
    {
        return new TankWeaponLoadout(weapons);
    }

    private static TankWeaponLoadout CreateFiredLoadout(SimTick lastFireTick)
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon).MarkFired(lastFireTick),
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

    private static MatchSensorRuntimeState CreateTwoTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntimeFirstFired(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateFiredLoadout(tick),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static void AssertLogsEqual(CombatLog actual, CombatLog expected)
    {
        Assert.Equal(expected.Entries.Count, actual.Entries.Count);
        for (int i = 0; i < expected.Entries.Count; i++)
        {
            Assert.Equal(expected.Entries[i].Tick, actual.Entries[i].Tick);
            Assert.Equal(expected.Entries[i].Category, actual.Entries[i].Category);
            Assert.Equal(expected.Entries[i].EventType, actual.Entries[i].EventType);
            Assert.Equal(expected.Entries[i].Message, actual.Entries[i].Message);
        }
    }

    #region Validation

    [Fact]
    public void Step_NullRuntime_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            LoggedCombinedRuntimeTickPipeline.Step(
                null!,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Step_NullPrograms_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            LoggedCombinedRuntimeTickPipeline.Step(
                CreateTwoTankRuntime(new SimTick(10)),
                null!,
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Step_ProgramCountMismatch_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            LoggedCombinedRuntimeTickPipeline.Step(
                CreateTwoTankRuntime(new SimTick(10)),
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Result and parity

    [Fact]
    public void Step_ReturnsLoggedCombinedRuntimeTickResult()
    {
        LoggedCombinedRuntimeTickResult result = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(0)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0));

        Assert.NotNull(result);
        Assert.NotNull(result.TickResult);
        Assert.NotNull(result.Log);
    }

    [Fact]
    public void Step_TickResult_MatchesUnloggedPipeline()
    {
        MatchSensorRuntimeState runtimeForUnlogged = CreateTwoTankRuntime(new SimTick(10));
        MatchSensorRuntimeState runtimeForLogged = CreateTwoTankRuntime(new SimTick(10));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(42);

        CombinedRuntimeTickResult unlogged = CombinedRuntimeTickPipeline.Step(
            runtimeForUnlogged,
            programs,
            sequence);

        LoggedCombinedRuntimeTickResult logged = LoggedCombinedRuntimeTickPipeline.Step(
            runtimeForLogged,
            programs,
            sequence);

        Assert.Same(runtimeForLogged, logged.TickResult.InitialRuntime);
        Assert.Same(runtimeForUnlogged, unlogged.InitialRuntime);
        Assert.Equal(
            logged.TickResult.FinalRuntime.State.CurrentTick,
            unlogged.FinalRuntime.State.CurrentTick);
        Assert.Equal(
            logged.TickResult.FinalProjectileIdSequence,
            unlogged.FinalProjectileIdSequence);
        Assert.Equal(
            logged.TickResult.ScriptResult.FinalRuntime.State.Projectiles.Count,
            unlogged.ScriptResult.FinalRuntime.State.Projectiles.Count);
        Assert.Equal(
            logged.TickResult.ScriptResult.FireApplicationResult.Count,
            unlogged.ScriptResult.FireApplicationResult.Count);

        for (int i = 0; i < logged.TickResult.ScriptResult.FireApplicationResult.Count; i++)
        {
            Assert.Equal(
                logged.TickResult.ScriptResult.FireApplicationResult.GetRecordAtIndex(i).Status,
                unlogged.ScriptResult.FireApplicationResult.GetRecordAtIndex(i).Status);
        }
    }

    [Fact]
    public void Step_Log_MatchesFactoryOutput()
    {
        LoggedCombinedRuntimeTickResult logged = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(10)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(42));

        CombatLog expectedLog =
            CombinedRuntimeTickCombatLogFactory.CreateTickLog(logged.TickResult);

        AssertLogsEqual(logged.Log, expectedLog);
    }

    #endregion

    #region Log scope and order

    [Fact]
    public void Step_ScanOnly_LogContainsOnlyScriptTick()
    {
        LoggedCombinedRuntimeTickResult result = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(0)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0));

        Assert.Single(result.Log.Entries);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[0].EventType);
    }

    [Fact]
    public void Step_Fire_LogContainsScriptTickAndFireApplied()
    {
        LoggedCombinedRuntimeTickResult result = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(10)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(42));

        Assert.Equal(2, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, result.Log.Entries[1].EventType);
    }

    [Fact]
    public void Step_MixedRejectedApplied_LogUsesRecordIndexOrder()
    {
        LoggedCombinedRuntimeTickResult result = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntimeFirstFired(new SimTick(10)),
            new List<ScriptProgram>
            {
                FireWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(0));

        Assert.Equal(3, result.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.ScriptTick, result.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireRejected, result.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, result.Log.Entries[2].EventType);
        Assert.Contains("record_index=0", result.Log.Entries[1].Message);
        Assert.Contains("record_index=1", result.Log.Entries[2].Message);
    }

    #endregion

    #region Determinism and purity

    [Fact]
    public void Step_Deterministic_RepeatedCalls()
    {
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(42);

        LoggedCombinedRuntimeTickResult a = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(10)),
            programs,
            sequence);
        LoggedCombinedRuntimeTickResult b = LoggedCombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(10)),
            programs,
            sequence);

        Assert.Equal(
            a.TickResult.FinalRuntime.State.CurrentTick,
            b.TickResult.FinalRuntime.State.CurrentTick);
        Assert.Equal(
            a.TickResult.FinalProjectileIdSequence,
            b.TickResult.FinalProjectileIdSequence);
        AssertLogsEqual(a.Log, b.Log);
    }

    [Fact]
    public void Step_DoesNotMutateInputRuntime()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(0));
        SimTick tickBefore = runtime.State.CurrentTick;
        TankState tank0Before = runtime.State.Tanks[0];
        MatchSensorLoadoutState loadoutsBefore = runtime.SensorLoadouts;

        _ = LoggedCombinedRuntimeTickPipeline.Step(
            runtime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0));

        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(tank0Before, runtime.State.Tanks[0]);
        Assert.Same(loadoutsBefore, runtime.SensorLoadouts);
    }

    #endregion
}
