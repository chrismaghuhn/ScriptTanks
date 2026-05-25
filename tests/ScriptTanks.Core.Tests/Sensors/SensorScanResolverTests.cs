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

public sealed class SensorScanResolverTests
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

    private static TankWeaponLoadout CreateLoadout()
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });
    }

    private static MatchState CreateState(
        SimTick? tick = null,
        TankState[]? tanks = null)
    {
        TankState[] stateTanks = tanks
            ?? new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(3, 4)),
            };

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick ?? new SimTick(10),
            stateTanks,
            Enumerable.Range(0, stateTanks.Length)
                .Select(_ => CreateLoadout())
                .ToArray(),
            Array.Empty<ProjectileState>());
    }

    private static SensorDefinition CreateSensorDefinition(
        Fixed? range = null,
        int cooldownTicks = 5)
    {
        return new SensorDefinition(
            "test_sensor",
            "Test Sensor",
            range ?? Fixed.FromInt(10),
            Fixed.FromInt(360),
            cooldownTicks,
            cpuCost: 1);
    }

    private static SensorState CreateSensorState(
        Fixed? range = null,
        int cooldownTicks = 5,
        SimTick? lastScanTick = null)
    {
        return new SensorState(
            CreateSensorDefinition(range, cooldownTicks),
            lastScanTick ?? new SimTick(0));
    }

    [Fact]
    public void Scan_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => SensorScanResolver.Scan(
                null!,
                new TankId(0),
                CreateSensorState()));
    }

    [Fact]
    public void Scan_RejectsUnknownScannerTank()
    {
        MatchState state = CreateState();

        Assert.Throws<InvalidOperationException>(
            () => SensorScanResolver.Scan(
                state,
                new TankId(99),
                CreateSensorState()));
    }

    [Fact]
    public void Scan_WhenSensorOnCooldown_ReturnsOnCooldownAndDoesNotUpdateState()
    {
        MatchState state = CreateState(new SimTick(10));
        SensorState sensorState = CreateSensorState(lastScanTick: new SimTick(8));

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            sensorState);

        Assert.Equal(SensorScanStatus.SensorOnCooldown, outcome.Result.Status);
        Assert.False(outcome.Result.HasDetections);
        Assert.Empty(outcome.Result.DetectedTanks);
        Assert.Equal(sensorState, outcome.UpdatedSensorState);
    }

    [Fact]
    public void Scan_WhenReadyAndNoEnemiesInRange_ReturnsNoDetectionAndMarksScanned()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(50, 50)),
            });
        SensorState sensorState = CreateSensorState(range: Fixed.FromInt(10));

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            sensorState);

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.False(outcome.Result.HasDetections);
        Assert.Empty(outcome.Result.DetectedTanks);
        Assert.Equal(new SimTick(10), outcome.UpdatedSensorState.LastScanTick);
    }

    [Fact]
    public void Scan_WhenEnemyInRange_ReturnsDetectedAndMarksScanned()
    {
        MatchState state = CreateState();
        SensorState sensorState = CreateSensorState(range: Fixed.FromInt(10));

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            sensorState);

        Assert.Equal(SensorScanStatus.Detected, outcome.Result.Status);
        DetectedTankSnapshot detected = Assert.Single(outcome.Result.DetectedTanks);
        Assert.Equal(new TankId(1), detected.TankId);
        Assert.Equal(new PlayerSlot(1), detected.OwnerSlot);
        Assert.Equal(FixedVec2.FromInts(3, 4), detected.Position);
        Assert.Equal(Fixed.FromInt(5), detected.Distance);
        Assert.Equal(new SimTick(10), outcome.UpdatedSensorState.LastScanTick);
    }

    [Fact]
    public void Scan_ResultMetadataUsesCurrentTickSensorAndScannerTank()
    {
        MatchState state = CreateState(new SimTick(42));
        SensorState sensorState = CreateSensorState(range: Fixed.FromInt(10));

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            sensorState);

        Assert.Equal(new SimTick(42), outcome.Result.Tick);
        Assert.Equal(sensorState.Definition, outcome.Result.Sensor);
        Assert.Equal(new TankId(0), outcome.Result.ScannerTankId);
    }

    [Fact]
    public void Scan_IgnoresScannerItself()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(100)));

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_IgnoresSameOwnerSlotTanks()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 0, FixedVec2.FromInts(3, 4)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(100)));

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_IgnoresDestroyedEnemyTanks()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(3, 4), hp: 0),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(100)));

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_IncludesEnemyExactlyOnRangeBoundary()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(3, 4)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(5)));

        DetectedTankSnapshot detected = Assert.Single(outcome.Result.DetectedTanks);
        Assert.Equal(new TankId(1), detected.TankId);
        Assert.Equal(Fixed.FromInt(5), detected.Distance);
    }

    [Fact]
    public void Scan_ExcludesEnemyOutsideRange()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(6, 8)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(5)));

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_PreservesTankOrderForDetections()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(2, 1, FixedVec2.FromInts(6, 8)),
                CreateTank(1, 1, FixedVec2.FromInts(3, 4)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(20)));

        Assert.Equal(2, outcome.Result.DetectedTanks.Count);
        Assert.Equal(new TankId(2), outcome.Result.DetectedTanks[0].TankId);
        Assert.Equal(new TankId(1), outcome.Result.DetectedTanks[1].TankId);
    }

    [Fact]
    public void Scan_ReturnsMultipleDetections()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(3, 4)),
                CreateTank(2, 1, FixedVec2.FromInts(6, 8)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(20)));

        Assert.Equal(SensorScanStatus.Detected, outcome.Result.Status);
        Assert.Equal(2, outcome.Result.DetectedTanks.Count);
        Assert.True(outcome.Result.HasDetections);
    }

    [Fact]
    public void Scan_ReportsZeroDistance_WhenEnemySharesPosition()
    {
        MatchState state = CreateState(
            new SimTick(10),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
                CreateTank(1, 1, FixedVec2.FromInts(0, 0)),
            });

        SensorScanOutcome outcome = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(1)));

        DetectedTankSnapshot detected = Assert.Single(outcome.Result.DetectedTanks);
        Assert.Equal(Fixed.Zero, detected.Distance);
    }

    [Fact]
    public void SensorScanOutcome_RejectsNullResult()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SensorScanOutcome(null!, CreateSensorState()));
    }

    [Fact]
    public void SensorScanOutcome_PreservesValues()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(10),
            CreateSensorDefinition(),
            new TankId(0));
        SensorState sensorState = CreateSensorState();

        SensorScanOutcome outcome = new SensorScanOutcome(result, sensorState);

        Assert.Same(result, outcome.Result);
        Assert.Equal(sensorState, outcome.UpdatedSensorState);
    }

    [Fact]
    public void Scan_DoesNotMutateOriginalState()
    {
        MatchState state = CreateState();
        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        TankWeaponLoadout[] originalLoadouts = state.Loadouts.ToArray();
        int originalProjectileCount = state.Projectiles.Count;

        _ = SensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorState(range: Fixed.FromInt(10)));

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTanks, state.Tanks);
        Assert.Equal(originalLoadouts, state.Loadouts);
        Assert.Equal(originalProjectileCount, state.Projectiles.Count);
    }
}
