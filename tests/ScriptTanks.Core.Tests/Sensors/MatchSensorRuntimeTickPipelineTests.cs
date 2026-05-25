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

public sealed class MatchSensorRuntimeTickPipelineTests
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

    [Fact]
    public void Outcome_RejectsNullUpdatedRuntime_WithParamNameUpdatedRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeTickOutcome(false, null, null!));

        Assert.Equal("updatedRuntime", ex.ParamName);
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
            () => new MatchSensorRuntimeTickOutcome(true, null, runtime));

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

        var outcome = new MatchSensorRuntimeTickOutcome(false, null, runtime);

        Assert.False(outcome.DidScan);
        Assert.Null(outcome.ScanResult);
        Assert.Same(runtime, outcome.UpdatedRuntime);
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

        var outcome = new MatchSensorRuntimeTickOutcome(true, scanResult, runtime);

        Assert.True(outcome.DidScan);
        Assert.Same(scanResult, outcome.ScanResult);
        Assert.Same(runtime, outcome.UpdatedRuntime);
    }

    [Fact]
    public void Step_RejectsNullRuntime_WithParamNameRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeTickPipeline.Step(
                null!,
                new MatchSensorScanRequest(0, SensorSlot.Zero)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Step_NullScanRequest_AdvancesMatchStateTickByOne()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickOutcome outcome =
            MatchSensorRuntimeTickPipeline.Step(runtime, scanRequest: null);

        Assert.Equal(new SimTick(11), outcome.UpdatedRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_NullScanRequest_ReturnsDidScanFalseAndNullScanResult()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickOutcome outcome =
            MatchSensorRuntimeTickPipeline.Step(runtime, scanRequest: null);

        Assert.False(outcome.DidScan);
        Assert.Null(outcome.ScanResult);
    }

    [Fact]
    public void Step_NullScanRequest_PreservesSensorLoadoutsReference()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorLoadoutState loadoutsBefore = runtime.SensorLoadouts;

        MatchSensorRuntimeTickOutcome outcome =
            MatchSensorRuntimeTickPipeline.Step(runtime, scanRequest: null);

        Assert.Same(loadoutsBefore, outcome.UpdatedRuntime.SensorLoadouts);
    }

    [Fact]
    public void Step_ValidScanRequest_ReturnsDidScanTrueAndNonNullScanResult()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorRuntimeTickOutcome outcome =
            MatchSensorRuntimeTickPipeline.Step(runtime, request);

        Assert.True(outcome.DidScan);
        Assert.NotNull(outcome.ScanResult);
    }

    [Fact]
    public void Step_ValidScanRequest_UpdatesSensorSlot_LastScanTickIsPreStepTick_StateTickAdvances()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorRuntimeTickOutcome outcome =
            MatchSensorRuntimeTickPipeline.Step(runtime, request);

        Assert.Equal(
            new SimTick(10),
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);

        Assert.Equal(new SimTick(11), outcome.UpdatedRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_PreservesOriginalRuntimePurity()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchState stateBefore = runtime.State;
        MatchSensorLoadoutState loadoutsBefore = runtime.SensorLoadouts;

        _ = MatchSensorRuntimeTickPipeline.Step(runtime, scanRequest: null);

        Assert.Same(stateBefore, runtime.State);
        Assert.Same(loadoutsBefore, runtime.SensorLoadouts);
    }

    [Fact]
    public void Step_InvalidScanRequest_DelegatesValidation_WithParamNameTankIndex()
    {
        MatchState matchState = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(2, SensorSlot.Zero);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorRuntimeTickPipeline.Step(runtime, request));

        Assert.Equal("tankIndex", ex.ParamName);
    }
}
