using System;
using System.Reflection;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankMovementRecordTests
{
    private static MovementState CreateMovement(FixedVec2 position, FixedVec2 velocity)
        => new MovementState(position, velocity);

    private static MovementState SampleMovement()
        => CreateMovement(FixedVec2.FromInts(10, 20), FixedVec2.FromInts(1, 0));

    [Fact]
    public void Constructor_preserves_values()
    {
        TankId tankId = new TankId(3);
        MovementState initial = SampleMovement();
        MovementState final = CreateMovement(FixedVec2.FromInts(11, 20), FixedVec2.FromInts(1, 0));

        MatchStateTankMovementRecord record = MatchStateTankMovementRecord.Moved(
            2,
            tankId,
            initial,
            final);

        Assert.Equal(2, record.TankIndex);
        Assert.Equal(tankId, record.TankId);
        Assert.Equal(MatchStateTankMovementStatus.Moved, record.Status);
        Assert.Equal(initial, record.InitialMovement);
        Assert.Equal(final, record.FinalMovement);
        Assert.True(record.DidMove);
    }

    [Fact]
    public void Constructor_negative_tank_index_throws()
    {
        MovementState movement = SampleMovement();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchStateTankMovementRecord(
                -1,
                new TankId(0),
                MatchStateTankMovementStatus.StayedStill,
                movement,
                movement));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_undefined_status_throws()
    {
        MovementState movement = SampleMovement();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchStateTankMovementRecord(
                0,
                new TankId(0),
                (MatchStateTankMovementStatus)99,
                movement,
                movement));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_skipped_destroyed_requires_same_movement_values()
    {
        MovementState initial = SampleMovement();
        MovementState final = CreateMovement(FixedVec2.FromInts(11, 20), FixedVec2.FromInts(1, 0));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementRecord(
                0,
                new TankId(0),
                MatchStateTankMovementStatus.SkippedDestroyed,
                initial,
                final));

        Assert.Equal("finalMovement", ex.ParamName);
    }

    [Fact]
    public void Constructor_stayed_still_requires_same_movement_values()
    {
        MovementState initial = SampleMovement();
        MovementState final = CreateMovement(FixedVec2.FromInts(11, 20), FixedVec2.FromInts(1, 0));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementRecord(
                0,
                new TankId(0),
                MatchStateTankMovementStatus.StayedStill,
                initial,
                final));

        Assert.Equal("finalMovement", ex.ParamName);
    }

    [Fact]
    public void Constructor_moved_requires_different_movement_values()
    {
        MovementState movement = SampleMovement();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementRecord(
                0,
                new TankId(0),
                MatchStateTankMovementStatus.Moved,
                movement,
                movement));

        Assert.Equal("finalMovement", ex.ParamName);
    }

    [Fact]
    public void SkippedDestroyed_factory_sets_status_and_DidMove_false()
    {
        MovementState movement = SampleMovement();

        MatchStateTankMovementRecord record = MatchStateTankMovementRecord.SkippedDestroyed(
            1,
            new TankId(1),
            movement);

        Assert.Equal(MatchStateTankMovementStatus.SkippedDestroyed, record.Status);
        Assert.False(record.DidMove);
        Assert.Equal(movement, record.InitialMovement);
        Assert.Equal(movement, record.FinalMovement);
    }

    [Fact]
    public void StayedStill_factory_sets_status_and_DidMove_false()
    {
        MovementState movement = SampleMovement();

        MatchStateTankMovementRecord record = MatchStateTankMovementRecord.StayedStill(
            0,
            new TankId(0),
            movement);

        Assert.Equal(MatchStateTankMovementStatus.StayedStill, record.Status);
        Assert.False(record.DidMove);
        Assert.Equal(movement, record.InitialMovement);
        Assert.Equal(movement, record.FinalMovement);
    }

    [Fact]
    public void Moved_factory_sets_status_and_DidMove_true()
    {
        MovementState initial = SampleMovement();
        MovementState final = CreateMovement(FixedVec2.FromInts(11, 20), FixedVec2.FromInts(1, 0));

        MatchStateTankMovementRecord record = MatchStateTankMovementRecord.Moved(
            0,
            new TankId(0),
            initial,
            final);

        Assert.Equal(MatchStateTankMovementStatus.Moved, record.Status);
        Assert.True(record.DidMove);
        Assert.Equal(initial, record.InitialMovement);
        Assert.Equal(final, record.FinalMovement);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equalsMethod = typeof(MatchStateTankMovementRecord).GetMethod(
            nameof(object.Equals),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equalsMethod);
        Assert.Equal(typeof(object), equalsMethod!.DeclaringType);
    }
}
