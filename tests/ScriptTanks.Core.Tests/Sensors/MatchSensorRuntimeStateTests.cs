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

public sealed class MatchSensorRuntimeStateTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(
                FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
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

    private static MatchState CreateMatchState(int tankCount)
    {
        TankState[] tanks = Enumerable.Range(0, tankCount)
            .Select(i => CreateTank(i, i))
            .ToArray();
        TankWeaponLoadout[] loadouts = Enumerable.Range(0, tankCount)
            .Select(_ => CreateWeaponLoadout())
            .ToArray();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            SimTick.Zero,
            tanks,
            loadouts,
            Array.Empty<ProjectileState>());
    }

    private static TankSensorLoadout CreateLoadout(SensorDefinition sensor)
    {
        return new TankSensorLoadout(new[] { SensorState.Ready(sensor) });
    }

    private static MatchSensorLoadoutState CreateSensorLoadouts(int count)
    {
        TankSensorLoadout[] loadouts = new TankSensorLoadout[count];
        for (int i = 0; i < count; i++)
        {
            loadouts[i] = CreateLoadout(
                i % 2 == 0
                    ? SensorCatalog.BasicRadar
                    : SensorCatalog.WideScanner);
        }

        return new MatchSensorLoadoutState(loadouts);
    }

    [Fact]
    public void Constructor_RejectsNullState()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeState(null!, CreateSensorLoadouts(2)));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullSensorLoadouts()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeState(CreateMatchState(2), null!));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsCountMismatch()
    {
        MatchState state = CreateMatchState(2);
        MatchSensorLoadoutState loadouts = CreateSensorLoadouts(3);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorRuntimeState(state, loadouts));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void Constructor_PreservesReferences()
    {
        MatchState state = CreateMatchState(2);
        MatchSensorLoadoutState loadouts = CreateSensorLoadouts(2);

        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            state,
            loadouts);

        Assert.Same(state, runtime.State);
        Assert.Same(loadouts, runtime.SensorLoadouts);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchState state = CreateMatchState(2);
        MatchSensorLoadoutState loadouts = CreateSensorLoadouts(2);

        MatchSensorRuntimeState a = new MatchSensorRuntimeState(state, loadouts);
        MatchSensorRuntimeState b = new MatchSensorRuntimeState(state, loadouts);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void WithState_RejectsNullState()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => runtime.WithState(null!));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void WithState_RejectsCountMismatch()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => runtime.WithState(CreateMatchState(3)));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void WithState_ReplacesStateAndPreservesSensorLoadouts()
    {
        MatchState originalState = CreateMatchState(2);
        MatchSensorLoadoutState originalLoadouts = CreateSensorLoadouts(2);
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            originalState,
            originalLoadouts);
        MatchState newState = CreateMatchState(2);

        MatchSensorRuntimeState updated = runtime.WithState(newState);

        Assert.Same(newState, updated.State);
        Assert.Same(originalLoadouts, updated.SensorLoadouts);
    }

    [Fact]
    public void WithSensorLoadouts_RejectsNullSensorLoadouts()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => runtime.WithSensorLoadouts(null!));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void WithSensorLoadouts_RejectsCountMismatch()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => runtime.WithSensorLoadouts(CreateSensorLoadouts(3)));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void WithSensorLoadouts_ReplacesSensorLoadoutsAndPreservesState()
    {
        MatchState originalState = CreateMatchState(2);
        MatchSensorLoadoutState originalLoadouts = CreateSensorLoadouts(2);
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            originalState,
            originalLoadouts);
        MatchSensorLoadoutState newLoadouts = CreateSensorLoadouts(2);

        MatchSensorRuntimeState updated = runtime.WithSensorLoadouts(newLoadouts);

        Assert.Same(originalState, updated.State);
        Assert.Same(newLoadouts, updated.SensorLoadouts);
    }

    [Fact]
    public void WithTankSensorLoadoutAtIndex_RejectsNullReplacement()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => runtime.WithTankSensorLoadoutAtIndex(0, null!));

        Assert.Equal("sensorLoadout", ex.ParamName);
    }

    [Fact]
    public void WithTankSensorLoadoutAtIndex_RejectsInvalidIndex()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => runtime.WithTankSensorLoadoutAtIndex(2, replacement));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void WithTankSensorLoadoutAtIndex_ReplacesSelectedLoadout()
    {
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            CreateMatchState(2),
            CreateSensorLoadouts(2));
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        MatchSensorRuntimeState updated = runtime.WithTankSensorLoadoutAtIndex(
            1,
            replacement);

        Assert.Same(replacement, updated.SensorLoadouts.GetLoadoutAtIndex(1));
    }

    [Fact]
    public void WithTankSensorLoadoutAtIndex_PreservesStateReference()
    {
        MatchState originalState = CreateMatchState(2);
        MatchSensorRuntimeState runtime = new MatchSensorRuntimeState(
            originalState,
            CreateSensorLoadouts(2));
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        MatchSensorRuntimeState updated = runtime.WithTankSensorLoadoutAtIndex(
            1,
            replacement);

        Assert.Same(originalState, updated.State);
    }

    [Fact]
    public void WithTankSensorLoadoutAtIndex_DoesNotMutateOriginalRuntimeState()
    {
        MatchSensorLoadoutState originalLoadoutsState = CreateSensorLoadouts(2);
        TankSensorLoadout originalSlot1 = originalLoadoutsState.GetLoadoutAtIndex(1);
        MatchSensorRuntimeState original = new MatchSensorRuntimeState(
            CreateMatchState(2),
            originalLoadoutsState);
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        _ = original.WithTankSensorLoadoutAtIndex(1, replacement);

        Assert.Same(originalSlot1, original.SensorLoadouts.GetLoadoutAtIndex(1));
        Assert.Same(originalLoadoutsState, original.SensorLoadouts);
    }
}
