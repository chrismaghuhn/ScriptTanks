using System;
using System.Collections.Generic;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Weapons;

public sealed class TankWeaponLoadoutTests
{
    private static WeaponState StandardReady()
        => WeaponState.Ready(WeaponCatalog.StandardCannon);

    private static WeaponState ShotgunReady()
        => WeaponState.Ready(WeaponCatalog.ShotgunCannon);

    private static WeaponState RailgunReady()
        => WeaponState.Ready(WeaponCatalog.Railgun);

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            StandardReady(),
            ShotgunReady(),
        });

    // -------------------- Block A: Constructor --------------------

    [Fact]
    public void Constructor_RejectsNullWeaponsCollection()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankWeaponLoadout(null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyWeaponsCollection()
    {
        Assert.Throws<ArgumentException>(
            () => new TankWeaponLoadout(Array.Empty<WeaponState>()));
    }

    [Fact]
    public void Constructor_PreservesWeaponCount()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        Assert.Equal(2, loadout.Weapons.Count);
    }

    [Fact]
    public void Constructor_PreservesWeaponOrder()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        Assert.Same(WeaponCatalog.StandardCannon, loadout.Weapons[0].Definition);
        Assert.Same(WeaponCatalog.ShotgunCannon, loadout.Weapons[1].Definition);
    }

    [Fact]
    public void Constructor_AcceptsOneWeapon()
    {
        TankWeaponLoadout loadout = new TankWeaponLoadout(new[]
        {
            StandardReady(),
        });

        Assert.Single(loadout.Weapons);
        Assert.Same(WeaponCatalog.StandardCannon, loadout.Weapons[0].Definition);
    }

    [Fact]
    public void Constructor_AcceptsMultipleWeapons()
    {
        TankWeaponLoadout loadout = new TankWeaponLoadout(new[]
        {
            StandardReady(),
            ShotgunReady(),
            RailgunReady(),
        });

        Assert.Equal(3, loadout.Weapons.Count);
        Assert.Same(WeaponCatalog.StandardCannon, loadout.Weapons[0].Definition);
        Assert.Same(WeaponCatalog.ShotgunCannon, loadout.Weapons[1].Definition);
        Assert.Same(WeaponCatalog.Railgun, loadout.Weapons[2].Definition);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesWeaponCollection()
    {
        List<WeaponState> source = new List<WeaponState>
        {
            StandardReady(),
            ShotgunReady(),
        };

        TankWeaponLoadout loadout = new TankWeaponLoadout(source);
        source.Add(RailgunReady());

        Assert.Equal(2, loadout.Weapons.Count);
        Assert.Same(WeaponCatalog.StandardCannon, loadout.Weapons[0].Definition);
        Assert.Same(WeaponCatalog.ShotgunCannon, loadout.Weapons[1].Definition);
    }

    // -------------------- Block B: GetWeapon --------------------

    [Fact]
    public void GetWeapon_ReturnsWeaponAtSlot0()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        WeaponState weapon = loadout.GetWeapon(new WeaponSlot(0));

        Assert.Same(WeaponCatalog.StandardCannon, weapon.Definition);
    }

    [Fact]
    public void GetWeapon_ReturnsWeaponAtSlot1()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        WeaponState weapon = loadout.GetWeapon(new WeaponSlot(1));

        Assert.Same(WeaponCatalog.ShotgunCannon, weapon.Definition);
    }

    [Fact]
    public void GetWeapon_RejectsSlotEqualToCount()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => loadout.GetWeapon(new WeaponSlot(2)));
    }

    [Fact]
    public void GetWeapon_RejectsSlotGreaterThanCount()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => loadout.GetWeapon(new WeaponSlot(99)));
    }

    // -------------------- Block C: WithWeapon --------------------

    [Fact]
    public void WithWeapon_ReplacesWeaponAtSlot0()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        TankWeaponLoadout result = loadout.WithWeapon(new WeaponSlot(0), RailgunReady());

        Assert.Same(WeaponCatalog.Railgun, result.GetWeapon(new WeaponSlot(0)).Definition);
    }

    [Fact]
    public void WithWeapon_ReplacesWeaponAtSlot1()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        TankWeaponLoadout result = loadout.WithWeapon(new WeaponSlot(1), RailgunReady());

        Assert.Same(WeaponCatalog.Railgun, result.GetWeapon(new WeaponSlot(1)).Definition);
    }

    [Fact]
    public void WithWeapon_PreservesOtherSlots()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        TankWeaponLoadout result = loadout.WithWeapon(new WeaponSlot(0), RailgunReady());

        Assert.Same(WeaponCatalog.ShotgunCannon, result.GetWeapon(new WeaponSlot(1)).Definition);
    }

    [Fact]
    public void WithWeapon_ReturnsNewLoadoutInstance()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        TankWeaponLoadout result = loadout.WithWeapon(new WeaponSlot(0), RailgunReady());

        Assert.NotSame(loadout, result);
    }

    [Fact]
    public void WithWeapon_DoesNotMutateOriginal()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        TankWeaponLoadout _ = loadout.WithWeapon(new WeaponSlot(0), RailgunReady());

        Assert.Same(WeaponCatalog.StandardCannon, loadout.GetWeapon(new WeaponSlot(0)).Definition);
        Assert.Same(WeaponCatalog.ShotgunCannon, loadout.GetWeapon(new WeaponSlot(1)).Definition);
    }

    [Fact]
    public void WithWeapon_RejectsSlotEqualToCount()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => loadout.WithWeapon(new WeaponSlot(2), RailgunReady()));
    }

    [Fact]
    public void WithWeapon_RejectsSlotGreaterThanCount()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => loadout.WithWeapon(new WeaponSlot(99), RailgunReady()));
    }

    // -------------------- Block D: No Runtime Logic --------------------

    [Fact]
    public void WithWeapon_CanStoreFiredWeaponState()
    {
        TankWeaponLoadout loadout = CreateLoadout();
        WeaponState fired = WeaponState.Ready(WeaponCatalog.StandardCannon)
            .MarkFired(new SimTick(10));

        TankWeaponLoadout result = loadout.WithWeapon(new WeaponSlot(0), fired);

        WeaponState stored = result.GetWeapon(new WeaponSlot(0));
        Assert.True(stored.HasFired);
        Assert.Equal(new SimTick(10), stored.LastFireTick);
    }
}
