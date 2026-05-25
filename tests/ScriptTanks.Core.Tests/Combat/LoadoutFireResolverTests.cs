using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class LoadoutFireResolverTests
{
    private static TankState CreateShooter()
        => TankSpawnFactory.Create(
            new TankId(7),
            TankCatalog.BasicTank,
            ArenaCatalog.OpenTestArena.StartPositions[0]);

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
            WeaponState.Ready(WeaponCatalog.Railgun),
        });

    private static TankWeaponLoadout CreateLoadoutWithNotReadySlot0()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon)
                .MarkFired(new SimTick(10)),
            WeaponState.Ready(WeaponCatalog.Railgun),
        });

    private static LoadoutFireResolutionOutcome ResolveSlot0Ready()
        => LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: CreateLoadout(),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(20),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

    private static ProjectileState CreateSampleProjectile()
        => ProjectileSpawnFactory.Create(
            new ProjectileId(3),
            new ProjectileDefinition(
                rawDamage: 25,
                speedPerTick: Fixed.FromInt(1),
                maxRange: Fixed.FromInt(40),
                radius: Fixed.FromRatio(1, 4)),
            new TankId(7),
            new WeaponSlot(0),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            new SimTick(0));

    // -------------------- Outcome Model --------------------

    [Fact]
    public void NotReady_CreatesNoFireOutcome()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolutionOutcome.NotReady(loadout);

        Assert.False(outcome.DidFire);
        Assert.Null(outcome.SpawnedProjectile);
    }

    [Fact]
    public void NotReady_PreservesUpdatedLoadoutReference()
    {
        TankWeaponLoadout loadout = CreateLoadout();

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolutionOutcome.NotReady(loadout);

        Assert.Same(loadout, outcome.UpdatedLoadout);
    }

    [Fact]
    public void NotReady_RejectsNullUpdatedLoadout()
    {
        Assert.Throws<ArgumentNullException>(
            () => LoadoutFireResolutionOutcome.NotReady(null!));
    }

    [Fact]
    public void Fired_CreatesFireOutcome()
    {
        TankWeaponLoadout loadout = CreateLoadout();
        ProjectileState projectile = CreateSampleProjectile();

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolutionOutcome.Fired(
            loadout,
            projectile);

        Assert.True(outcome.DidFire);
        Assert.True(outcome.SpawnedProjectile.HasValue);
    }

    [Fact]
    public void Fired_PreservesUpdatedLoadoutReference()
    {
        TankWeaponLoadout loadout = CreateLoadout();
        ProjectileState projectile = CreateSampleProjectile();

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolutionOutcome.Fired(
            loadout,
            projectile);

        Assert.Same(loadout, outcome.UpdatedLoadout);
    }

    [Fact]
    public void Fired_PreservesSpawnedProjectile()
    {
        TankWeaponLoadout loadout = CreateLoadout();
        ProjectileState projectile = CreateSampleProjectile();

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolutionOutcome.Fired(
            loadout,
            projectile);

        Assert.True(outcome.SpawnedProjectile.HasValue);
        Assert.Equal(projectile, outcome.SpawnedProjectile.Value);
    }

    [Fact]
    public void Fired_RejectsNullUpdatedLoadout()
    {
        ProjectileState projectile = CreateSampleProjectile();

        Assert.Throws<ArgumentNullException>(
            () => LoadoutFireResolutionOutcome.Fired(null!, projectile));
    }

    // -------------------- Validation / Slot --------------------

    [Fact]
    public void Resolve_RejectsNullLoadout()
    {
        Assert.Throws<ArgumentNullException>(
            () => LoadoutFireResolver.Resolve(
                shooter: CreateShooter(),
                loadout: null!,
                weaponSlot: new WeaponSlot(0),
                currentTick: new SimTick(20),
                projectileId: new ProjectileId(3),
                muzzlePosition: FixedVec2.FromInts(10, 20),
                fireVelocity: FixedVec2.FromInts(1, 0)));
    }

    [Fact]
    public void Resolve_RejectsSlotEqualToCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LoadoutFireResolver.Resolve(
                shooter: CreateShooter(),
                loadout: CreateLoadout(),
                weaponSlot: new WeaponSlot(2),
                currentTick: new SimTick(20),
                projectileId: new ProjectileId(3),
                muzzlePosition: FixedVec2.FromInts(10, 20),
                fireVelocity: FixedVec2.FromInts(1, 0)));
    }

    [Fact]
    public void Resolve_RejectsSlotGreaterThanCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LoadoutFireResolver.Resolve(
                shooter: CreateShooter(),
                loadout: CreateLoadout(),
                weaponSlot: new WeaponSlot(99),
                currentTick: new SimTick(20),
                projectileId: new ProjectileId(3),
                muzzlePosition: FixedVec2.FromInts(10, 20),
                fireVelocity: FixedVec2.FromInts(1, 0)));
    }

    // -------------------- Ready Fire --------------------

    [Fact]
    public void Resolve_ReadyWeapon_Fires()
    {
        LoadoutFireResolutionOutcome outcome = ResolveSlot0Ready();

        Assert.True(outcome.DidFire);
        Assert.True(outcome.SpawnedProjectile.HasValue);
    }

    [Fact]
    public void Resolve_ReadyWeapon_UpdatesWeaponAtSlot()
    {
        LoadoutFireResolutionOutcome outcome = ResolveSlot0Ready();

        WeaponState updatedSlot0 = outcome.UpdatedLoadout.GetWeapon(new WeaponSlot(0));
        Assert.True(updatedSlot0.HasFired);
        Assert.Equal(new SimTick(20), updatedSlot0.LastFireTick);
    }

    [Fact]
    public void Resolve_ReadyWeapon_PreservesOtherSlots()
    {
        LoadoutFireResolutionOutcome outcome = ResolveSlot0Ready();

        WeaponState slot1 = outcome.UpdatedLoadout.GetWeapon(new WeaponSlot(1));
        Assert.Same(WeaponCatalog.Railgun, slot1.Definition);
        Assert.False(slot1.HasFired);
        Assert.True(slot1.IsReady(new SimTick(20)));
    }

    [Fact]
    public void Resolve_ReadyWeapon_DoesNotMutateOriginalLoadout()
    {
        TankWeaponLoadout originalLoadout = CreateLoadout();

        LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: originalLoadout,
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(20),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        WeaponState originalSlot0 = originalLoadout.GetWeapon(new WeaponSlot(0));
        Assert.False(originalSlot0.HasFired);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileWithShooterAndSlot()
    {
        LoadoutFireResolutionOutcome outcome = ResolveSlot0Ready();

        ProjectileState projectile = outcome.SpawnedProjectile!.Value;
        Assert.Equal(new TankId(7), projectile.OwnerTankId);
        Assert.Equal(new WeaponSlot(0), projectile.OwnerWeaponSlot);
    }

    [Fact]
    public void Resolve_ReadyWeapon_UsesProvidedProjectileIdPositionAndVelocity()
    {
        LoadoutFireResolutionOutcome outcome = ResolveSlot0Ready();

        ProjectileState projectile = outcome.SpawnedProjectile!.Value;
        Assert.Equal(new ProjectileId(3), projectile.Id);
        Assert.Equal(FixedVec2.FromInts(10, 20), projectile.Position);
        Assert.Equal(FixedVec2.FromInts(1, 0), projectile.VelocityPerTick);
    }

    // -------------------- Not Ready --------------------

    [Fact]
    public void Resolve_NotReadyWeapon_DoesNotFire()
    {
        LoadoutFireResolutionOutcome outcome = LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: CreateLoadoutWithNotReadySlot0(),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(11),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.False(outcome.DidFire);
        Assert.Null(outcome.SpawnedProjectile);
    }

    [Fact]
    public void Resolve_NotReadyWeapon_PreservesWeaponAtSlot()
    {
        TankWeaponLoadout loadout = CreateLoadoutWithNotReadySlot0();
        WeaponState originalSlot0 = loadout.GetWeapon(new WeaponSlot(0));

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: loadout,
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(11),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        WeaponState updatedSlot0 = outcome.UpdatedLoadout.GetWeapon(new WeaponSlot(0));
        Assert.Equal(originalSlot0, updatedSlot0);
    }

    [Fact]
    public void Resolve_NotReadyWeapon_PreservesOtherSlots()
    {
        TankWeaponLoadout loadout = CreateLoadoutWithNotReadySlot0();
        WeaponState originalSlot1 = loadout.GetWeapon(new WeaponSlot(1));

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: loadout,
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(11),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        WeaponState updatedSlot1 = outcome.UpdatedLoadout.GetWeapon(new WeaponSlot(1));
        Assert.Equal(originalSlot1, updatedSlot1);
    }

    // -------------------- Delegation / Definition Mapping --------------------

    [Fact]
    public void Resolve_ReadyWeapon_ProjectileDefinitionMirrorsWeaponDefinition()
    {
        LoadoutFireResolutionOutcome outcome = LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: CreateLoadout(),
            weaponSlot: new WeaponSlot(1),
            currentTick: new SimTick(20),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(2, 0));

        ProjectileDefinition projectileDef = outcome.SpawnedProjectile!.Value.Definition;
        Assert.Equal(WeaponCatalog.Railgun.RawDamage, projectileDef.RawDamage);
        Assert.Equal(WeaponCatalog.Railgun.ProjectileSpeedPerTick, projectileDef.SpeedPerTick);
        Assert.Equal(WeaponCatalog.Railgun.ProjectileRange, projectileDef.MaxRange);
        Assert.Equal(WeaponCatalog.Railgun.ProjectileRadius, projectileDef.Radius);
    }

    [Fact]
    public void Resolve_DoesNotRequireVelocityToMatchWeaponSpeed()
    {
        LoadoutFireResolutionOutcome outcome = LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: CreateLoadout(),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(20),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(10, 0));

        Assert.True(outcome.DidFire);
        Assert.Equal(
            FixedVec2.FromInts(10, 0),
            outcome.SpawnedProjectile!.Value.VelocityPerTick);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void Resolve_DoesNotMutateShooter()
    {
        TankState originalShooter = CreateShooter();
        TankState shooterSnapshot = originalShooter;

        LoadoutFireResolver.Resolve(
            shooter: originalShooter,
            loadout: CreateLoadout(),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(20),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.Equal(shooterSnapshot, originalShooter);
    }

    [Fact]
    public void Resolve_ReturnsNewLoadoutInstance()
    {
        TankWeaponLoadout originalLoadout = CreateLoadout();

        LoadoutFireResolutionOutcome outcome = LoadoutFireResolver.Resolve(
            shooter: CreateShooter(),
            loadout: originalLoadout,
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(20),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.NotSame(originalLoadout, outcome.UpdatedLoadout);
    }
}
