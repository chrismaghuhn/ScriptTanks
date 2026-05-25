using System;
using System.Collections.Generic;
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

public sealed class MatchSensorRuntimeScheduledTickPipelineTests
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
            new MovementState(
                position ?? FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
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

    [Fact]
    public void Outcome_RejectsNullUpdatedRuntime_WithParamNameUpdatedRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeScheduledTickOutcome(false, null, null!, 0));

        Assert.Equal("updatedRuntime", ex.ParamName);
    }

    [Fact]
    public void Outcome_RejectsNegativeNextIndex_WithParamNameNextIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeScheduledTickOutcome(false, null, runtime, -1));

        Assert.Equal("nextIndex", ex.ParamName);
    }

    [Fact]
    public void Outcome_RejectsDidScanTrueWithNullScanResult_WithParamNameScanResult()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeScheduledTickOutcome(true, null, runtime, 0));

        Assert.Equal("scanResult", ex.ParamName);
    }

    [Fact]
    public void Outcome_AllowsDidScanFalseWithNullScanResult()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var outcome = new MatchSensorRuntimeScheduledTickOutcome(false, null, runtime, 3);

        Assert.False(outcome.DidScan);
        Assert.Null(outcome.ScanResult);
        Assert.Equal(3, outcome.NextIndex);
    }

    [Fact]
    public void Outcome_PreservesValuesAndReferences()
    {
        SensorScanResult scanResult = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var outcome = new MatchSensorRuntimeScheduledTickOutcome(true, scanResult, runtime, 7);

        Assert.True(outcome.DidScan);
        Assert.Same(scanResult, outcome.ScanResult);
        Assert.Same(runtime, outcome.UpdatedRuntime);
        Assert.Equal(7, outcome.NextIndex);
    }

    [Fact]
    public void Step_RejectsNullRuntime_WithParamNameRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeScheduledTickPipeline.Step(
                null!,
                Array.Empty<MatchScheduledSensorScanRequest>(),
                0));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Step_DelegatesNullScheduleValidation_WithParamNameScheduledScanRequests()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeScheduledTickPipeline.Step(runtime, null!, 0));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void Step_DelegatesNegativeNextIndex_WithParamNameNextIndex()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorRuntimeScheduledTickPipeline.Step(
                runtime,
                Array.Empty<MatchScheduledSensorScanRequest>(),
                -1));

        Assert.Equal("nextIndex", ex.ParamName);
    }

    [Fact]
    public void Step_ExhaustedSchedule_AdvancesMatchStateTick_PreservesNextIndex()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeScheduledTickOutcome outcome =
            MatchSensorRuntimeScheduledTickPipeline.Step(
                runtime,
                Array.Empty<MatchScheduledSensorScanRequest>(),
                nextIndex: 0);

        Assert.False(outcome.DidScan);
        Assert.Equal(new SimTick(11), outcome.UpdatedRuntime.State.CurrentTick);
        Assert.Equal(0, outcome.NextIndex);
    }

    [Fact]
    public void Step_NonMatchingScheduledTick_AdvancesMatchStateTick_PreservesNextIndex()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        IReadOnlyList<MatchScheduledSensorScanRequest> schedule =
            new[] { CreateScheduled(11, tankIndex: 0, sensorSlot: 0) };

        MatchSensorRuntimeScheduledTickOutcome outcome =
            MatchSensorRuntimeScheduledTickPipeline.Step(runtime, schedule, nextIndex: 0);

        Assert.False(outcome.DidScan);
        Assert.Equal(new SimTick(11), outcome.UpdatedRuntime.State.CurrentTick);
        Assert.Equal(0, outcome.NextIndex);
    }

    [Fact]
    public void Step_MatchingScheduledTick_ReturnsDidScanTrue_IncrementsNextIndex()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        IReadOnlyList<MatchScheduledSensorScanRequest> schedule =
            new[] { CreateScheduled(10, tankIndex: 0, sensorSlot: 0) };

        MatchSensorRuntimeScheduledTickOutcome outcome =
            MatchSensorRuntimeScheduledTickPipeline.Step(runtime, schedule, nextIndex: 0);

        Assert.True(outcome.DidScan);
        Assert.NotNull(outcome.ScanResult);
        Assert.Equal(1, outcome.NextIndex);
    }

    [Fact]
    public void Step_MatchingScheduledTick_UpdatesSensorWithPreStepTick()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        IReadOnlyList<MatchScheduledSensorScanRequest> schedule =
            new[] { CreateScheduled(10, tankIndex: 0, sensorSlot: 0) };

        MatchSensorRuntimeScheduledTickOutcome outcome =
            MatchSensorRuntimeScheduledTickPipeline.Step(runtime, schedule, nextIndex: 0);

        Assert.Equal(
            new SimTick(10),
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);
    }

    [Fact]
    public void Step_MatchingScheduledTick_AdvancesMatchStateTickAfterScan()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        IReadOnlyList<MatchScheduledSensorScanRequest> schedule =
            new[] { CreateScheduled(10, tankIndex: 0, sensorSlot: 0) };

        MatchSensorRuntimeScheduledTickOutcome outcome =
            MatchSensorRuntimeScheduledTickPipeline.Step(runtime, schedule, nextIndex: 0);

        Assert.Equal(new SimTick(11), outcome.UpdatedRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_InvalidSelectedScheduledRequest_Delegates_WithParamNameTankIndex()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        IReadOnlyList<MatchScheduledSensorScanRequest> schedule =
            new[] { CreateScheduled(10, tankIndex: 2, sensorSlot: 0) };

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorRuntimeScheduledTickPipeline.Step(runtime, schedule, nextIndex: 0));

        Assert.Equal("tankIndex", ex.ParamName);
    }
}
