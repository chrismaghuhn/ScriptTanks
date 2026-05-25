using System;
using System.Linq;
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

public sealed class MatchSensorRuntimeFixedTickRunnerTests
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
        params TankState[] tanks)
    {
        TankWeaponLoadout[] loadouts = tanks
            .Select(_ => CreateWeaponLoadout())
            .ToArray();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            loadouts,
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

    private static MatchScheduledSensorScanRequest CreateScheduled(
        int tick,
        int tankIndex = 0,
        int sensorSlot = 0)
    {
        return new MatchScheduledSensorScanRequest(
            new SimTick(tick),
            new MatchSensorScanRequest(
                tankIndex,
                new SensorSlot(sensorSlot)));
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime(SimTick tick)
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));

        MatchState state = CreateMatchState(tick, scanner, enemy);

        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void RunForTicks_RejectsNullInitialRuntime_WithParamNameInitialRuntime()
    {
        MatchSensorRuntimeState? runtime = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime!,
                maxTicks: 1,
                Array.Empty<MatchScheduledSensorScanRequest>()));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void RunForTicks_RejectsNegativeMaxTicks_WithParamNameMaxTicks()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: -1,
                Array.Empty<MatchScheduledSensorScanRequest>()));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RunForTicks_DelegatesNullScheduleValidation_WithParamNameScheduledScanRequests()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 0,
                null!));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void RunForTicks_DelegatesInvalidScheduleBeforeInitialTick_WithParamNameScheduledScanRequests()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 1,
                new[] { CreateScheduled(9) }));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void RunForTicks_DelegatesDuplicateScheduleTickValidation_WithParamNameScheduledScanRequests()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 1,
                new[]
                {
                    CreateScheduled(10),
                    CreateScheduled(10),
                }));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void RunForTicks_WithZeroMaxTicks_ReturnsInitialRuntimeReference()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 0,
                Array.Empty<MatchScheduledSensorScanRequest>());

        Assert.Same(runtime, result.FinalRuntime);
    }

    [Fact]
    public void RunForTicks_WithZeroMaxTicks_ReturnsTicksExecutedZeroAndNextScheduledScanIndexZero()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 0,
                Array.Empty<MatchScheduledSensorScanRequest>());

        Assert.Equal(0, result.TicksExecuted);
        Assert.Equal(0, result.NextScheduledScanIndex);
    }

    [Fact]
    public void RunForTicks_WithOneTick_AdvancesMatchStateTickByOne()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 1,
                Array.Empty<MatchScheduledSensorScanRequest>());

        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunForTicks_WithMultipleTicks_AdvancesMatchStateTickByMaxTicks()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 7,
                Array.Empty<MatchScheduledSensorScanRequest>());

        Assert.Equal(new SimTick(17), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunForTicks_WithNoScheduledScans_PreservesNextScheduledScanIndexZero()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 4,
                Array.Empty<MatchScheduledSensorScanRequest>());

        Assert.Equal(0, result.NextScheduledScanIndex);
    }

    [Fact]
    public void RunForTicks_AppliesScheduledScanAtInitialTickAndIncrementsNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        var schedule = new[]
        {
            CreateScheduled(10, tankIndex: 0, sensorSlot: 0),
        };

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 1,
                schedule);

        Assert.Equal(1, result.NextScheduledScanIndex);
        Assert.Equal(
            new SimTick(10),
            result.FinalRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunForTicks_AppliesFutureScheduledScanWhenItsTickIsReached()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        var schedule = new[]
        {
            CreateScheduled(12, tankIndex: 0, sensorSlot: 0),
        };

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 3,
                schedule);

        Assert.Equal(1, result.NextScheduledScanIndex);
        Assert.Equal(
            new SimTick(12),
            result.FinalRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);
        Assert.Equal(new SimTick(13), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunForTicks_LeavesFutureUnreachedScheduledScanIndexUnchanged()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        var schedule = new[]
        {
            CreateScheduled(15, tankIndex: 0, sensorSlot: 0),
        };

        MatchSensorRuntimeRunResult result =
            MatchSensorRuntimeFixedTickRunner.RunForTicks(
                runtime,
                maxTicks: 3,
                schedule);

        Assert.Equal(0, result.NextScheduledScanIndex);
        Assert.Equal(new SimTick(13), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunForTicks_DoesNotMutateOriginalRuntime()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));
        SensorState originalSensor = runtime.SensorLoadouts
            .GetLoadoutAtIndex(0)
            .GetSensor(SensorSlot.Zero);
        MatchState originalState = runtime.State;

        _ = MatchSensorRuntimeFixedTickRunner.RunForTicks(
            runtime,
            maxTicks: 1,
            new[] { CreateScheduled(10) });

        Assert.Same(originalState, runtime.State);
        Assert.Equal(originalSensor, runtime.SensorLoadouts
            .GetLoadoutAtIndex(0)
            .GetSensor(SensorSlot.Zero));
    }
}
