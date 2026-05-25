using System;
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

public sealed class LoggedMatchRunResultTests
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

    private static MatchRunResult CreateRunResult()
    {
        MatchState state = CreateState();
        return new MatchRunResult(
            state,
            MatchEndConditionResult.TimeoutDraw(),
            ticksExecuted: 0);
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
        MatchRunResult runResult = CreateRunResult();
        CombatLog log = CreateLog();

        LoggedMatchRunResult result = new LoggedMatchRunResult(runResult, log);

        Assert.Same(runResult, result.RunResult);
        Assert.Same(log, result.Log);
    }

    [Fact]
    public void Constructor_RejectsNullRunResult()
    {
        CombatLog log = CreateLog();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedMatchRunResult(null!, log));
        Assert.Equal("runResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullLog()
    {
        MatchRunResult runResult = CreateRunResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedMatchRunResult(runResult, null!));
        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsEmptyLog()
    {
        MatchRunResult runResult = CreateRunResult();
        CombatLog emptyLog = new CombatLogBuilder().Build();

        LoggedMatchRunResult result = new LoggedMatchRunResult(runResult, emptyLog);

        Assert.Same(emptyLog, result.Log);
        Assert.Empty(result.Log.Entries);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchRunResult runResult = CreateRunResult();
        CombatLog log = CreateLog();
        LoggedMatchRunResult a = new LoggedMatchRunResult(runResult, log);
        LoggedMatchRunResult b = new LoggedMatchRunResult(runResult, log);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
