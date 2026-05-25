using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchFireRequestTests
{
    private static MatchFireRequest CreateBaseRequest()
        => new MatchFireRequest(
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

    private static MatchFireRequest WithShooter(MatchFireRequest baseRequest, TankId shooterTankId)
        => new MatchFireRequest(
            shooterTankId,
            baseRequest.WeaponSlot,
            baseRequest.ProjectileId,
            baseRequest.MuzzlePosition,
            baseRequest.FireVelocity);

    private static MatchFireRequest WithWeaponSlot(MatchFireRequest baseRequest, WeaponSlot weaponSlot)
        => new MatchFireRequest(
            baseRequest.ShooterTankId,
            weaponSlot,
            baseRequest.ProjectileId,
            baseRequest.MuzzlePosition,
            baseRequest.FireVelocity);

    private static MatchFireRequest WithProjectileId(MatchFireRequest baseRequest, ProjectileId projectileId)
        => new MatchFireRequest(
            baseRequest.ShooterTankId,
            baseRequest.WeaponSlot,
            projectileId,
            baseRequest.MuzzlePosition,
            baseRequest.FireVelocity);

    private static MatchFireRequest WithMuzzlePosition(MatchFireRequest baseRequest, FixedVec2 muzzlePosition)
        => new MatchFireRequest(
            baseRequest.ShooterTankId,
            baseRequest.WeaponSlot,
            baseRequest.ProjectileId,
            muzzlePosition,
            baseRequest.FireVelocity);

    private static MatchFireRequest WithFireVelocity(MatchFireRequest baseRequest, FixedVec2 fireVelocity)
        => new MatchFireRequest(
            baseRequest.ShooterTankId,
            baseRequest.WeaponSlot,
            baseRequest.ProjectileId,
            baseRequest.MuzzlePosition,
            fireVelocity);

    [Fact]
    public void Constructor_PreservesValues()
    {
        TankId shooter = new TankId(7);
        WeaponSlot slot = new WeaponSlot(2);
        ProjectileId projectile = new ProjectileId(42);
        FixedVec2 muzzle = FixedVec2.FromInts(11, 22);
        FixedVec2 velocity = new FixedVec2(Fixed.FromInt(3), Fixed.FromInt(-1));

        MatchFireRequest request = new MatchFireRequest(
            shooter,
            slot,
            projectile,
            muzzle,
            velocity);

        Assert.Equal(shooter, request.ShooterTankId);
        Assert.Equal(slot, request.WeaponSlot);
        Assert.Equal(projectile, request.ProjectileId);
        Assert.Equal(muzzle, request.MuzzlePosition);
        Assert.Equal(velocity, request.FireVelocity);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameValues()
    {
        MatchFireRequest a = CreateBaseRequest();
        MatchFireRequest b = CreateBaseRequest();

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenShooterDiffers()
    {
        MatchFireRequest baseRequest = CreateBaseRequest();
        MatchFireRequest other = WithShooter(baseRequest, new TankId(1));

        Assert.False(baseRequest.Equals(other));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenWeaponSlotDiffers()
    {
        MatchFireRequest baseRequest = CreateBaseRequest();
        MatchFireRequest other = WithWeaponSlot(baseRequest, new WeaponSlot(1));

        Assert.False(baseRequest.Equals(other));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenProjectileIdDiffers()
    {
        MatchFireRequest baseRequest = CreateBaseRequest();
        MatchFireRequest other = WithProjectileId(baseRequest, new ProjectileId(124));

        Assert.False(baseRequest.Equals(other));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenMuzzlePositionDiffers()
    {
        MatchFireRequest baseRequest = CreateBaseRequest();
        MatchFireRequest other = WithMuzzlePosition(baseRequest, FixedVec2.FromInts(11, 20));

        Assert.False(baseRequest.Equals(other));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenFireVelocityDiffers()
    {
        MatchFireRequest baseRequest = CreateBaseRequest();
        MatchFireRequest other = WithFireVelocity(
            baseRequest,
            new FixedVec2(Fixed.FromInt(2), Fixed.Zero));

        Assert.False(baseRequest.Equals(other));
    }

    [Fact]
    public void Operators_MatchEquals()
    {
        MatchFireRequest a = CreateBaseRequest();
        MatchFireRequest aDup = CreateBaseRequest();
        MatchFireRequest b = WithShooter(a, new TankId(1));

        Assert.True(a == aDup);
        Assert.False(a != aDup);
        Assert.True(a != b);
        Assert.False(a == b);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualRequests()
    {
        MatchFireRequest a = CreateBaseRequest();
        MatchFireRequest b = CreateBaseRequest();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsSmokeSafeString()
    {
        MatchFireRequest request = CreateBaseRequest();

        string text = request.ToString();

        Assert.False(string.IsNullOrEmpty(text));
        Assert.Contains("MatchFireRequest", text);
        Assert.Contains("ShooterTankId", text);
    }
}
