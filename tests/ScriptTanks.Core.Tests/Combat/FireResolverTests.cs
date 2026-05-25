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

public sealed class FireResolverTests
{
    private static TankState CreateShooter()
        => TankSpawnFactory.Create(
            new TankId(7),
            TankCatalog.BasicTank,
            ArenaCatalog.OpenTestArena.StartPositions[0]);

    private static WeaponState ReadyStandardCannon()
        => WeaponState.Ready(WeaponCatalog.StandardCannon);

    private static WeaponState FiredStandardCannonAt(int tick)
        => WeaponState.Ready(WeaponCatalog.StandardCannon)
            .MarkFired(new SimTick(tick));

    private static FireResolutionOutcome ResolveReadyShot()
        => FireResolver.Resolve(
            shooter: CreateShooter(),
            weaponSlot: new WeaponSlot(1),
            weapon: ReadyStandardCannon(),
            currentTick: new SimTick(10),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

    // -------------------- Outcome Model --------------------

    [Fact]
    public void NotReady_CreatesNoFireOutcome()
    {
        FireResolutionOutcome outcome = FireResolutionOutcome.NotReady(ReadyStandardCannon());

        Assert.False(outcome.DidFire);
        Assert.Null(outcome.SpawnedProjectile);
    }

    [Fact]
    public void NotReady_PreservesWeapon()
    {
        WeaponState weapon = ReadyStandardCannon();

        FireResolutionOutcome outcome = FireResolutionOutcome.NotReady(weapon);

        Assert.Equal(weapon, outcome.UpdatedWeapon);
    }

    [Fact]
    public void Fired_CreatesFireOutcome()
    {
        WeaponState updatedWeapon = ReadyStandardCannon().MarkFired(new SimTick(10));
        ProjectileState projectile = ProjectileSpawnFactory.Create(
            new ProjectileId(3),
            new ProjectileDefinition(
                rawDamage: 25,
                speedPerTick: Fixed.FromInt(1),
                maxRange: Fixed.FromInt(40),
                radius: Fixed.FromRatio(1, 4)),
            new TankId(7),
            new WeaponSlot(1),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            new SimTick(10));

        FireResolutionOutcome outcome = FireResolutionOutcome.Fired(updatedWeapon, projectile);

        Assert.True(outcome.DidFire);
        Assert.True(outcome.SpawnedProjectile.HasValue);
    }

    [Fact]
    public void Fired_PreservesUpdatedWeapon()
    {
        WeaponState updatedWeapon = ReadyStandardCannon().MarkFired(new SimTick(10));
        ProjectileState projectile = ProjectileSpawnFactory.Create(
            new ProjectileId(3),
            new ProjectileDefinition(
                rawDamage: 25,
                speedPerTick: Fixed.FromInt(1),
                maxRange: Fixed.FromInt(40),
                radius: Fixed.FromRatio(1, 4)),
            new TankId(7),
            new WeaponSlot(1),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            new SimTick(10));

        FireResolutionOutcome outcome = FireResolutionOutcome.Fired(updatedWeapon, projectile);

        Assert.Equal(updatedWeapon, outcome.UpdatedWeapon);
    }

    [Fact]
    public void Fired_PreservesSpawnedProjectile()
    {
        WeaponState updatedWeapon = ReadyStandardCannon().MarkFired(new SimTick(10));
        ProjectileState projectile = ProjectileSpawnFactory.Create(
            new ProjectileId(3),
            new ProjectileDefinition(
                rawDamage: 25,
                speedPerTick: Fixed.FromInt(1),
                maxRange: Fixed.FromInt(40),
                radius: Fixed.FromRatio(1, 4)),
            new TankId(7),
            new WeaponSlot(1),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            new SimTick(10));

        FireResolutionOutcome outcome = FireResolutionOutcome.Fired(updatedWeapon, projectile);

        Assert.True(outcome.SpawnedProjectile.HasValue);
        Assert.Equal(projectile, outcome.SpawnedProjectile.Value);
    }

    // -------------------- Not Ready --------------------

    [Fact]
    public void Resolve_NotReadyWeapon_DoesNotFire()
    {
        WeaponState firedWeapon = FiredStandardCannonAt(10);

        FireResolutionOutcome outcome = FireResolver.Resolve(
            shooter: CreateShooter(),
            weaponSlot: new WeaponSlot(1),
            weapon: firedWeapon,
            currentTick: new SimTick(11),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.False(outcome.DidFire);
        Assert.Null(outcome.SpawnedProjectile);
    }

    [Fact]
    public void Resolve_NotReadyWeapon_PreservesWeaponState()
    {
        WeaponState firedWeapon = FiredStandardCannonAt(10);

        FireResolutionOutcome outcome = FireResolver.Resolve(
            shooter: CreateShooter(),
            weaponSlot: new WeaponSlot(1),
            weapon: firedWeapon,
            currentTick: new SimTick(11),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.Equal(firedWeapon, outcome.UpdatedWeapon);
    }

    // -------------------- Ready / Weapon Update --------------------

    [Fact]
    public void Resolve_ReadyWeapon_Fires()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.True(outcome.DidFire);
        Assert.True(outcome.SpawnedProjectile.HasValue);
    }

    [Fact]
    public void Resolve_ReadyWeapon_MarksWeaponFired()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.True(outcome.UpdatedWeapon.HasFired);
        Assert.Equal(new SimTick(10), outcome.UpdatedWeapon.LastFireTick);
    }

    [Fact]
    public void Resolve_ReadyWeapon_PreservesWeaponDefinitionReference()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Same(WeaponCatalog.StandardCannon, outcome.UpdatedWeapon.Definition);
    }

    // -------------------- Projectile Spawn --------------------

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileWithProvidedProjectileId()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(new ProjectileId(3), outcome.SpawnedProjectile!.Value.Id);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileWithShooterTankId()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(new TankId(7), outcome.SpawnedProjectile!.Value.OwnerTankId);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileWithWeaponSlot()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(new WeaponSlot(1), outcome.SpawnedProjectile!.Value.OwnerWeaponSlot);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileWithCurrentTickAsSpawnTick()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(new SimTick(10), outcome.SpawnedProjectile!.Value.SpawnTick);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileAtMuzzlePosition()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(FixedVec2.FromInts(10, 20), outcome.SpawnedProjectile!.Value.Position);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsProjectileWithFireVelocity()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(FixedVec2.FromInts(1, 0), outcome.SpawnedProjectile!.Value.VelocityPerTick);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SpawnsActiveProjectile()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.True(outcome.SpawnedProjectile!.Value.IsActive);
    }

    [Fact]
    public void Resolve_ReadyWeapon_SetsProjectileRemainingRangeFromWeaponRange()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(
            WeaponCatalog.StandardCannon.ProjectileRange,
            outcome.SpawnedProjectile!.Value.RemainingRange);
    }

    // -------------------- Projectile Definition Mapping --------------------

    [Fact]
    public void Resolve_ReadyWeapon_MapsRawDamageFromWeaponDefinition()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(
            WeaponCatalog.StandardCannon.RawDamage,
            outcome.SpawnedProjectile!.Value.Definition.RawDamage);
    }

    [Fact]
    public void Resolve_ReadyWeapon_MapsSpeedFromWeaponDefinition()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(
            WeaponCatalog.StandardCannon.ProjectileSpeedPerTick,
            outcome.SpawnedProjectile!.Value.Definition.SpeedPerTick);
    }

    [Fact]
    public void Resolve_ReadyWeapon_MapsRangeFromWeaponDefinition()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(
            WeaponCatalog.StandardCannon.ProjectileRange,
            outcome.SpawnedProjectile!.Value.Definition.MaxRange);
    }

    [Fact]
    public void Resolve_ReadyWeapon_MapsRadiusFromWeaponDefinition()
    {
        FireResolutionOutcome outcome = ResolveReadyShot();

        Assert.Equal(
            WeaponCatalog.StandardCannon.ProjectileRadius,
            outcome.SpawnedProjectile!.Value.Definition.Radius);
    }

    // -------------------- Edge / Purity --------------------

    [Fact]
    public void Resolve_DoesNotRequireVelocityToMatchWeaponSpeed()
    {
        FireResolutionOutcome outcome = FireResolver.Resolve(
            shooter: CreateShooter(),
            weaponSlot: new WeaponSlot(1),
            weapon: ReadyStandardCannon(),
            currentTick: new SimTick(10),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(10, 0));

        Assert.True(outcome.DidFire);
        Assert.Equal(
            FixedVec2.FromInts(10, 0),
            outcome.SpawnedProjectile!.Value.VelocityPerTick);
    }

    [Fact]
    public void Resolve_DoesNotMutateInputWeapon()
    {
        WeaponState originalWeapon = ReadyStandardCannon();
        WeaponState weaponSnapshot = originalWeapon;

        FireResolver.Resolve(
            shooter: CreateShooter(),
            weaponSlot: new WeaponSlot(1),
            weapon: originalWeapon,
            currentTick: new SimTick(10),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.Equal(weaponSnapshot, originalWeapon);
        Assert.False(originalWeapon.HasFired);
    }

    [Fact]
    public void Resolve_DoesNotMutateShooter()
    {
        TankState originalShooter = CreateShooter();
        TankState shooterSnapshot = originalShooter;

        FireResolver.Resolve(
            shooter: originalShooter,
            weaponSlot: new WeaponSlot(1),
            weapon: ReadyStandardCannon(),
            currentTick: new SimTick(10),
            projectileId: new ProjectileId(3),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: FixedVec2.FromInts(1, 0));

        Assert.Equal(shooterSnapshot, originalShooter);
    }
}
