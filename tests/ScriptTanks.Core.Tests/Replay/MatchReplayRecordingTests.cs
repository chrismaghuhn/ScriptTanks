using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Replay;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Replay;

public sealed class MatchReplayRecordingTests
{
    private static MatchState CreateState()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        TankWeaponLoadout loadout0 = CreateLoadout();
        TankWeaponLoadout loadout1 = CreateLoadout();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(0),
            new[] { tank0, tank1 },
            new[] { loadout0, loadout1 },
            Array.Empty<ProjectileState>());
    }

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position)
    {
        MovementState movement = new MovementState(position, FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static MatchReplayFrame CreateFrame(int frameIndex)
        => new MatchReplayFrame(frameIndex, CreateState());

    [Fact]
    public void Constructor_PreservesFramesInOrder()
    {
        MatchReplayFrame f0 = CreateFrame(0);
        MatchReplayFrame f1 = CreateFrame(2);
        MatchReplayFrame f2 = CreateFrame(5);

        MatchReplayRecording recording = new MatchReplayRecording(
            new[] { f0, f1, f2 });

        Assert.Equal(3, recording.Frames.Count);
        Assert.Same(f0, recording.Frames[0]);
        Assert.Same(f1, recording.Frames[1]);
        Assert.Same(f2, recording.Frames[2]);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesInputArray()
    {
        MatchReplayFrame f0 = CreateFrame(0);
        MatchReplayFrame f1 = CreateFrame(1);
        MatchReplayFrame[] input = new[] { f0, f1 };

        MatchReplayRecording recording = new MatchReplayRecording(input);

        input[1] = CreateFrame(99);

        Assert.Equal(2, recording.Frames.Count);
        Assert.Same(f0, recording.Frames[0]);
        Assert.Same(f1, recording.Frames[1]);
        Assert.Equal(1, recording.Frames[1].FrameIndex);
    }

    [Fact]
    public void Constructor_RejectsNullFrames()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchReplayRecording(null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyFrames()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchReplayRecording(Array.Empty<MatchReplayFrame>()));
        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullFrameEntry()
    {
        MatchReplayFrame[] input = new[] { CreateFrame(0), null! };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchReplayRecording(input));
        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsDuplicateFrameIndex()
    {
        MatchReplayFrame[] input = new[]
        {
            CreateFrame(0),
            CreateFrame(1),
            CreateFrame(1),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchReplayRecording(input));
        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNonIncreasingFrameIndex()
    {
        MatchReplayFrame[] input = new[]
        {
            CreateFrame(0),
            CreateFrame(2),
            CreateFrame(1),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchReplayRecording(input));
        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void FramesCollection_IsReadOnly()
    {
        MatchReplayRecording recording = new MatchReplayRecording(
            new[] { CreateFrame(0), CreateFrame(1) });

        IList<MatchReplayFrame> list = (IList<MatchReplayFrame>)recording.Frames;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateFrame(99)));
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchReplayFrame f0 = CreateFrame(0);
        MatchReplayFrame f1 = CreateFrame(1);

        MatchReplayRecording a = new MatchReplayRecording(new[] { f0, f1 });
        MatchReplayRecording b = new MatchReplayRecording(new[] { f0, f1 });

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
