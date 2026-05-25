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

public sealed class MatchSensorRuntimeScheduledScanPipelineTests
{
    private static TankState CreateTank(int id, int ownerSlot, FixedVec2? position = null, int? hp = null)
    {
        return new TankState(new TankId(id), new PlayerSlot(ownerSlot), TankCatalog.BasicTank,
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints, bodyRotation: Fixed.Zero, turretRotation: Fixed.Zero);
    }
    private static TankWeaponLoadout CreateWeaponLoadout() =>
        new TankWeaponLoadout(new[] { WeaponState.Ready(WeaponCatalog.StandardCannon) });
    private static MatchState CreateMatchState(SimTick tick, params TankState[] tanks) =>
        new MatchState(ArenaCatalog.OpenTestArena, tick, tanks, tanks.Select(_ => CreateWeaponLoadout()).ToArray(), Array.Empty<ProjectileState>());
    private static TankSensorLoadout CreateSensorLoadout(SensorState first, SensorState? second = null) =>
        second.HasValue ? new TankSensorLoadout(new[] { first, second.Value }) : new TankSensorLoadout(new[] { first });
    private static MatchSensorRuntimeState CreateRuntime(MatchState state, params TankSensorLoadout[] sensorLoadouts) =>
        new MatchSensorRuntimeState(state, new MatchSensorLoadoutState(sensorLoadouts));
    private static MatchScheduledSensorScanRequest CreateScheduled(int tick, int tankIndex = 0, int sensorSlot = 0) =>
        new MatchScheduledSensorScanRequest(new SimTick(tick), new MatchSensorScanRequest(tankIndex, new SensorSlot(sensorSlot)));

    [Fact] public void Outcome_RejectsNullUpdatedRuntime_WithParamNameUpdatedRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MatchSensorRuntimeScheduledScanOutcome(false, null, null!, 0));
        Assert.Equal("updatedRuntime", ex.ParamName);
    }
    [Fact] public void Outcome_RejectsNegativeNextIndex_WithParamNameNextIndex()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSensorRuntimeScheduledScanOutcome(false, null, runtime, -1));
        Assert.Equal("nextIndex", ex.ParamName);
    }
    [Fact] public void Outcome_RejectsDidScanTrueWithNullResult_WithParamNameResult()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MatchSensorRuntimeScheduledScanOutcome(true, null, runtime, 0));
        Assert.Equal("result", ex.ParamName);
    }
    [Fact] public void Outcome_AllowsDidScanFalseWithNullResult()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = new MatchSensorRuntimeScheduledScanOutcome(false, null, runtime, 2);
        Assert.False(outcome.DidScan); Assert.Null(outcome.Result); Assert.Equal(2, outcome.NextIndex);
    }
    [Fact] public void Outcome_PreservesValuesAndReferences()
    {
        var result = SensorScanResult.NoDetection(new SimTick(10), SensorCatalog.BasicRadar, new TankId(0));
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = new MatchSensorRuntimeScheduledScanOutcome(true, result, runtime, 5);
        Assert.True(outcome.DidScan); Assert.Same(result, outcome.Result); Assert.Same(runtime, outcome.UpdatedRuntime); Assert.Equal(5, outcome.NextIndex);
    }

    [Fact] public void Pipeline_RejectsNullRuntime_WithParamNameRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(null!, Array.Empty<MatchScheduledSensorScanRequest>(), 0));
        Assert.Equal("runtime", ex.ParamName);
    }
    [Fact] public void Pipeline_DelegatesNullScheduleValidation_WithParamNameScheduledScanRequests()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, null!, 0));
        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }
    [Fact] public void Pipeline_DelegatesNegativeNextIndex_WithParamNameNextIndex()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, Array.Empty<MatchScheduledSensorScanRequest>(), -1));
        Assert.Equal("nextIndex", ex.ParamName);
    }
    [Fact] public void Pipeline_ExhaustedSchedule_ReturnsDidScanFalse()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, Array.Empty<MatchScheduledSensorScanRequest>(), 0);
        Assert.False(outcome.DidScan); Assert.Null(outcome.Result);
    }
    [Fact] public void Pipeline_ExhaustedSchedule_PreservesRuntimeReference()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, Array.Empty<MatchScheduledSensorScanRequest>(), 0);
        Assert.Same(runtime, outcome.UpdatedRuntime);
    }
    [Fact] public void Pipeline_ExhaustedSchedule_PreservesNextIndex()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, Array.Empty<MatchScheduledSensorScanRequest>(), 3);
        Assert.Equal(3, outcome.NextIndex);
    }
    [Fact] public void Pipeline_EarlierScheduledTick_ReturnsDidScanFalse_PreservesNextIndex()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, new[] { CreateScheduled(9) }, 0);
        Assert.False(outcome.DidScan); Assert.Equal(0, outcome.NextIndex);
    }
    [Fact] public void Pipeline_LaterScheduledTick_ReturnsDidScanFalse_PreservesNextIndex()
    {
        var runtime = CreateRuntime(CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0))), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, new[] { CreateScheduled(11) }, 0);
        Assert.False(outcome.DidScan); Assert.Equal(0, outcome.NextIndex);
    }

    [Fact] public void Pipeline_MatchingCurrentTick_ReturnsDidScanTrue()
    {
        var scanner = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        var enemy = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        var matchState = CreateMatchState(new SimTick(10), scanner, enemy);
        var runtime = CreateRuntime(matchState, CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, new[] { CreateScheduled(10, 0, 0) }, 0);
        Assert.True(outcome.DidScan); Assert.NotNull(outcome.Result);
    }
    [Fact] public void Pipeline_MatchingCurrentTick_IncrementsNextIndexByOne()
    {
        var matchState = CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0)), CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        var runtime = CreateRuntime(matchState, CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        MatchScheduledSensorScanRequest[] schedule =
        {
            CreateScheduled(1), CreateScheduled(2), CreateScheduled(3), CreateScheduled(4),
            CreateScheduled(10, 0, 0),
        };
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, schedule, 4);
        Assert.Equal(5, outcome.NextIndex);
    }
    [Fact] public void Pipeline_MatchingCurrentTick_UpdatesSelectedSensorSlot()
    {
        var matchState = CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0)), CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        var runtime = CreateRuntime(matchState, CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, new[] { CreateScheduled(10, 0, 0) }, 0);
        Assert.Equal(new SimTick(10), outcome.UpdatedRuntime.SensorLoadouts.GetLoadoutAtIndex(0).GetSensor(SensorSlot.Zero).LastScanTick);
    }
    [Fact] public void Pipeline_MatchingCurrentTick_PreservesUnderlyingMatchStateReference()
    {
        var matchState = CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0)), CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        var runtime = CreateRuntime(matchState, CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        var outcome = MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, new[] { CreateScheduled(10, 0, 0) }, 0);
        Assert.Same(matchState, outcome.UpdatedRuntime.State);
    }
    [Fact] public void Pipeline_InvalidSelectedRequest_DelegatesScanValidation_WithParamNameTankIndex()
    {
        var matchState = CreateMatchState(new SimTick(10), CreateTank(0, 0, FixedVec2.FromInts(0, 0)), CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        var runtime = CreateRuntime(matchState, CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)), CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => MatchSensorRuntimeScheduledScanPipeline.ApplyScheduledScanForCurrentTick(runtime, new[] { CreateScheduled(10, 2, 0) }, 0));
        Assert.Equal("tankIndex", ex.ParamName);
    }
}
