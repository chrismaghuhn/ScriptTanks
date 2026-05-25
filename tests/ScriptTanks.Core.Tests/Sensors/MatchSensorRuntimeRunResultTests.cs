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

public sealed class MatchSensorRuntimeRunResultTests
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
        TankState tank = CreateTank(0, 0);

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            SimTick.Zero,
            new[] { tank },
            new[] { CreateWeaponLoadout() },
            Array.Empty<ProjectileState>());

        MatchSensorLoadoutState sensorLoadouts = new MatchSensorLoadoutState(new[]
        {
            new TankSensorLoadout(new[]
            {
                SensorState.Ready(SensorCatalog.BasicRadar),
            }),
        });

        return new MatchSensorRuntimeState(state, sensorLoadouts);
    }

    [Fact]
    public void Constructor_RejectsNullFinalRuntime_WithParamNameFinalRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeRunResult(null!, 0, 0));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeTicksExecuted_WithParamNameTicksExecuted()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeRunResult(runtime, -1, 0));

        Assert.Equal("ticksExecuted", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeNextScheduledScanIndex_WithParamNameNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeRunResult(runtime, 0, -1));

        Assert.Equal("nextScheduledScanIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsZeroTicksExecuted()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var result = new MatchSensorRuntimeRunResult(runtime, 0, 5);

        Assert.Equal(0, result.TicksExecuted);
    }

    [Fact]
    public void Constructor_AllowsZeroNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var result = new MatchSensorRuntimeRunResult(runtime, 5, 0);

        Assert.Equal(0, result.NextScheduledScanIndex);
    }

    [Fact]
    public void Constructor_PreservesFinalRuntimeReference()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var result = new MatchSensorRuntimeRunResult(runtime, 4, 2);

        Assert.Same(runtime, result.FinalRuntime);
    }

    [Fact]
    public void Constructor_PreservesTicksExecutedAndNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var result = new MatchSensorRuntimeRunResult(runtime, ticksExecuted: 3, nextScheduledScanIndex: 1);

        Assert.Equal(3, result.TicksExecuted);
        Assert.Equal(1, result.NextScheduledScanIndex);
    }

    [Fact]
    public void Equals_UsesReferenceEqualityForInstances()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var first = new MatchSensorRuntimeRunResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1);

        var second = new MatchSensorRuntimeRunResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }
}
