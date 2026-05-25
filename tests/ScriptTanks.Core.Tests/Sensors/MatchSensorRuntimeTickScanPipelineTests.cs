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

public sealed class MatchSensorRuntimeTickScanPipelineTests
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
    public void NoScan_RejectsNullRuntime_WithParamNameRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeTickScanOutcome.NoScan(null!));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void NoScan_ReturnsDidScanFalse()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanOutcome.NoScan(runtime);

        Assert.False(outcome.DidScan);
    }

    [Fact]
    public void NoScan_ReturnsNullResult()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanOutcome.NoScan(runtime);

        Assert.Null(outcome.Result);
    }

    [Fact]
    public void NoScan_PreservesRuntimeReference()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanOutcome.NoScan(runtime);

        Assert.Same(runtime, outcome.UpdatedRuntime);
    }

    [Fact]
    public void Scanned_RejectsNullResult_WithParamNameResult()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeTickScanOutcome.Scanned(null!, runtime));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Scanned_RejectsNullUpdatedRuntime_WithParamNameUpdatedRuntime()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeTickScanOutcome.Scanned(result, null!));

        Assert.Equal("updatedRuntime", ex.ParamName);
    }

    [Fact]
    public void Scanned_PreservesReferences_AndSetsDidScanTrue()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanOutcome.Scanned(result, runtime);

        Assert.True(outcome.DidScan);
        Assert.Same(result, outcome.Result);
        Assert.Same(runtime, outcome.UpdatedRuntime);
    }

    [Fact]
    public void ApplyOptionalScan_RejectsNullRuntime_WithParamNameRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(
                null!,
                new MatchSensorScanRequest(0, SensorSlot.Zero)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void ApplyOptionalScan_NullRequest_ReturnsNoScanOutcome()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(
                runtime,
                scanRequest: null);

        Assert.False(outcome.DidScan);
        Assert.Null(outcome.Result);
    }

    [Fact]
    public void ApplyOptionalScan_NullRequest_PreservesRuntimeReference()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(
                runtime,
                scanRequest: null);

        Assert.Same(runtime, outcome.UpdatedRuntime);
    }

    [Fact]
    public void ApplyOptionalScan_ValidRequest_ReadySensorNoEnemies_ReturnsDidScanAndNoDetection()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(runtime, request);

        Assert.True(outcome.DidScan);
        Assert.NotNull(outcome.Result);
        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result!.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void ApplyOptionalScan_ValidRequest_EnemyInRange_ReturnsDidScanAndDetected()
    {
        TankState scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        MatchState matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        MatchSensorRuntimeState runtime = CreateRuntime(
            matchState,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(runtime, request);

        Assert.True(outcome.DidScan);
        Assert.NotNull(outcome.Result);
        Assert.Equal(SensorScanStatus.Detected, outcome.Result!.Status);
        Assert.Single(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void ApplyOptionalScan_ValidRequest_UpdatesSelectedSensorSlot()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(runtime, request);

        Assert.Equal(
            new SimTick(10),
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);
    }

    [Fact]
    public void ApplyOptionalScan_ValidRequest_PreservesMatchStateReferenceInUpdatedRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorRuntimeTickScanOutcome outcome =
            MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(runtime, request);

        Assert.Same(state, outcome.UpdatedRuntime.State);
    }

    [Fact]
    public void ApplyOptionalScan_InvalidTankIndex_IsDelegated_WithParamNameTankIndex()
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
            () => MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(runtime, request));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void ApplyOptionalScan_InvalidSensorSlot_IsDelegated_WithParamNameSlot()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, new SensorSlot(1));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(runtime, request));

        Assert.Equal("slot", ex.ParamName);
    }
}
