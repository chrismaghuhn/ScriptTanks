using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class MatchCombatLogFactoryTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        int? hp = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    [Fact]
    public void CreateMatchStartedLog_ReturnsExpectedEntry()
    {
        SimTick tick = new SimTick(5);

        CombatLog log = MatchCombatLogFactory.CreateMatchStartedLog(tick);

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Match, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.MatchStarted, log.Entries[0].EventType);
        Assert.Equal("Match started.", log.Entries[0].Message);
        Assert.Equal(new SimTick(5), log.Entries[0].Tick);
    }

    [Fact]
    public void CreateMatchEndedLog_WhenTankDestroyed_ReturnsExpectedEntry()
    {
        TankState winner = CreateTank(0, 0);
        MatchEndConditionResult endCondition =
            MatchEndConditionResult.TankDestroyed(winner);

        CombatLog log = MatchCombatLogFactory.CreateMatchEndedLog(
            new SimTick(5),
            endCondition);

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Match, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.MatchEnded, log.Entries[0].EventType);
        Assert.Equal(
            "Match ended: tank 0 won by tank destruction.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateMatchEndedLog_WhenTimeoutHpAdvantage_ReturnsExpectedEntry()
    {
        TankState winner = CreateTank(0, 0, hp: 80);
        MatchEndConditionResult endCondition =
            MatchEndConditionResult.TimeoutHpAdvantage(winner);

        CombatLog log = MatchCombatLogFactory.CreateMatchEndedLog(
            new SimTick(5),
            endCondition);

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Match, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.MatchEnded, log.Entries[0].EventType);
        Assert.Equal(
            "Match ended: tank 0 won by timeout HP advantage.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateMatchEndedLog_WhenTimeoutDraw_ReturnsExpectedEntry()
    {
        MatchEndConditionResult endCondition =
            MatchEndConditionResult.TimeoutDraw();

        CombatLog log = MatchCombatLogFactory.CreateMatchEndedLog(
            new SimTick(5),
            endCondition);

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Match, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.MatchEnded, log.Entries[0].EventType);
        Assert.Equal(
            "Match ended: timeout draw.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateMatchEndedLog_UsesProvidedTick()
    {
        SimTick tick = new SimTick(42);
        MatchEndConditionResult endCondition =
            MatchEndConditionResult.TimeoutDraw();

        CombatLog log = MatchCombatLogFactory.CreateMatchEndedLog(tick, endCondition);

        Assert.Equal(new SimTick(42), log.Entries[0].Tick);
    }

    [Fact]
    public void CreateMatchEndedLog_RejectsNullEndCondition()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchCombatLogFactory.CreateMatchEndedLog(
                new SimTick(5),
                null!));
        Assert.Equal("endCondition", ex.ParamName);
    }

    [Fact]
    public void CreateMatchEndedLog_RejectsRunningEndCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchCombatLogFactory.CreateMatchEndedLog(
                new SimTick(5),
                MatchEndConditionResult.Running()));
        Assert.Equal("endCondition", ex.ParamName);
    }
}
