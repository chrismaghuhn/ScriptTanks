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

public sealed class MatchSensorRuntimeEndedRunResultTests
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

    private static MatchSensorRuntimeState CreateRuntime()
    {
        TankState tankA = CreateTank(0, 0);
        TankState tankB = CreateTank(1, 1);

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            SimTick.Zero,
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

    private static MatchEndConditionResult CreateEndCondition(
        MatchSensorRuntimeState runtime)
    {
        return MatchEndConditionEvaluator.Evaluate(
            runtime.State,
            maxTicks: 100);
    }

    [Fact]
    public void Constructor_RejectsNullFinalRuntime_WithParamNameFinalRuntime()
    {
        MatchEndConditionResult endCondition = MatchEndConditionResult.Running();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeEndedRunResult(
                null!,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeTicksExecuted_WithParamNameTicksExecuted()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeEndedRunResult(
                runtime,
                ticksExecuted: -1,
                nextScheduledScanIndex: 0,
                endCondition));

        Assert.Equal("ticksExecuted", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeNextScheduledScanIndex_WithParamNameNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeEndedRunResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: -1,
                endCondition));

        Assert.Equal("nextScheduledScanIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullEndCondition_WithParamNameEndCondition()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeEndedRunResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition: null!));

        Assert.Equal("endCondition", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsZeroTicksExecuted()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 0,
            nextScheduledScanIndex: 1,
            endCondition);

        Assert.Equal(0, result.TicksExecuted);
    }

    [Fact]
    public void Constructor_AllowsZeroNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 5,
            nextScheduledScanIndex: 0,
            endCondition);

        Assert.Equal(0, result.NextScheduledScanIndex);
    }

    [Fact]
    public void Constructor_PreservesFinalRuntimeReference()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 2,
            nextScheduledScanIndex: 3,
            endCondition);

        Assert.Same(runtime, result.FinalRuntime);
    }

    [Fact]
    public void Constructor_PreservesTicksExecutedAndNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 7,
            nextScheduledScanIndex: 4,
            endCondition);

        Assert.Equal(7, result.TicksExecuted);
        Assert.Equal(4, result.NextScheduledScanIndex);
    }

    [Fact]
    public void Constructor_PreservesEndConditionReference()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 1,
            nextScheduledScanIndex: 2,
            endCondition);

        Assert.Same(endCondition, result.EndCondition);
    }

    [Fact]
    public void Instances_AreDistinctAndUseDefaultReferenceEquality()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var first = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1,
            endCondition);

        var second = new MatchSensorRuntimeEndedRunResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1,
            endCondition);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }
}
