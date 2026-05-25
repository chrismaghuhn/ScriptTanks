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

public sealed class LoggedCombinedRuntimeTickResultTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
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

    private static MatchSensorRuntimeState CreateSingleTankRuntime()
    {
        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(0),
            new[] { CreateTank(0, 0) },
            new[] { CreateWeaponLoadout() },
            Array.Empty<ProjectileState>());
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(new[]
            {
                new TankSensorLoadout(new[] { SensorState.Ready(SensorCatalog.BasicRadar) }),
            }));
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

    private static CombinedRuntimeTickResult CreateTickResult()
    {
        return CombinedRuntimeTickPipeline.Step(
            CreateSingleTankRuntime(),
            new List<ScriptProgram> { ScanWhenAlways() },
            new ProjectileIdSequence(0));
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

    [Fact]
    public void Constructor_PreservesValues()
    {
        CombinedRuntimeTickResult tickResult = CreateTickResult();
        CombatLog log = CreateLog();

        LoggedCombinedRuntimeTickResult result = new LoggedCombinedRuntimeTickResult(
            tickResult,
            log);

        Assert.Same(tickResult, result.TickResult);
        Assert.Same(log, result.Log);
    }

    [Fact]
    public void Constructor_RejectsNullTickResult()
    {
        CombatLog log = CreateLog();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedCombinedRuntimeTickResult(null!, log));
        Assert.Equal("tickResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullLog()
    {
        CombinedRuntimeTickResult tickResult = CreateTickResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedCombinedRuntimeTickResult(tickResult, null!));
        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsEmptyLog()
    {
        CombinedRuntimeTickResult tickResult = CreateTickResult();
        CombatLog emptyLog = new CombatLogBuilder().Build();

        LoggedCombinedRuntimeTickResult result = new LoggedCombinedRuntimeTickResult(
            tickResult,
            emptyLog);

        Assert.Same(emptyLog, result.Log);
        Assert.Empty(result.Log.Entries);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        CombinedRuntimeTickResult tickResult = CreateTickResult();
        CombatLog log = CreateLog();
        LoggedCombinedRuntimeTickResult a = new LoggedCombinedRuntimeTickResult(tickResult, log);
        LoggedCombinedRuntimeTickResult b = new LoggedCombinedRuntimeTickResult(tickResult, log);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
