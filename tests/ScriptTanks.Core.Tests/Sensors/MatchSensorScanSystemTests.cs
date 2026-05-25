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

public sealed class MatchSensorScanSystemTests
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
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            tanks.Select(_ => CreateWeaponLoadout()).ToArray(),
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
    public void Outcome_Constructor_RejectsNullResult()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorScanOutcome(null!, runtime));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Outcome_Constructor_RejectsNullUpdatedRuntime()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorScanOutcome(result, null!));

        Assert.Equal("updatedRuntime", ex.ParamName);
    }

    [Fact]
    public void Outcome_Constructor_PreservesReferences()
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

        MatchSensorScanOutcome outcome = new MatchSensorScanOutcome(
            result,
            runtime);

        Assert.Same(result, outcome.Result);
        Assert.Same(runtime, outcome.UpdatedRuntime);
    }

    [Fact]
    public void Outcome_Equals_UsesReferenceEquality()
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

        MatchSensorScanOutcome a = new MatchSensorScanOutcome(result, runtime);
        MatchSensorScanOutcome b = new MatchSensorScanOutcome(result, runtime);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void ScanSensorAtIndex_RejectsNullRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorScanSystem.ScanSensorAtIndex(
                null!,
                0,
                SensorSlot.Zero));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void ScanSensorAtIndex_RejectsNegativeTankIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorScanSystem.ScanSensorAtIndex(
                runtime,
                -1,
                SensorSlot.Zero));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void ScanSensorAtIndex_RejectsPastEndTankIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorScanSystem.ScanSensorAtIndex(
                runtime,
                2,
                SensorSlot.Zero));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void ScanSensorAtIndex_RejectsInvalidSensorSlotThroughLoadoutValidation()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorScanSystem.ScanSensorAtIndex(
                runtime,
                0,
                new SensorSlot(1)));

        Assert.Equal("slot", ex.ParamName);
    }

    [Fact]
    public void ScanSensorAtIndex_WhenReadyAndNoEnemies_ReturnsNoDetection()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void ScanSensorAtIndex_WhenReadyAndEnemyInRange_ReturnsDetected()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Equal(SensorScanStatus.Detected, outcome.Result.Status);
    }

    [Fact]
    public void ScanSensorAtIndex_DetectedSnapshotUsesEnemyData()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        DetectedTankSnapshot detected = Assert.Single(outcome.Result.DetectedTanks);
        Assert.Equal(new TankId(1), detected.TankId);
        Assert.Equal(new PlayerSlot(1), detected.OwnerSlot);
        Assert.Equal(FixedVec2.FromInts(3, 4), detected.Position);
        Assert.Equal(Fixed.FromInt(5), detected.Distance);
    }

    [Fact]
    public void ScanSensorAtIndex_UpdatesSelectedTanksSelectedSensorSlot()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Equal(
            new SimTick(10),
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);
    }

    [Fact]
    public void ScanSensorAtIndex_PreservesSelectedTanksOtherSensorSlots()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(
                SensorState.Ready(SensorCatalog.BasicRadar),
                SensorState.Ready(SensorCatalog.WideScanner)));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Equal(
            SensorState.Ready(SensorCatalog.WideScanner),
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(new SensorSlot(1)));
    }

    [Fact]
    public void ScanSensorAtIndex_PreservesOtherTankSensorLoadouts()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        TankSensorLoadout originalTank1Loadout = CreateSensorLoadout(
            SensorState.Ready(SensorCatalog.WideScanner));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            originalTank1Loadout);

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Same(
            originalTank1Loadout,
            outcome.UpdatedRuntime.SensorLoadouts.GetLoadoutAtIndex(1));
    }

    [Fact]
    public void ScanSensorAtIndex_DoesNotMutateOriginalRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState original = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchState originalState = original.State;
        MatchSensorLoadoutState originalLoadoutsState = original.SensorLoadouts;
        SensorState originalScannerSensor = original.SensorLoadouts
            .GetLoadoutAtIndex(0)
            .GetSensor(SensorSlot.Zero);

        _ = MatchSensorScanSystem.ScanSensorAtIndex(
            original,
            0,
            SensorSlot.Zero);

        Assert.Same(originalState, original.State);
        Assert.Same(originalLoadoutsState, original.SensorLoadouts);
        Assert.Equal(
            originalScannerSensor,
            original.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero));
    }

    [Fact]
    public void ScanSensorAtIndex_PreservesUnderlyingMatchStateReferenceInUpdatedRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Same(state, outcome.UpdatedRuntime.State);
    }

    [Fact]
    public void ScanSensorAtIndex_WhenSensorOnCooldown_ReturnsSensorOnCooldown()
    {
        SensorState cooldownSensor = SensorState
            .Ready(SensorCatalog.BasicRadar)
            .MarkScanned(SimTick.Zero);
        MatchState state = CreateMatchState(
            SimTick.Zero,
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(cooldownSensor));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Equal(SensorScanStatus.SensorOnCooldown, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void ScanSensorAtIndex_WhenSensorOnCooldown_KeepsSelectedSensorValueUnchanged()
    {
        SensorState cooldownSensor = SensorState
            .Ready(SensorCatalog.BasicRadar)
            .MarkScanned(SimTick.Zero);
        MatchState state = CreateMatchState(
            SimTick.Zero,
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(cooldownSensor));

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.ScanSensorAtIndex(
            runtime,
            0,
            SensorSlot.Zero);

        Assert.Equal(
            cooldownSensor,
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero));
    }
}
