using System;
using System.Collections.Generic;
using System.Linq;
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

public sealed class MatchReplayPlaybackCursorTests
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

    private static MatchReplayRecording CreateRecording(params int[] frameIndices)
        => new MatchReplayRecording(frameIndices.Select(i => CreateFrame(i)).ToArray());

    // -------------------- Validation --------------------

    [Fact]
    public void Constructor_RejectsNullRecording()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchReplayPlaybackCursor(null!, 0));
    }

    [Fact]
    public void Constructor_RejectsNegativePosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchReplayPlaybackCursor(recording, -1));
        Assert.Equal("position", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsPositionPastEnd()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchReplayPlaybackCursor(recording, recording.Frames.Count));
        Assert.Equal("position", ex.ParamName);
    }

    [Fact]
    public void Constructor_PreservesRecordingAndPosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);

        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 1);

        Assert.Same(recording, cursor.Recording);
        Assert.Equal(1, cursor.Position);
    }

    // -------------------- Position / frame inspection --------------------

    [Fact]
    public void Start_ReturnsFirstFrame()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);

        MatchReplayPlaybackCursor cursor = MatchReplayPlaybackCursor.Start(recording);

        Assert.Equal(0, cursor.Position);
        Assert.True(cursor.IsFirst);
        Assert.Same(recording.Frames[0], cursor.CurrentFrame);
    }

    [Fact]
    public void CurrentFrame_ReturnsFrameAtPosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);

        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 1);

        Assert.Same(recording.Frames[1], cursor.CurrentFrame);
        Assert.Equal(2, cursor.CurrentFrame.FrameIndex);
    }

    [Fact]
    public void IsFirstAndIsLast_ReflectPosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);

        MatchReplayPlaybackCursor first = new MatchReplayPlaybackCursor(recording, 0);
        Assert.True(first.IsFirst);
        Assert.False(first.IsLast);

        MatchReplayPlaybackCursor middle = new MatchReplayPlaybackCursor(recording, 1);
        Assert.False(middle.IsFirst);
        Assert.False(middle.IsLast);

        MatchReplayPlaybackCursor last = new MatchReplayPlaybackCursor(recording, 2);
        Assert.False(last.IsFirst);
        Assert.True(last.IsLast);
    }

    // -------------------- Navigation --------------------

    [Fact]
    public void MoveNext_AdvancesPosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 0);

        MatchReplayPlaybackCursor advanced = cursor.MoveNext();

        Assert.NotSame(cursor, advanced);
        Assert.Equal(1, advanced.Position);
        Assert.Same(recording.Frames[1], advanced.CurrentFrame);
    }

    [Fact]
    public void MoveNext_AtLast_ReturnsSameInstance()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 2);

        Assert.Same(cursor, cursor.MoveNext());
    }

    [Fact]
    public void MovePrevious_DecreasesPosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 1);

        MatchReplayPlaybackCursor previous = cursor.MovePrevious();

        Assert.NotSame(cursor, previous);
        Assert.Equal(0, previous.Position);
        Assert.Same(recording.Frames[0], previous.CurrentFrame);
    }

    [Fact]
    public void MovePrevious_AtFirst_ReturnsSameInstance()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 0);

        Assert.Same(cursor, cursor.MovePrevious());
    }

    [Fact]
    public void MoveLast_MovesToFinalFrame()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 0);

        MatchReplayPlaybackCursor moved = cursor.MoveLast();

        Assert.NotSame(cursor, moved);
        Assert.Equal(2, moved.Position);
        Assert.True(moved.IsLast);

        Assert.Same(moved, moved.MoveLast());
    }

    // -------------------- Seek --------------------

    [Fact]
    public void SeekToPosition_ReturnsRequestedPosition()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 0);

        MatchReplayPlaybackCursor seeked = cursor.SeekToPosition(2);

        Assert.NotSame(cursor, seeked);
        Assert.Equal(2, seeked.Position);
        Assert.Same(recording.Frames[2], seeked.CurrentFrame);
    }

    [Fact]
    public void SeekToFrameIndex_ReturnsMatchingFrame()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 0);

        MatchReplayPlaybackCursor seeked = cursor.SeekToFrameIndex(5);

        Assert.Equal(2, seeked.Position);
        Assert.Equal(5, seeked.CurrentFrame.FrameIndex);
        Assert.Same(recording.Frames[2], seeked.CurrentFrame);
    }

    [Fact]
    public void SeekToFrameIndex_RejectsMissingFrameIndex()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor cursor = new MatchReplayPlaybackCursor(recording, 0);

        Assert.Throws<KeyNotFoundException>(
            () => cursor.SeekToFrameIndex(7));
    }

    // -------------------- Equality --------------------

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        MatchReplayRecording recording = CreateRecording(0, 2, 5);
        MatchReplayPlaybackCursor a = new MatchReplayPlaybackCursor(recording, 1);
        MatchReplayPlaybackCursor b = new MatchReplayPlaybackCursor(recording, 1);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
