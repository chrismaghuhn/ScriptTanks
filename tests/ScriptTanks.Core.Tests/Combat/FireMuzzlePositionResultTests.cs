using System;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class FireMuzzlePositionResultTests
{
    private static FixedVec2 SamplePosition() => FixedVec2.FromInts(10, 20);

    [Fact]
    public void Constructor_stores_Resolved_status_and_muzzle_position()
    {
        FixedVec2 position = FixedVec2.FromInts(1, 2);

        FireMuzzlePositionResult result = new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.Resolved,
            position);

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(position, result.MuzzlePosition);
        Assert.True(result.IsResolved);
    }

    [Fact]
    public void Constructor_stores_non_resolved_status_with_null_muzzle_position()
    {
        FireMuzzlePositionResult result = new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.MissingAimDirection,
            muzzlePosition: null);

        Assert.Equal(FireMuzzlePositionStatus.MissingAimDirection, result.Status);
        Assert.Null(result.MuzzlePosition);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        var bad = (FireMuzzlePositionStatus)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FireMuzzlePositionResult(bad, muzzlePosition: null));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Resolved_requires_non_null_muzzle_position_ParamName_muzzlePosition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FireMuzzlePositionResult(
                FireMuzzlePositionStatus.Resolved,
                muzzlePosition: null));

        Assert.Equal("muzzlePosition", ex.ParamName);
    }

    [Theory]
    [InlineData(FireMuzzlePositionStatus.TankIndexOutOfRange)]
    [InlineData(FireMuzzlePositionStatus.TankDestroyed)]
    [InlineData(FireMuzzlePositionStatus.WeaponSlotMissing)]
    [InlineData(FireMuzzlePositionStatus.MissingAimDirection)]
    [InlineData(FireMuzzlePositionStatus.MissingWeaponGeometry)]
    public void Non_resolved_statuses_reject_non_null_muzzle_position_ParamName_muzzlePosition(
        FireMuzzlePositionStatus status)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FireMuzzlePositionResult(status, SamplePosition()));

        Assert.Equal("muzzlePosition", ex.ParamName);
    }

    [Fact]
    public void IsResolved_is_true_for_Resolved_factory()
    {
        FireMuzzlePositionResult result = FireMuzzlePositionResult.Resolved(SamplePosition());

        Assert.True(result.IsResolved);
    }

    [Theory]
    [InlineData(FireMuzzlePositionStatus.TankIndexOutOfRange)]
    [InlineData(FireMuzzlePositionStatus.TankDestroyed)]
    [InlineData(FireMuzzlePositionStatus.WeaponSlotMissing)]
    [InlineData(FireMuzzlePositionStatus.MissingAimDirection)]
    [InlineData(FireMuzzlePositionStatus.MissingWeaponGeometry)]
    public void IsResolved_is_false_for_non_resolved_statuses(FireMuzzlePositionStatus status)
    {
        FireMuzzlePositionResult result = status switch
        {
            FireMuzzlePositionStatus.TankIndexOutOfRange =>
                FireMuzzlePositionResult.TankIndexOutOfRange(),
            FireMuzzlePositionStatus.TankDestroyed =>
                FireMuzzlePositionResult.TankDestroyed(),
            FireMuzzlePositionStatus.WeaponSlotMissing =>
                FireMuzzlePositionResult.WeaponSlotMissing(),
            FireMuzzlePositionStatus.MissingAimDirection =>
                FireMuzzlePositionResult.MissingAimDirection(),
            FireMuzzlePositionStatus.MissingWeaponGeometry =>
                FireMuzzlePositionResult.MissingWeaponGeometry(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolved_factory_stores_position()
    {
        FixedVec2 position = SamplePosition();

        FireMuzzlePositionResult result = FireMuzzlePositionResult.Resolved(position);

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(position, result.MuzzlePosition);
    }

    [Fact]
    public void TankIndexOutOfRange_factory_creates_correct_status_with_null_position()
    {
        FireMuzzlePositionResult result = FireMuzzlePositionResult.TankIndexOutOfRange();

        Assert.Equal(FireMuzzlePositionStatus.TankIndexOutOfRange, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void TankDestroyed_factory_creates_correct_status_with_null_position()
    {
        FireMuzzlePositionResult result = FireMuzzlePositionResult.TankDestroyed();

        Assert.Equal(FireMuzzlePositionStatus.TankDestroyed, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void WeaponSlotMissing_factory_creates_correct_status_with_null_position()
    {
        FireMuzzlePositionResult result = FireMuzzlePositionResult.WeaponSlotMissing();

        Assert.Equal(FireMuzzlePositionStatus.WeaponSlotMissing, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void MissingAimDirection_factory_creates_correct_status_with_null_position()
    {
        FireMuzzlePositionResult result = FireMuzzlePositionResult.MissingAimDirection();

        Assert.Equal(FireMuzzlePositionStatus.MissingAimDirection, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void MissingWeaponGeometry_factory_creates_correct_status_with_null_position()
    {
        FireMuzzlePositionResult result = FireMuzzlePositionResult.MissingWeaponGeometry();

        Assert.Equal(FireMuzzlePositionStatus.MissingWeaponGeometry, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void No_equality_override_uses_reference_equality()
    {
        FixedVec2 position = FixedVec2.FromInts(1, 2);

        FireMuzzlePositionResult a = FireMuzzlePositionResult.Resolved(position);
        FireMuzzlePositionResult b = FireMuzzlePositionResult.Resolved(position);

        Assert.NotEqual(a, b);
        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
    }
}
