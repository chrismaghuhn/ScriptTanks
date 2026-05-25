using System;
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

public sealed class LoggedCombinedRuntimeRecordedRunResultTests
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

    private static MatchReplayRecording CreateRecording()
    {
        MatchReplayFrame frame = new MatchReplayFrame(0, CreateState());
        return new MatchReplayRecording(new[] { frame });
    }

    private static MatchRecordedRunResult CreateRecordedRunResult()
    {
        return new MatchRecordedRunResult(
            CreateRunResult(),
            CreateRecording());
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
        MatchRecordedRunResult recordedRunResult = CreateRecordedRunResult();
        CombatLog log = CreateLog();

        LoggedCombinedRuntimeRecordedRunResult result =
            new LoggedCombinedRuntimeRecordedRunResult(recordedRunResult, log);

        Assert.Same(recordedRunResult, result.RecordedRunResult);
        Assert.Same(log, result.Log);
    }

    [Fact]
    public void Constructor_RejectsNullRecordedRunResult()
    {
        CombatLog log = CreateLog();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedCombinedRuntimeRecordedRunResult(null!, log));
        Assert.Equal("recordedRunResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullLog()
    {
        MatchRecordedRunResult recordedRunResult = CreateRecordedRunResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedCombinedRuntimeRecordedRunResult(recordedRunResult, null!));
        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsEmptyLog()
    {
        MatchRecordedRunResult recordedRunResult = CreateRecordedRunResult();
        CombatLog emptyLog = new CombatLogBuilder().Build();

        LoggedCombinedRuntimeRecordedRunResult result =
            new LoggedCombinedRuntimeRecordedRunResult(recordedRunResult, emptyLog);

        Assert.Same(emptyLog, result.Log);
        Assert.Empty(result.Log.Entries);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchRecordedRunResult recordedRunResult = CreateRecordedRunResult();
        CombatLog log = CreateLog();
        LoggedCombinedRuntimeRecordedRunResult a =
            new LoggedCombinedRuntimeRecordedRunResult(recordedRunResult, log);
        LoggedCombinedRuntimeRecordedRunResult b =
            new LoggedCombinedRuntimeRecordedRunResult(recordedRunResult, log);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
