using System;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class FireVelocityResultTests
{
    private static FixedVec2 SampleVelocity() => FixedVec2.FromInts(1, 0);

    [Fact]
    public void Constructor_stores_Resolved_status_and_fire_velocity()
    {
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);

        FireVelocityResult result = new FireVelocityResult(
            FireVelocityStatus.Resolved,
            velocity);

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(velocity, result.FireVelocity);
        Assert.True(result.IsResolved);
    }

    [Fact]
    public void Constructor_stores_non_resolved_status_with_null_fire_velocity()
    {
        FireVelocityResult result = new FireVelocityResult(
            FireVelocityStatus.MissingAimDirection,
            fireVelocity: null);

        Assert.Equal(FireVelocityStatus.MissingAimDirection, result.Status);
        Assert.Null(result.FireVelocity);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        var bad = (FireVelocityStatus)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FireVelocityResult(bad, fireVelocity: null));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Resolved_requires_non_null_fire_velocity_ParamName_fireVelocity()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FireVelocityResult(
                FireVelocityStatus.Resolved,
                fireVelocity: null));

        Assert.Equal("fireVelocity", ex.ParamName);
    }

    [Theory]
    [InlineData(FireVelocityStatus.TankIndexOutOfRange)]
    [InlineData(FireVelocityStatus.TankDestroyed)]
    [InlineData(FireVelocityStatus.WeaponSlotMissing)]
    [InlineData(FireVelocityStatus.MissingAimDirection)]
    [InlineData(FireVelocityStatus.MissingWeaponSpeed)]
    public void Non_resolved_statuses_reject_non_null_fire_velocity_ParamName_fireVelocity(
        FireVelocityStatus status)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FireVelocityResult(status, SampleVelocity()));

        Assert.Equal("fireVelocity", ex.ParamName);
    }

    [Fact]
    public void IsResolved_is_true_for_Resolved_factory()
    {
        FireVelocityResult result = FireVelocityResult.Resolved(SampleVelocity());

        Assert.True(result.IsResolved);
    }

    [Theory]
    [InlineData(FireVelocityStatus.TankIndexOutOfRange)]
    [InlineData(FireVelocityStatus.TankDestroyed)]
    [InlineData(FireVelocityStatus.WeaponSlotMissing)]
    [InlineData(FireVelocityStatus.MissingAimDirection)]
    [InlineData(FireVelocityStatus.MissingWeaponSpeed)]
    public void IsResolved_is_false_for_non_resolved_statuses(FireVelocityStatus status)
    {
        FireVelocityResult result = status switch
        {
            FireVelocityStatus.TankIndexOutOfRange =>
                FireVelocityResult.TankIndexOutOfRange(),
            FireVelocityStatus.TankDestroyed =>
                FireVelocityResult.TankDestroyed(),
            FireVelocityStatus.WeaponSlotMissing =>
                FireVelocityResult.WeaponSlotMissing(),
            FireVelocityStatus.MissingAimDirection =>
                FireVelocityResult.MissingAimDirection(),
            FireVelocityStatus.MissingWeaponSpeed =>
                FireVelocityResult.MissingWeaponSpeed(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolved_factory_stores_fire_velocity()
    {
        FixedVec2 velocity = SampleVelocity();

        FireVelocityResult result = FireVelocityResult.Resolved(velocity);

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(velocity, result.FireVelocity);
    }

    [Fact]
    public void TankIndexOutOfRange_factory_creates_correct_status_with_null_fire_velocity()
    {
        FireVelocityResult result = FireVelocityResult.TankIndexOutOfRange();

        Assert.Equal(FireVelocityStatus.TankIndexOutOfRange, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void TankDestroyed_factory_creates_correct_status_with_null_fire_velocity()
    {
        FireVelocityResult result = FireVelocityResult.TankDestroyed();

        Assert.Equal(FireVelocityStatus.TankDestroyed, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void WeaponSlotMissing_factory_creates_correct_status_with_null_fire_velocity()
    {
        FireVelocityResult result = FireVelocityResult.WeaponSlotMissing();

        Assert.Equal(FireVelocityStatus.WeaponSlotMissing, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void MissingAimDirection_factory_creates_correct_status_with_null_fire_velocity()
    {
        FireVelocityResult result = FireVelocityResult.MissingAimDirection();

        Assert.Equal(FireVelocityStatus.MissingAimDirection, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void MissingWeaponSpeed_factory_creates_correct_status_with_null_fire_velocity()
    {
        FireVelocityResult result = FireVelocityResult.MissingWeaponSpeed();

        Assert.Equal(FireVelocityStatus.MissingWeaponSpeed, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void No_equality_override_uses_reference_equality()
    {
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);

        FireVelocityResult a = FireVelocityResult.Resolved(velocity);
        FireVelocityResult b = FireVelocityResult.Resolved(velocity);

        Assert.NotEqual(a, b);
        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
    }
}
