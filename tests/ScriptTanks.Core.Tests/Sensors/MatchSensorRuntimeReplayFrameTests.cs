using System;
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

public sealed class MatchSensorRuntimeReplayFrameTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
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

    private static MatchSensorRuntimeState CreateRuntime()
    {
        TankState tankA = CreateTank(0, 0);
        TankState tankB = CreateTank(1, 1);

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            SimTick.Zero,
            new[] { tankA, tankB },
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            Array.Empty<ProjectileState>());

        MatchSensorLoadoutState sensorLoadouts = new MatchSensorLoadoutState(new[]
        {
            new TankSensorLoadout(new[]
            {
                SensorState.Ready(SensorCatalog.BasicRadar),
            }),
            new TankSensorLoadout(new[]
            {
                SensorState.Ready(SensorCatalog.BasicRadar),
            }),
        });

        return new MatchSensorRuntimeState(state, sensorLoadouts);
    }

    [Fact]
    public void Constructor_PreservesFrameIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var frame = new MatchSensorRuntimeReplayFrame(
            frameIndex: 42,
            state: runtime);

        Assert.Equal(42, frame.FrameIndex);
    }

    [Fact]
    public void Constructor_PreservesStateReference()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var frame = new MatchSensorRuntimeReplayFrame(
            frameIndex: 0,
            state: runtime);

        Assert.Same(runtime, frame.State);
    }

    [Fact]
    public void Constructor_RejectsNegativeFrameIndex_WithParamNameFrameIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeReplayFrame(
                frameIndex: -1,
                state: runtime));

        Assert.Equal("frameIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullState_WithParamNameState()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeReplayFrame(
                frameIndex: 0,
                state: null!));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsFrameIndexZero()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var frame = new MatchSensorRuntimeReplayFrame(
            frameIndex: 0,
            state: runtime);

        Assert.Equal(0, frame.FrameIndex);
    }

    [Fact]
    public void Instances_AreDistinctAndUseDefaultReferenceEquality()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        var first = new MatchSensorRuntimeReplayFrame(
            frameIndex: 3,
            state: runtime);

        var second = new MatchSensorRuntimeReplayFrame(
            frameIndex: 3,
            state: runtime);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }
}
