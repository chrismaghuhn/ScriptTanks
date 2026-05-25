using System;
using System.Collections.Generic;
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

public sealed class MatchSensorRuntimeRunWithReplayResultTests
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

    private static MatchEndConditionResult CreateEndCondition(
        MatchSensorRuntimeState runtime)
    {
        return MatchEndConditionEvaluator.Evaluate(
            runtime.State,
            maxTicks: 100);
    }

    private static MatchSensorRuntimeReplayFrame CreateFrame(
        int frameIndex,
        MatchSensorRuntimeState? state = null)
    {
        return new MatchSensorRuntimeReplayFrame(
            frameIndex,
            state ?? CreateRuntime());
    }

    private static MatchSensorRuntimeRunWithReplayResult CreateResult(
        MatchSensorRuntimeReplayFrame[] frames)
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);
        return new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 0,
            nextScheduledScanIndex: 0,
            endCondition,
            frames);
    }

    [Fact]
    public void Constructor_RejectsNullFinalRuntime_WithParamNameFinalRuntime()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                null!,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition,
                new[] { CreateFrame(0, runtime) }));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeTicksExecuted_WithParamNameTicksExecuted()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                runtime,
                ticksExecuted: -1,
                nextScheduledScanIndex: 0,
                endCondition,
                new[] { CreateFrame(0, runtime) }));

        Assert.Equal("ticksExecuted", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeNextScheduledScanIndex_WithParamNameNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: -1,
                endCondition,
                new[] { CreateFrame(0, runtime) }));

        Assert.Equal("nextScheduledScanIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullEndCondition_WithParamNameEndCondition()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition: null!,
                new[] { CreateFrame(0, runtime) }));

        Assert.Equal("endCondition", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullReplayFrames_WithParamNameReplayFrames()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition,
                replayFrames: null!));

        Assert.Equal("replayFrames", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyReplayFrames_WithParamNameReplayFrames()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition,
                Array.Empty<MatchSensorRuntimeReplayFrame>()));

        Assert.Equal("replayFrames", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullReplayFrameElement_WithParamNameReplayFrames()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);
        MatchSensorRuntimeReplayFrame frame0 = CreateFrame(0, runtime);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorRuntimeRunWithReplayResult(
                runtime,
                ticksExecuted: 0,
                nextScheduledScanIndex: 0,
                endCondition,
                new MatchSensorRuntimeReplayFrame[] { frame0, null! }));

        Assert.Equal("replayFrames", ex.ParamName);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesReplayFramesInputArray()
    {
        MatchSensorRuntimeReplayFrame frame0 = CreateFrame(0);
        MatchSensorRuntimeReplayFrame frame1 = CreateFrame(1);
        MatchSensorRuntimeReplayFrame replacement = CreateFrame(2);

        MatchSensorRuntimeReplayFrame[] frames = { frame0, frame1 };

        MatchSensorRuntimeRunWithReplayResult result = CreateResult(frames);

        frames[1] = replacement;

        Assert.Same(frame1, result.ReplayFrames[1]);
    }

    [Fact]
    public void ReplayFramesCollection_IsReadOnly()
    {
        MatchSensorRuntimeRunWithReplayResult result = CreateResult(
            new[]
            {
                CreateFrame(0),
                CreateFrame(1),
            });

        IList<MatchSensorRuntimeReplayFrame> list =
            Assert.IsAssignableFrom<IList<MatchSensorRuntimeReplayFrame>>(result.ReplayFrames);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateFrame(2)));
    }

    [Fact]
    public void Constructor_PreservesFinalRuntimeReference()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1,
            endCondition,
            new[] { CreateFrame(0, runtime), CreateFrame(1, runtime) });

        Assert.Same(runtime, result.FinalRuntime);
    }

    [Fact]
    public void Constructor_PreservesEndConditionReference()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 2,
            nextScheduledScanIndex: 0,
            endCondition,
            new[] { CreateFrame(0, runtime) });

        Assert.Same(endCondition, result.EndCondition);
    }

    [Fact]
    public void Constructor_PreservesTicksExecutedAndNextScheduledScanIndex()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);

        var result = new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 9,
            nextScheduledScanIndex: 7,
            endCondition,
            new[] { CreateFrame(0, runtime) });

        Assert.Equal(9, result.TicksExecuted);
        Assert.Equal(7, result.NextScheduledScanIndex);
    }

    [Fact]
    public void Constructor_PreservesReplayFrameOrder()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);
        MatchSensorRuntimeReplayFrame frame0 = CreateFrame(0, runtime);
        MatchSensorRuntimeReplayFrame frame1 = CreateFrame(1, runtime);

        var result = new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1,
            endCondition,
            new[] { frame0, frame1 });

        Assert.Same(frame0, result.ReplayFrames[0]);
        Assert.Same(frame1, result.ReplayFrames[1]);
        Assert.Equal(0, result.ReplayFrames[0].FrameIndex);
        Assert.Equal(1, result.ReplayFrames[1].FrameIndex);
    }

    [Fact]
    public void Instances_AreDistinctAndUseDefaultReferenceEquality()
    {
        MatchSensorRuntimeState runtime = CreateRuntime();
        MatchEndConditionResult endCondition = CreateEndCondition(runtime);
        MatchSensorRuntimeReplayFrame[] frames =
        {
            CreateFrame(0, runtime),
        };

        var first = new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1,
            endCondition,
            frames);

        var second = new MatchSensorRuntimeRunWithReplayResult(
            runtime,
            ticksExecuted: 3,
            nextScheduledScanIndex: 1,
            endCondition,
            frames);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }
}
