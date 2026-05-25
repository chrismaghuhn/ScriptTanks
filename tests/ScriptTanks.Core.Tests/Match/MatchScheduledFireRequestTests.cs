using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchScheduledFireRequestTests
{
    private static MatchFireRequest CreateRequest(
        int shooterId = 0,
        int weaponSlot = 0,
        int projectileId = 123)
        => new MatchFireRequest(
            shooterTankId: new TankId(shooterId),
            weaponSlot: new WeaponSlot(weaponSlot),
            projectileId: new ProjectileId(projectileId),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

    private static MatchScheduledFireRequest CreateBaseScheduled()
        => new MatchScheduledFireRequest(new SimTick(5), CreateRequest());

    [Fact]
    public void Constructor_PreservesValues()
    {
        SimTick tick = new SimTick(7);
        MatchFireRequest request = CreateRequest(shooterId: 2, weaponSlot: 1, projectileId: 42);

        MatchScheduledFireRequest scheduled = new MatchScheduledFireRequest(tick, request);

        Assert.Equal(tick, scheduled.Tick);
        Assert.Equal(request, scheduled.Request);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameValues()
    {
        MatchScheduledFireRequest a = CreateBaseScheduled();
        MatchScheduledFireRequest b = CreateBaseScheduled();

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenTickDiffers()
    {
        MatchFireRequest request = CreateRequest();
        MatchScheduledFireRequest a = new MatchScheduledFireRequest(new SimTick(5), request);
        MatchScheduledFireRequest b = new MatchScheduledFireRequest(new SimTick(6), request);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenRequestDiffers()
    {
        SimTick tick = new SimTick(5);
        MatchScheduledFireRequest a = new MatchScheduledFireRequest(tick, CreateRequest(shooterId: 0));
        MatchScheduledFireRequest b = new MatchScheduledFireRequest(tick, CreateRequest(shooterId: 1));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Operators_MatchEquals()
    {
        MatchScheduledFireRequest a = CreateBaseScheduled();
        MatchScheduledFireRequest aDup = CreateBaseScheduled();
        MatchScheduledFireRequest b = new MatchScheduledFireRequest(
            new SimTick(6),
            CreateRequest());

        Assert.True(a == aDup);
        Assert.False(a != aDup);
        Assert.True(a != b);
        Assert.False(a == b);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualRequests()
    {
        MatchScheduledFireRequest a = CreateBaseScheduled();
        MatchScheduledFireRequest b = CreateBaseScheduled();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsSmokeSafeString()
    {
        MatchScheduledFireRequest scheduled = CreateBaseScheduled();

        string text = scheduled.ToString();

        Assert.False(string.IsNullOrEmpty(text));
        Assert.Contains("MatchScheduledFireRequest", text);
        Assert.Contains("Tick", text);
    }
}
