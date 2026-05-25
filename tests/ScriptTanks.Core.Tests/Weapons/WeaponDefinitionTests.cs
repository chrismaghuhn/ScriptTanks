using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Weapons;

public sealed class WeaponDefinitionTests
{
    private static WeaponDefinition CreateValidDefinition(
        string id = "test_weapon",
        string displayName = "Test Weapon",
        string description = "Test weapon definition.",
        int rawDamage = 10,
        int cooldownTicks = 5,
        Fixed? projectileSpeedPerTick = null,
        Fixed? projectileRange = null,
        Fixed? projectileRadius = null,
        Fixed? muzzleOffsetFromCenter = null,
        IEnumerable<string>? tags = null)
    {
        return new WeaponDefinition(
            id,
            displayName,
            description,
            rawDamage,
            cooldownTicks,
            projectileSpeedPerTick ?? Fixed.FromInt(1),
            projectileRange ?? Fixed.FromInt(10),
            projectileRadius ?? Fixed.FromRatio(1, 10),
            muzzleOffsetFromCenter ?? Fixed.FromInt(1),
            tags ?? new[] { "test" });
    }

    // -------------------- Block A: Preserve --------------------

    [Fact]
    public void Constructor_PreservesId()
    {
        WeaponDefinition definition = CreateValidDefinition(id: "weapon_x");
        Assert.Equal("weapon_x", definition.Id);
    }

    [Fact]
    public void Constructor_PreservesDisplayName()
    {
        WeaponDefinition definition = CreateValidDefinition(displayName: "Weapon X");
        Assert.Equal("Weapon X", definition.DisplayName);
    }

    [Fact]
    public void Constructor_PreservesDescription()
    {
        WeaponDefinition definition = CreateValidDefinition(description: "Specific description.");
        Assert.Equal("Specific description.", definition.Description);
    }

    [Fact]
    public void Constructor_PreservesRawDamage()
    {
        WeaponDefinition definition = CreateValidDefinition(rawDamage: 42);
        Assert.Equal(42, definition.RawDamage);
    }

    [Fact]
    public void Constructor_PreservesCooldownTicks()
    {
        WeaponDefinition definition = CreateValidDefinition(cooldownTicks: 17);
        Assert.Equal(17, definition.CooldownTicks);
    }

    [Fact]
    public void Constructor_PreservesProjectileSpeedPerTick()
    {
        Fixed value = Fixed.FromRatio(3, 2);
        WeaponDefinition definition = CreateValidDefinition(projectileSpeedPerTick: value);
        Assert.Equal(value, definition.ProjectileSpeedPerTick);
    }

    [Fact]
    public void Constructor_PreservesProjectileRange()
    {
        Fixed value = Fixed.FromInt(25);
        WeaponDefinition definition = CreateValidDefinition(projectileRange: value);
        Assert.Equal(value, definition.ProjectileRange);
    }

    [Fact]
    public void Constructor_PreservesProjectileRadius()
    {
        Fixed value = Fixed.FromRatio(7, 20);
        WeaponDefinition definition = CreateValidDefinition(projectileRadius: value);
        Assert.Equal(value, definition.ProjectileRadius);
    }

    [Fact]
    public void Constructor_PreservesMuzzleOffsetFromCenter()
    {
        Fixed value = Fixed.FromInt(3);
        WeaponDefinition definition = CreateValidDefinition(muzzleOffsetFromCenter: value);
        Assert.Equal(value, definition.MuzzleOffsetFromCenter);
    }

    [Fact]
    public void Constructor_PreservesTags()
    {
        WeaponDefinition definition = CreateValidDefinition(tags: new[] { "alpha", "beta" });
        Assert.Equal(new[] { "alpha", "beta" }, definition.Tags);
    }

    [Fact]
    public void Constructor_PreservesTagOrder()
    {
        WeaponDefinition definition = CreateValidDefinition(tags: new[] { "a", "b", "c" });
        Assert.Equal(new[] { "a", "b", "c" }, definition.Tags);
    }

    // -------------------- Block B: Metadata Validation --------------------

    [Fact]
    public void Constructor_RejectsNullId()
    {
        Assert.Throws<ArgumentNullException>(
            () => CreateValidDefinition(id: null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(id: ""));
    }

    [Fact]
    public void Constructor_RejectsWhitespaceId()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(id: "   "));
    }

    [Fact]
    public void Constructor_RejectsNullDisplayName()
    {
        Assert.Throws<ArgumentNullException>(
            () => CreateValidDefinition(displayName: null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyDisplayName()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(displayName: ""));
    }

    [Fact]
    public void Constructor_RejectsWhitespaceDisplayName()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(displayName: "   "));
    }

    [Fact]
    public void Constructor_RejectsNullDescription()
    {
        Assert.Throws<ArgumentNullException>(
            () => CreateValidDefinition(description: null!));
    }

    [Fact]
    public void Constructor_AcceptsEmptyDescription()
    {
        WeaponDefinition definition = CreateValidDefinition(description: "");
        Assert.Equal("", definition.Description);
    }

    [Fact]
    public void Constructor_RejectsNullTagsCollection()
    {
        Assert.Throws<ArgumentNullException>(
            () => new WeaponDefinition(
                id: "test_weapon",
                displayName: "Test Weapon",
                description: "Test weapon definition.",
                rawDamage: 10,
                cooldownTicks: 5,
                projectileSpeedPerTick: Fixed.FromInt(1),
                projectileRange: Fixed.FromInt(10),
                projectileRadius: Fixed.FromRatio(1, 10),
                muzzleOffsetFromCenter: Fixed.FromInt(1),
                tags: null!));
    }

    [Fact]
    public void Constructor_AcceptsEmptyTagsCollection()
    {
        WeaponDefinition definition = CreateValidDefinition(tags: Array.Empty<string>());
        Assert.Empty(definition.Tags);
    }

    [Fact]
    public void Constructor_RejectsNullTag()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(tags: new string[] { "ok", null! }));
    }

    [Fact]
    public void Constructor_RejectsEmptyTag()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(tags: new[] { "ok", "" }));
    }

    [Fact]
    public void Constructor_RejectsWhitespaceTag()
    {
        Assert.Throws<ArgumentException>(
            () => CreateValidDefinition(tags: new[] { "ok", "   " }));
    }

    [Fact]
    public void Constructor_DefensivelyCopiesTags()
    {
        List<string> source = new List<string> { "a", "b" };

        WeaponDefinition definition = CreateValidDefinition(tags: source);
        source.Add("c");

        Assert.Equal(new[] { "a", "b" }, definition.Tags);
    }

    // -------------------- Block C: Stat Validation --------------------

    [Fact]
    public void Constructor_RejectsZeroRawDamage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(rawDamage: 0));
    }

    [Fact]
    public void Constructor_RejectsNegativeRawDamage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(rawDamage: -1));
    }

    [Fact]
    public void Constructor_RejectsRawDamageAboveOverflowBound()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(rawDamage: int.MaxValue / 100 + 1));
    }

    [Fact]
    public void Constructor_AcceptsRawDamageAtOverflowBound()
    {
        WeaponDefinition definition = CreateValidDefinition(rawDamage: int.MaxValue / 100);
        Assert.Equal(int.MaxValue / 100, definition.RawDamage);
    }

    [Fact]
    public void Constructor_RejectsZeroCooldownTicks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(cooldownTicks: 0));
    }

    [Fact]
    public void Constructor_RejectsNegativeCooldownTicks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(cooldownTicks: -1));
    }

    [Fact]
    public void Constructor_RejectsZeroProjectileSpeedPerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(projectileSpeedPerTick: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeProjectileSpeedPerTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(projectileSpeedPerTick: Fixed.FromInt(-1)));
    }

    [Fact]
    public void Constructor_RejectsZeroProjectileRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(projectileRange: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeProjectileRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(projectileRange: Fixed.FromInt(-1)));
    }

    [Fact]
    public void Constructor_RejectsZeroProjectileRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(projectileRadius: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeProjectileRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(projectileRadius: Fixed.FromInt(-1)));
    }

    [Fact]
    public void Constructor_RejectsZeroMuzzleOffsetFromCenter()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(muzzleOffsetFromCenter: Fixed.Zero));

        Assert.Equal("muzzleOffsetFromCenter", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeMuzzleOffsetFromCenter()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValidDefinition(muzzleOffsetFromCenter: Fixed.FromInt(-1)));

        Assert.Equal("muzzleOffsetFromCenter", ex.ParamName);
    }
}
