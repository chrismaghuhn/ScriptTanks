using System;
using System.Reflection;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankBoundsRecordTests
{
    private static FixedVec2 SamplePosition()
        => FixedVec2.FromInts(10, 20);

    [Fact]
    public void Constructor_preserves_values()
    {
        TankId tankId = new TankId(3);
        FixedVec2 initial = FixedVec2.FromInts(-1, 20);
        FixedVec2 final = FixedVec2.FromInts(2, 20);

        MatchStateTankBoundsRecord record = MatchStateTankBoundsRecord.Clamped(
            2,
            tankId,
            initial,
            final);

        Assert.Equal(2, record.TankIndex);
        Assert.Equal(tankId, record.TankId);
        Assert.Equal(MatchStateTankBoundsStatus.Clamped, record.Status);
        Assert.Equal(initial, record.InitialPosition);
        Assert.Equal(final, record.FinalPosition);
        Assert.True(record.DidClamp);
    }

    [Fact]
    public void Constructor_negative_tank_index_throws()
    {
        FixedVec2 position = SamplePosition();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchStateTankBoundsRecord(
                -1,
                new TankId(0),
                MatchStateTankBoundsStatus.InsideBounds,
                position,
                position));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_undefined_status_throws()
    {
        FixedVec2 position = SamplePosition();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchStateTankBoundsRecord(
                0,
                new TankId(0),
                (MatchStateTankBoundsStatus)99,
                position,
                position));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_skipped_destroyed_requires_same_position()
    {
        FixedVec2 initial = SamplePosition();
        FixedVec2 final = FixedVec2.FromInts(11, 20);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankBoundsRecord(
                0,
                new TankId(0),
                MatchStateTankBoundsStatus.SkippedDestroyed,
                initial,
                final));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void Constructor_inside_bounds_requires_same_position()
    {
        FixedVec2 initial = SamplePosition();
        FixedVec2 final = FixedVec2.FromInts(11, 20);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankBoundsRecord(
                0,
                new TankId(0),
                MatchStateTankBoundsStatus.InsideBounds,
                initial,
                final));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void Constructor_clamped_requires_different_position()
    {
        FixedVec2 position = SamplePosition();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankBoundsRecord(
                0,
                new TankId(0),
                MatchStateTankBoundsStatus.Clamped,
                position,
                position));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void SkippedDestroyed_factory_sets_status_and_DidClamp_false()
    {
        FixedVec2 position = SamplePosition();

        MatchStateTankBoundsRecord record = MatchStateTankBoundsRecord.SkippedDestroyed(
            1,
            new TankId(1),
            position);

        Assert.Equal(MatchStateTankBoundsStatus.SkippedDestroyed, record.Status);
        Assert.False(record.DidClamp);
        Assert.Equal(position, record.InitialPosition);
        Assert.Equal(position, record.FinalPosition);
    }

    [Fact]
    public void InsideBounds_factory_sets_status_and_DidClamp_false()
    {
        FixedVec2 position = SamplePosition();

        MatchStateTankBoundsRecord record = MatchStateTankBoundsRecord.InsideBounds(
            0,
            new TankId(0),
            position);

        Assert.Equal(MatchStateTankBoundsStatus.InsideBounds, record.Status);
        Assert.False(record.DidClamp);
        Assert.Equal(position, record.InitialPosition);
        Assert.Equal(position, record.FinalPosition);
    }

    [Fact]
    public void Clamped_factory_sets_status_and_DidClamp_true()
    {
        FixedVec2 initial = FixedVec2.FromInts(-1, 20);
        FixedVec2 final = FixedVec2.FromInts(2, 20);

        MatchStateTankBoundsRecord record = MatchStateTankBoundsRecord.Clamped(
            0,
            new TankId(0),
            initial,
            final);

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, record.Status);
        Assert.True(record.DidClamp);
        Assert.Equal(initial, record.InitialPosition);
        Assert.Equal(final, record.FinalPosition);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equalsMethod = typeof(MatchStateTankBoundsRecord).GetMethod(
            nameof(object.Equals),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equalsMethod);
        Assert.Equal(typeof(object), equalsMethod!.DeclaringType);
    }
}
