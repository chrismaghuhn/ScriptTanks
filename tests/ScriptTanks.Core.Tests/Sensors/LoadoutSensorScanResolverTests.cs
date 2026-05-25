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

public sealed class LoadoutSensorScanResolverTests
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

    private static MatchState CreateState(SimTick tick, params TankState[] tanks)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            Enumerable.Range(0, tanks.Length)
                .Select(_ => CreateWeaponLoadout())
                .ToArray(),
            Array.Empty<ProjectileState>());
    }

    private static TankSensorLoadout CreateSensorLoadout()
    {
        return new TankSensorLoadout(new[]
        {
            SensorState.Ready(SensorCatalog.BasicRadar),
            SensorState.Ready(SensorCatalog.WideScanner),
        });
    }

    [Fact]
    public void Outcome_Constructor_RejectsNullResult()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoadoutSensorScanOutcome(null!, CreateSensorLoadout()));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Outcome_Constructor_RejectsNullUpdatedLoadout()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoadoutSensorScanOutcome(result, null!));

        Assert.Equal("updatedLoadout", ex.ParamName);
    }

    [Fact]
    public void Outcome_Constructor_PreservesReferences()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));
        TankSensorLoadout loadout = CreateSensorLoadout();

        LoadoutSensorScanOutcome outcome = new LoadoutSensorScanOutcome(
            result,
            loadout);

        Assert.Same(result, outcome.Result);
        Assert.Same(loadout, outcome.UpdatedLoadout);
    }

    [Fact]
    public void Outcome_Equals_UsesReferenceEquality()
    {
        SensorScanResult resultA = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));
        SensorScanResult resultB = SensorScanResult.NoDetection(
            new SimTick(10),
            SensorCatalog.BasicRadar,
            new TankId(0));
        TankSensorLoadout loadout = CreateSensorLoadout();

        LoadoutSensorScanOutcome a = new LoadoutSensorScanOutcome(resultA, loadout);
        LoadoutSensorScanOutcome b = new LoadoutSensorScanOutcome(resultB, loadout);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Scan_RejectsNullState()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => LoadoutSensorScanResolver.Scan(
                null!,
                new TankId(0),
                CreateSensorLoadout(),
                SensorSlot.Zero));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void Scan_RejectsNullLoadout()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => LoadoutSensorScanResolver.Scan(
                state,
                new TankId(0),
                null!,
                SensorSlot.Zero));

        Assert.Equal("loadout", ex.ParamName);
    }

    [Fact]
    public void Scan_RejectsInvalidSlotThroughLoadout()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        TankSensorLoadout loadout = CreateSensorLoadout();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => LoadoutSensorScanResolver.Scan(
                state,
                new TankId(0),
                loadout,
                new SensorSlot(2)));

        Assert.Equal("slot", ex.ParamName);
    }

    [Fact]
    public void Scan_DelegatesMissingScannerTankToSensorScanResolver()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));

        Assert.Throws<InvalidOperationException>(
            () => LoadoutSensorScanResolver.Scan(
                state,
                new TankId(99),
                CreateSensorLoadout(),
                SensorSlot.Zero));
    }

    [Fact]
    public void Scan_WhenReadyAndNoEnemies_ReturnsNoDetection()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));

        LoadoutSensorScanOutcome outcome = LoadoutSensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorLoadout(),
            SensorSlot.Zero);

        Assert.Equal(SensorScanStatus.NoDetection, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
    }

    [Fact]
    public void Scan_WhenReadyAndEnemyInRange_ReturnsDetected()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));

        LoadoutSensorScanOutcome outcome = LoadoutSensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorLoadout(),
            SensorSlot.Zero);

        Assert.Equal(SensorScanStatus.Detected, outcome.Result.Status);
        DetectedTankSnapshot detected = Assert.Single(outcome.Result.DetectedTanks);
        Assert.Equal(new TankId(1), detected.TankId);
        Assert.Equal(Fixed.FromInt(5), detected.Distance);
    }

    [Fact]
    public void Scan_MarksOnlySelectedSensorAsScanned()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));

        LoadoutSensorScanOutcome outcome = LoadoutSensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorLoadout(),
            SensorSlot.Zero);

        Assert.Equal(
            new SimTick(10),
            outcome.UpdatedLoadout.GetSensor(SensorSlot.Zero).LastScanTick);
    }

    [Fact]
    public void Scan_PreservesOtherSensorSlotsUnchanged()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));

        LoadoutSensorScanOutcome outcome = LoadoutSensorScanResolver.Scan(
            state,
            new TankId(0),
            CreateSensorLoadout(),
            SensorSlot.Zero);

        Assert.Equal(
            SensorState.Ready(SensorCatalog.WideScanner),
            outcome.UpdatedLoadout.GetSensor(new SensorSlot(1)));
    }

    [Fact]
    public void Scan_DoesNotMutateOriginalLoadout()
    {
        MatchState state = CreateState(
            new SimTick(10),
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        TankSensorLoadout originalLoadout = CreateSensorLoadout();

        _ = LoadoutSensorScanResolver.Scan(
            state,
            new TankId(0),
            originalLoadout,
            SensorSlot.Zero);

        Assert.Equal(
            SensorState.Ready(SensorCatalog.BasicRadar),
            originalLoadout.GetSensor(SensorSlot.Zero));
        Assert.Equal(
            SensorState.Ready(SensorCatalog.WideScanner),
            originalLoadout.GetSensor(new SensorSlot(1)));
    }

    [Fact]
    public void Scan_WhenSensorOnCooldown_ReturnsCooldownAndKeepsSelectedSensorUnchanged()
    {
        SensorState cooldownSensor = SensorState
            .Ready(SensorCatalog.BasicRadar)
            .MarkScanned(SimTick.Zero);
        TankSensorLoadout loadout = new TankSensorLoadout(new[]
        {
            cooldownSensor,
            SensorState.Ready(SensorCatalog.WideScanner),
        });
        MatchState state = CreateState(
            SimTick.Zero,
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));

        LoadoutSensorScanOutcome outcome = LoadoutSensorScanResolver.Scan(
            state,
            new TankId(0),
            loadout,
            SensorSlot.Zero);

        Assert.Equal(SensorScanStatus.SensorOnCooldown, outcome.Result.Status);
        Assert.Empty(outcome.Result.DetectedTanks);
        Assert.Equal(
            cooldownSensor,
            outcome.UpdatedLoadout.GetSensor(SensorSlot.Zero));
    }
}
