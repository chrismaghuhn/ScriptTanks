using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchSensorRuntimeReplaySummaryFactoryTests
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

    private static MatchSensorRuntimeState CreateRuntimeAtTick(int tick)
    {
        TankState tankA = CreateTank(0, 0);
        TankState tankB = CreateTank(1, 1);

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(tick),
            new[] { tankA, tankB },
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            Array.Empty<ProjectileState>());

        MatchSensorLoadoutState sensorLoadouts = new MatchSensorLoadoutState(new[]
        {
            new TankSensorLoadout(new[]
            {
                SensorState.Ready(SensorCatalog.BasicRadar),
            }),
            new TankSensorLoadout(new[]
            {
                SensorState.Ready(SensorCatalog.BasicRadar),
            }),
        });

        return new MatchSensorRuntimeState(state, sensorLoadouts);
    }

    private static MatchSensorRuntimeRunWithReplayResult CreateCompletedTimeoutRun()
    {
        MatchSensorRuntimeState initialRuntime = CreateRuntimeAtTick(10);
        MatchSensorRuntimeState finalRuntime = CreateRuntimeAtTick(13);
        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(
                finalRuntime.State,
                maxTicks: 13);

        return new MatchSensorRuntimeRunWithReplayResult(
            finalRuntime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 0,
            endCondition,
            new[]
            {
                new MatchSensorRuntimeReplayFrame(0, initialRuntime),
                new MatchSensorRuntimeReplayFrame(1, CreateRuntimeAtTick(11)),
                new MatchSensorRuntimeReplayFrame(2, CreateRuntimeAtTick(12)),
                new MatchSensorRuntimeReplayFrame(3, finalRuntime),
            });
    }

    [Fact]
    public void Create_RejectsNullResult_WithParamNameResult()
    {
        MatchSensorRuntimeRunWithReplayResult? result = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeReplaySummaryFactory.Create(result!));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Create_MapsFrameCountFromReplayFramesCount()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateCompletedTimeoutRun();

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.Equal(4, summary.FrameCount);
    }

    [Fact]
    public void Create_MapsTicksExecuted()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateCompletedTimeoutRun();

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.Equal(3, summary.TicksExecuted);
    }

    [Fact]
    public void Create_MapsInitialTickFromFirstFrameRuntimeState()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateCompletedTimeoutRun();

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.Equal(new SimTick(10), summary.InitialTick);
    }

    [Fact]
    public void Create_MapsFinalTickFromFinalRuntimeState()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateCompletedTimeoutRun();

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.Equal(new SimTick(13), summary.FinalTick);
    }

    [Fact]
    public void Create_MapsDidEndFromEndCondition()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateCompletedTimeoutRun();

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.True(summary.DidEnd);
    }

    [Fact]
    public void Create_MapsEndReasonFromEndCondition()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateCompletedTimeoutRun();

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.Equal(MatchEndReason.TimeoutDraw, summary.EndReason);
    }
    [Fact]
    public void Create_DoesNotRequireFinalReplayFrameToMatchFinalRuntime()
    {
        MatchSensorRuntimeState initialRuntime = CreateRuntimeAtTick(10);
        MatchSensorRuntimeState finalRuntime = CreateRuntimeAtTick(13);
        MatchSensorRuntimeState unrelatedLastFrameRuntime = CreateRuntimeAtTick(11);

        var result = new MatchSensorRuntimeRunWithReplayResult(
            finalRuntime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 0,
            MatchEndConditionEvaluator.Evaluate(finalRuntime.State, maxTicks: 13),
            new[]
            {
                new MatchSensorRuntimeReplayFrame(0, initialRuntime),
                new MatchSensorRuntimeReplayFrame(1, unrelatedLastFrameRuntime),
            });

        MatchSensorRuntimeReplaySummary summary =
            MatchSensorRuntimeReplaySummaryFactory.Create(result);

        Assert.Equal(new SimTick(13), summary.FinalTick);
    }
}
