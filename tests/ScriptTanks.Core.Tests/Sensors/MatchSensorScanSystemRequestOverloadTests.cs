using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

public sealed class MatchSensorScanSystemRequestOverloadTests
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

    /// <summary>
    /// Builds a request with an arbitrary tank index without going through
    /// <see cref="MatchSensorScanRequest"/>'s constructor (which rejects
    /// negatives) so <see cref="MatchSensorScanSystem.ScanSensorAtIndex"/>
    /// validation can be exercised.
    /// </summary>
    private static MatchSensorScanRequest RequestWithUncheckedTankIndex(
        int tankIndex,
        SensorSlot sensorSlot)
    {
        Span<byte> span = stackalloc byte[Unsafe.SizeOf<MatchSensorScanRequest>()];
        BitConverter.TryWriteBytes(span, tankIndex);
        BitConverter.TryWriteBytes(span.Slice(4), sensorSlot.Value);
        return MemoryMarshal.Read<MatchSensorScanRequest>(span);
    }

    [Fact]
    public void Scan_RejectsNullRuntime_WithParamNameRuntime()
    {
        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchSensorScanSystem.Scan(null!, request));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Scan_DelegatesNegativeTankIndexValidation_WithParamNameTankIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchSensorScanRequest request = RequestWithUncheckedTankIndex(
            -1,
            SensorSlot.Zero);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorScanSystem.Scan(runtime, request));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Scan_DelegatesPastEndTankIndexValidation_WithParamNameTankIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(2, SensorSlot.Zero);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorScanSystem.Scan(runtime, request));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Scan_DelegatesInvalidSensorSlotValidation_WithParamNameSlot()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, new SensorSlot(1));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchSensorScanSystem.Scan(runtime, request));

        Assert.Equal("slot", ex.ParamName);
    }

    [Fact]
    public void Scan_WhenReadyAndNoEnemies_ReturnsNoDetection()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.Scan(runtime, request);

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_WhenReadyAndEnemyInRange_ReturnsDetected()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.Scan(runtime, request);

        Assert.Equal(SensorScanStatus.Detected, outcome.Result.Status);
        Assert.Single(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_UpdatesSelectedSensorSlot()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorScanOutcome outcome = MatchSensorScanSystem.Scan(runtime, request);

        Assert.Equal(
            new SimTick(10),
            outcome.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero)
                .LastScanTick);
    }

    [Fact]
    public void Scan_ProducesEquivalentResultToScanSensorAtIndex_ForSameRuntimeRequest()
    {
        MatchState stateA = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtimeA = CreateRuntime(
            stateA,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchState stateB = CreateMatchState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtimeB = CreateRuntime(
            stateB,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);

        MatchSensorScanOutcome direct = MatchSensorScanSystem.ScanSensorAtIndex(
            runtimeA,
            request.TankIndex,
            request.SensorSlot);
        MatchSensorScanOutcome viaRequest = MatchSensorScanSystem.Scan(
            runtimeB,
            request);

        Assert.Equal(direct.Result.Status, viaRequest.Result.Status);
        Assert.Equal(direct.Result.DetectedTanks.Count, viaRequest.Result.DetectedTanks.Count);
        Assert.Equal(
            direct.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero),
            viaRequest.UpdatedRuntime.SensorLoadouts
                .GetLoadoutAtIndex(0)
                .GetSensor(SensorSlot.Zero));
    }
}
