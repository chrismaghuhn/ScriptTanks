using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Weapons;

public sealed class WeaponCatalogTests
{
    private static void AssertWeapon(
        string id,
        string displayName,
        string description,
        int rawDamage,
        int cooldownTicks,
        Fixed projectileSpeed,
        Fixed projectileRange,
        Fixed projectileRadius,
        Fixed muzzleOffsetFromCenter,
        IReadOnlyList<string> tags,
        WeaponDefinition actual)
    {
        Assert.Equal(id, actual.Id);
        Assert.Equal(displayName, actual.DisplayName);
        Assert.Equal(description, actual.Description);
        Assert.Equal(rawDamage, actual.RawDamage);
        Assert.Equal(cooldownTicks, actual.CooldownTicks);
        Assert.Equal(projectileSpeed, actual.ProjectileSpeedPerTick);
        Assert.Equal(projectileRange, actual.ProjectileRange);
        Assert.Equal(projectileRadius, actual.ProjectileRadius);
        Assert.Equal(muzzleOffsetFromCenter, actual.MuzzleOffsetFromCenter);
        Assert.Equal(tags, actual.Tags);
    }

    // -------------------- Block D: Per-Weapon --------------------

    [Fact]
    public void StandardCannon_IsNotNull()
    {
        Assert.NotNull(WeaponCatalog.StandardCannon);
    }

    [Fact]
    public void StandardCannon_HasExpectedValues()
    {
        AssertWeapon(
            id: "standard_cannon",
            displayName: "Standard Cannon",
            description: "Balanced baseline cannon for deterministic weapon tests.",
            rawDamage: 25,
            cooldownTicks: 30,
            projectileSpeed: Fixed.FromInt(1),
            projectileRange: Fixed.FromInt(40),
            projectileRadius: Fixed.FromRatio(1, 4),
            muzzleOffsetFromCenter: Fixed.FromInt(1),
            tags: new[] { "test", "cannon", "standard" },
            actual: WeaponCatalog.StandardCannon);
    }

    [Fact]
    public void ShotgunCannon_IsNotNull()
    {
        Assert.NotNull(WeaponCatalog.ShotgunCannon);
    }

    [Fact]
    public void ShotgunCannon_HasExpectedValues()
    {
        AssertWeapon(
            id: "shotgun_cannon",
            displayName: "Shotgun Cannon",
            description: "Short-range high-impact cannon for close-distance weapon tests.",
            rawDamage: 18,
            cooldownTicks: 24,
            projectileSpeed: Fixed.FromRatio(8, 10),
            projectileRange: Fixed.FromInt(18),
            projectileRadius: Fixed.FromRatio(35, 100),
            muzzleOffsetFromCenter: Fixed.FromInt(1),
            tags: new[] { "test", "cannon", "close_range" },
            actual: WeaponCatalog.ShotgunCannon);
    }

    [Fact]
    public void Railgun_IsNotNull()
    {
        Assert.NotNull(WeaponCatalog.Railgun);
    }

    [Fact]
    public void Railgun_HasExpectedValues()
    {
        AssertWeapon(
            id: "railgun",
            displayName: "Railgun",
            description: "Long-range high-damage cannon for precision weapon tests.",
            rawDamage: 45,
            cooldownTicks: 60,
            projectileSpeed: Fixed.FromInt(2),
            projectileRange: Fixed.FromInt(70),
            projectileRadius: Fixed.FromRatio(15, 100),
            muzzleOffsetFromCenter: Fixed.FromInt(2),
            tags: new[] { "test", "cannon", "precision" },
            actual: WeaponCatalog.Railgun);
    }

    [Fact]
    public void StandardCannon_MuzzleOffsetFromCenter_IsPositive()
    {
        Assert.True(WeaponCatalog.StandardCannon.MuzzleOffsetFromCenter > Fixed.Zero);
    }

    [Fact]
    public void All_CatalogWeapons_HavePositiveMuzzleOffsetFromCenter()
    {
        foreach (WeaponDefinition weapon in WeaponCatalog.All)
        {
            Assert.True(weapon.MuzzleOffsetFromCenter > Fixed.Zero);
        }
    }

    // -------------------- Block E: All --------------------

    [Fact]
    public void All_HasExpectedCount()
    {
        Assert.Equal(3, WeaponCatalog.All.Count);
    }

    [Fact]
    public void All_FirstEntry_IsStandardCannon()
    {
        Assert.Same(WeaponCatalog.StandardCannon, WeaponCatalog.All[0]);
    }

    [Fact]
    public void All_SecondEntry_IsShotgunCannon()
    {
        Assert.Same(WeaponCatalog.ShotgunCannon, WeaponCatalog.All[1]);
    }

    [Fact]
    public void All_ThirdEntry_IsRailgun()
    {
        Assert.Same(WeaponCatalog.Railgun, WeaponCatalog.All[2]);
    }

    [Fact]
    public void All_IsNotMutableArray()
    {
        Assert.IsNotType<WeaponDefinition[]>(WeaponCatalog.All);
    }

    // -------------------- Block F: GetById --------------------

    [Fact]
    public void GetById_StandardCannon_ReturnsSameInstance()
    {
        Assert.Same(WeaponCatalog.StandardCannon, WeaponCatalog.GetById("standard_cannon"));
    }

    [Fact]
    public void GetById_ShotgunCannon_ReturnsSameInstance()
    {
        Assert.Same(WeaponCatalog.ShotgunCannon, WeaponCatalog.GetById("shotgun_cannon"));
    }

    [Fact]
    public void GetById_Railgun_ReturnsSameInstance()
    {
        Assert.Same(WeaponCatalog.Railgun, WeaponCatalog.GetById("railgun"));
    }

    [Fact]
    public void GetById_NullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => WeaponCatalog.GetById(null!));
    }

    [Fact]
    public void GetById_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => WeaponCatalog.GetById(""));
    }

    [Fact]
    public void GetById_WhitespaceId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => WeaponCatalog.GetById("   "));
    }

    [Fact]
    public void GetById_UpperCaseId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(
            () => WeaponCatalog.GetById("STANDARD_CANNON"));
    }

    [Fact]
    public void GetById_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(
            () => WeaponCatalog.GetById("unknown"));
    }
}
