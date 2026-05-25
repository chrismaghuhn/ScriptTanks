using System;
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

public sealed class LoggedCombinedRuntimeRunResultTests
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

    private static CombinedRuntimeRunResult CreateRunResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchRunResult runResult = new MatchRunResult(
            runtime.State,
            MatchEndConditionResult.TimeoutDraw(),
            ticksExecuted: 0);
        return new CombinedRuntimeRunResult(
            runtime,
            runResult,
            new ProjectileIdSequence(42),
            lastTickResult: null);
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
        CombinedRuntimeRunResult runResult = CreateRunResult();
        CombatLog log = CreateLog();

        LoggedCombinedRuntimeRunResult result = new LoggedCombinedRuntimeRunResult(
            runResult,
            log);

        Assert.Same(runResult, result.RunResult);
        Assert.Same(log, result.Log);
    }

    [Fact]
    public void Constructor_RejectsNullRunResult()
    {
        CombatLog log = CreateLog();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedCombinedRuntimeRunResult(null!, log));
        Assert.Equal("runResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullLog()
    {
        CombinedRuntimeRunResult runResult = CreateRunResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedCombinedRuntimeRunResult(runResult, null!));
        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsEmptyLog()
    {
        CombinedRuntimeRunResult runResult = CreateRunResult();
        CombatLog emptyLog = new CombatLogBuilder().Build();

        LoggedCombinedRuntimeRunResult result = new LoggedCombinedRuntimeRunResult(
            runResult,
            emptyLog);

        Assert.Same(emptyLog, result.Log);
        Assert.Empty(result.Log.Entries);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        CombinedRuntimeRunResult runResult = CreateRunResult();
        CombatLog log = CreateLog();
        LoggedCombinedRuntimeRunResult a = new LoggedCombinedRuntimeRunResult(runResult, log);
        LoggedCombinedRuntimeRunResult b = new LoggedCombinedRuntimeRunResult(runResult, log);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
