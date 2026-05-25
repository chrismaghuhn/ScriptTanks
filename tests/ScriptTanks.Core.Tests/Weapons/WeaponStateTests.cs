using System;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Weapons;

public sealed class WeaponStateTests
{
    private static WeaponState CreateFiredState(
        WeaponDefinition? definition = null,
        int lastFireTick = 10)
    {
        return new WeaponState(
            definition ?? WeaponCatalog.StandardCannon,
            new SimTick(lastFireTick),
            hasFired: true);
    }

    private static WeaponDefinition CreateStandardCannonClone()
    {
        return new WeaponDefinition(
            id: WeaponCatalog.StandardCannon.Id,
            displayName: WeaponCatalog.StandardCannon.DisplayName,
            description: WeaponCatalog.StandardCannon.Description,
            rawDamage: WeaponCatalog.StandardCannon.RawDamage,
            cooldownTicks: WeaponCatalog.StandardCannon.CooldownTicks,
            projectileSpeedPerTick: WeaponCatalog.StandardCannon.ProjectileSpeedPerTick,
            projectileRange: WeaponCatalog.StandardCannon.ProjectileRange,
            projectileRadius: WeaponCatalog.StandardCannon.ProjectileRadius,
            muzzleOffsetFromCenter: WeaponCatalog.StandardCannon.MuzzleOffsetFromCenter,
            tags: WeaponCatalog.StandardCannon.Tags);
    }

    // -------------------- Block A: Constructor --------------------

    [Fact]
    public void Constructor_PreservesDefinitionReference()
    {
        WeaponState state = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(7),
            hasFired: true);

        Assert.Same(WeaponCatalog.StandardCannon, state.Definition);
    }

    [Fact]
    public void Constructor_PreservesLastFireTick()
    {
        WeaponState state = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(42),
            hasFired: true);

        Assert.Equal(new SimTick(42), state.LastFireTick);
    }

    [Fact]
    public void Constructor_PreservesHasFired()
    {
        WeaponState state = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(0),
            hasFired: true);

        Assert.True(state.HasFired);
    }

    [Fact]
    public void Constructor_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(
            () => new WeaponState(null!, new SimTick(0), hasFired: false));
    }

    [Fact]
    public void Constructor_AcceptsHasFiredFalse()
    {
        WeaponState state = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(0),
            hasFired: false);

        Assert.False(state.HasFired);
    }

    [Fact]
    public void Constructor_AcceptsHasFiredTrue()
    {
        WeaponState state = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(0),
            hasFired: true);

        Assert.True(state.HasFired);
    }

    // -------------------- Block B: Ready Factory --------------------

    [Fact]
    public void Ready_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(
            () => WeaponState.Ready(null!));
    }

    [Fact]
    public void Ready_PreservesDefinitionReference()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.Same(WeaponCatalog.StandardCannon, state.Definition);
    }

    [Fact]
    public void Ready_SetsLastFireTickToZero()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.Equal(SimTick.Zero, state.LastFireTick);
    }

    [Fact]
    public void Ready_SetsHasFiredToFalse()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.False(state.HasFired);
    }

    [Fact]
    public void Ready_IsReadyAtSimTickZero()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.True(state.IsReady(SimTick.Zero));
    }

    [Fact]
    public void Ready_IsReadyAtLaterTick()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.True(state.IsReady(new SimTick(123)));
    }

    // -------------------- Block C: IsReady --------------------

    [Fact]
    public void IsReady_ReturnsTrue_WhenWeaponHasNeverFired()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.True(state.IsReady(new SimTick(5)));
    }

    [Fact]
    public void IsReady_ReturnsFalse_BeforeCooldownElapsed()
    {
        WeaponState state = CreateFiredState(lastFireTick: 10);
        Assert.False(state.IsReady(new SimTick(39)));
    }

    [Fact]
    public void IsReady_ReturnsTrue_WhenCooldownExactlyElapsed()
    {
        WeaponState state = CreateFiredState(lastFireTick: 10);
        Assert.True(state.IsReady(new SimTick(40)));
    }

    [Fact]
    public void IsReady_ReturnsTrue_WhenCooldownExceeded()
    {
        WeaponState state = CreateFiredState(lastFireTick: 10);
        Assert.True(state.IsReady(new SimTick(41)));
    }

    [Fact]
    public void IsReady_ReturnsFalse_WhenCurrentTickIsBeforeLastFireTick()
    {
        WeaponState state = CreateFiredState(lastFireTick: 10);
        Assert.False(state.IsReady(new SimTick(9)));
    }

    [Fact]
    public void IsReady_UsesDefinitionCooldownTicks_BelowCooldown()
    {
        WeaponState state = CreateFiredState(
            definition: WeaponCatalog.Railgun,
            lastFireTick: 10);

        Assert.False(state.IsReady(new SimTick(69)));
    }

    [Fact]
    public void IsReady_UsesDefinitionCooldownTicks_AtOrAboveCooldown()
    {
        WeaponState state = CreateFiredState(
            definition: WeaponCatalog.Railgun,
            lastFireTick: 10);

        Assert.True(state.IsReady(new SimTick(70)));
    }

    // -------------------- Block D: MarkFired --------------------

    [Fact]
    public void MarkFired_SetsHasFiredToTrue()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        WeaponState fired = state.MarkFired(new SimTick(5));
        Assert.True(fired.HasFired);
    }

    [Fact]
    public void MarkFired_SetsLastFireTickToProvidedFireTick()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        WeaponState fired = state.MarkFired(new SimTick(123));
        Assert.Equal(new SimTick(123), fired.LastFireTick);
    }

    [Fact]
    public void MarkFired_PreservesDefinitionReference()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        WeaponState fired = state.MarkFired(new SimTick(5));
        Assert.Same(WeaponCatalog.StandardCannon, fired.Definition);
    }

    [Fact]
    public void MarkFired_DoesNotRequireWeaponToBeReady()
    {
        WeaponState state = CreateFiredState(lastFireTick: 10);
        Assert.False(state.IsReady(new SimTick(11)));

        WeaponState fired = state.MarkFired(new SimTick(11));

        Assert.Equal(new SimTick(11), fired.LastFireTick);
        Assert.True(fired.HasFired);
    }

    // -------------------- Block E: Equality --------------------

    [Fact]
    public void Equals_ReturnsTrue_ForSameValues()
    {
        WeaponState a = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(10),
            hasFired: true);
        WeaponState b = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(10),
            hasFired: true);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDefinitionReferenceDiffers()
    {
        WeaponState original = WeaponState.Ready(WeaponCatalog.StandardCannon);
        WeaponState cloneDefinitionState = WeaponState.Ready(CreateStandardCannonClone());

        Assert.False(original.Equals(cloneDefinitionState));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenLastFireTickDiffers()
    {
        WeaponState a = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(10),
            hasFired: true);
        WeaponState b = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(11),
            hasFired: true);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenHasFiredDiffers()
    {
        WeaponState a = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(0),
            hasFired: false);
        WeaponState b = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(0),
            hasFired: true);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Object_ReturnsFalse_ForNull()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.False(state.Equals((object?)null));
    }

    [Fact]
    public void Equals_Object_ReturnsFalse_ForWrongType()
    {
        WeaponState state = WeaponState.Ready(WeaponCatalog.StandardCannon);
        Assert.False(state.Equals("not a weapon state"));
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        WeaponState a = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(10),
            hasFired: true);
        WeaponState b = new WeaponState(
            WeaponCatalog.StandardCannon,
            new SimTick(10),
            hasFired: true);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // -------------------- Block F: ToString --------------------

    [Fact]
    public void ToString_ContainsKeyValues_SmokeOnly()
    {
        WeaponState state = CreateFiredState(lastFireTick: 10);
        string result = state.ToString();

        Assert.Contains("standard_cannon", result);
        Assert.Contains("10", result);
        Assert.Contains("True", result);
    }
}
