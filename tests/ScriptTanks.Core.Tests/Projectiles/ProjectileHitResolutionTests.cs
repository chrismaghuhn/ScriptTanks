using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Projectiles;

public sealed class ProjectileHitResolutionTests
{
    private static ProjectileDefinition CreateProjectileDefinition(int rawDamage = 25)
    {
        return new ProjectileDefinition(
            rawDamage: rawDamage,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));
    }

    private static ProjectileState CreateProjectile(
        bool isActive = true,
        ProjectileDefinition? definition = null,
        int ownerTankId = 7)
    {
        return new ProjectileState(
            id: new ProjectileId(3),
            definition: definition ?? CreateProjectileDefinition(),
            ownerTankId: new TankId(ownerTankId),
            ownerWeaponSlot: new WeaponSlot(1),
            spawnTick: new SimTick(0),
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(40),
            isActive: isActive);
    }

    private static TankState CreateTank(
        int id,
        int ownerSlot = 0,
        FixedVec2? position = null,
        int? hp = null)
    {
        MovementState movement = new MovementState(
            position ?? FixedVec2.FromInts(10, 20),
            FixedVec2.Zero);

        return new TankState(
            id: new TankId(id),
            ownerSlot: new PlayerSlot(ownerSlot),
            definition: TankCatalog.BasicTank,
            movement: movement,
            currentHitPoints: hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static WallBlock CreateWall(string id = "test_wall")
    {
        return new WallBlock(
            id,
            FixedRect.FromMinSize(
                FixedVec2.FromInts(0, 0),
                Fixed.FromInt(5),
                Fixed.FromInt(5)));
    }

    // -------------------- Outcome Model --------------------

    [Fact]
    public void Outcome_PreservesUpdatedProjectile()
    {
        ProjectileState projectile = CreateProjectile();

        ProjectileHitResolutionOutcome outcome = new ProjectileHitResolutionOutcome(
            projectile,
            updatedTank: null);

        Assert.Equal(projectile, outcome.UpdatedProjectile);
    }

    [Fact]
    public void Outcome_WithNullTank_HasUpdatedTankFalse()
    {
        ProjectileState projectile = CreateProjectile();

        ProjectileHitResolutionOutcome outcome = new ProjectileHitResolutionOutcome(
            projectile,
            updatedTank: null);

        Assert.Null(outcome.UpdatedTank);
        Assert.False(outcome.HasUpdatedTank);
    }

    [Fact]
    public void Outcome_WithTank_HasUpdatedTankTrue()
    {
        ProjectileState projectile = CreateProjectile();
        TankState tank = CreateTank(id: 1);

        ProjectileHitResolutionOutcome outcome = new ProjectileHitResolutionOutcome(
            projectile,
            updatedTank: tank);

        Assert.True(outcome.HasUpdatedTank);
    }

    [Fact]
    public void Outcome_WithTank_PreservesUpdatedTank()
    {
        ProjectileState projectile = CreateProjectile();
        TankState tank = CreateTank(id: 1);

        ProjectileHitResolutionOutcome outcome = new ProjectileHitResolutionOutcome(
            projectile,
            updatedTank: tank);

        Assert.True(outcome.UpdatedTank.HasValue);
        Assert.True(outcome.UpdatedTank.Value.Equals(tank));
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Resolve_RejectsNullHit()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Throws<ArgumentNullException>(
            () => ProjectileHitResolution.Resolve(projectile, null!, Array.Empty<TankState>()));
    }

    [Fact]
    public void Resolve_RejectsNullTanks()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Throws<ArgumentNullException>(
            () => ProjectileHitResolution.Resolve(projectile, ProjectileHitResult.None, null!));
    }

    // -------------------- No-Op --------------------

    [Fact]
    public void Resolve_InactiveProjectile_ReturnsNoOpEvenForTankHit()
    {
        ProjectileState projectile = CreateProjectile(isActive: false);
        TankState tank = CreateTank(id: 7);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { tank });

        Assert.Equal(projectile, outcome.UpdatedProjectile);
        Assert.False(outcome.HasUpdatedTank);
    }

    [Fact]
    public void Resolve_NoneHit_ReturnsNoOp()
    {
        ProjectileState projectile = CreateProjectile();

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            ProjectileHitResult.None,
            Array.Empty<TankState>());

        Assert.Equal(projectile, outcome.UpdatedProjectile);
        Assert.False(outcome.HasUpdatedTank);
    }

    // -------------------- Wall Hit --------------------

    [Fact]
    public void Resolve_WallHit_DeactivatesProjectile()
    {
        ProjectileState projectile = CreateProjectile();
        ProjectileHitResult hit = ProjectileHitResult.Wall(CreateWall());

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            Array.Empty<TankState>());

        Assert.False(outcome.UpdatedProjectile.IsActive);
    }

    [Fact]
    public void Resolve_WallHit_DoesNotUpdateTank()
    {
        ProjectileState projectile = CreateProjectile();
        ProjectileHitResult hit = ProjectileHitResult.Wall(CreateWall());

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            Array.Empty<TankState>());

        Assert.Null(outcome.UpdatedTank);
        Assert.False(outcome.HasUpdatedTank);
    }

    [Fact]
    public void Resolve_WallHit_PreservesProjectileIdentityAndStateExceptActiveFlag()
    {
        ProjectileState projectile = CreateProjectile();
        ProjectileHitResult hit = ProjectileHitResult.Wall(CreateWall());

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            Array.Empty<TankState>());

        ProjectileState updated = outcome.UpdatedProjectile;
        Assert.Equal(projectile.Id, updated.Id);
        Assert.Same(projectile.Definition, updated.Definition);
        Assert.Equal(projectile.OwnerTankId, updated.OwnerTankId);
        Assert.Equal(projectile.OwnerWeaponSlot, updated.OwnerWeaponSlot);
        Assert.Equal(projectile.Position, updated.Position);
        Assert.Equal(projectile.VelocityPerTick, updated.VelocityPerTick);
        Assert.Equal(projectile.RemainingRange, updated.RemainingRange);
        Assert.False(updated.IsActive);
    }

    // -------------------- Tank Hit --------------------

    [Fact]
    public void Resolve_TankHit_DeactivatesProjectile()
    {
        ProjectileState projectile = CreateProjectile();
        TankState tank = CreateTank(id: 1);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { tank });

        Assert.False(outcome.UpdatedProjectile.IsActive);
    }

    [Fact]
    public void Resolve_TankHit_AppliesProjectileRawDamageToHitTank()
    {
        ProjectileState projectile = CreateProjectile(
            definition: CreateProjectileDefinition(rawDamage: 25));
        TankState tank = CreateTank(id: 1);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { tank });

        Assert.True(outcome.HasUpdatedTank);
        Assert.Equal(80, outcome.UpdatedTank!.Value.CurrentHitPoints);
    }

    [Fact]
    public void Resolve_TankHit_UsesProjectileDefinitionRawDamage()
    {
        ProjectileState projectile = CreateProjectile(
            definition: CreateProjectileDefinition(rawDamage: 10));
        TankState tank = CreateTank(id: 1);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { tank });

        Assert.True(outcome.HasUpdatedTank);
        Assert.Equal(92, outcome.UpdatedTank!.Value.CurrentHitPoints);
    }

    [Fact]
    public void Resolve_TankHit_ReturnsUpdatedTankWithMatchingId()
    {
        ProjectileState projectile = CreateProjectile();
        TankState tank1 = CreateTank(id: 1);
        TankState tank7 = CreateTank(id: 7, ownerSlot: 1);
        TankState tank9 = CreateTank(id: 9, ownerSlot: 2);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank7);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { tank1, tank7, tank9 });

        Assert.True(outcome.HasUpdatedTank);
        Assert.Equal(new TankId(7), outcome.UpdatedTank!.Value.Id);
    }

    [Fact]
    public void Resolve_TankHit_UsesFirstMatchingTankWhenDuplicateIdsExist()
    {
        ProjectileState projectile = CreateProjectile();
        TankState firstTankWithId7 = CreateTank(id: 7, ownerSlot: 0, hp: 100);
        TankState secondTankWithId7 = CreateTank(id: 7, ownerSlot: 1, hp: 50);
        ProjectileHitResult hit = ProjectileHitResult.Tank(firstTankWithId7);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { firstTankWithId7, secondTankWithId7 });

        Assert.True(outcome.HasUpdatedTank);
        Assert.Equal(new PlayerSlot(0), outcome.UpdatedTank!.Value.OwnerSlot);
        Assert.Equal(80, outcome.UpdatedTank!.Value.CurrentHitPoints);
    }

    [Fact]
    public void Resolve_TankHit_PreservesUpdatedTankNonHpFields()
    {
        ProjectileState projectile = CreateProjectile();
        FixedVec2 tankPosition = FixedVec2.FromInts(33, 44);
        TankState tank = CreateTank(id: 5, ownerSlot: 1, position: tankPosition);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank);

        ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
            projectile,
            hit,
            new[] { tank });

        Assert.True(outcome.HasUpdatedTank);
        TankState updated = outcome.UpdatedTank!.Value;
        Assert.Equal(tank.Id, updated.Id);
        Assert.Equal(tank.OwnerSlot, updated.OwnerSlot);
        Assert.Same(tank.Definition, updated.Definition);
        Assert.Equal(tank.Movement, updated.Movement);
        Assert.Equal(tank.BodyRotation, updated.BodyRotation);
        Assert.Equal(tank.TurretRotation, updated.TurretRotation);
    }

    [Fact]
    public void Resolve_TankHit_ThrowsWhenTankIdNotFound()
    {
        ProjectileState projectile = CreateProjectile();
        TankState missingTank = CreateTank(id: 99);
        ProjectileHitResult hit = ProjectileHitResult.Tank(missingTank);
        TankState[] tanks = new[] { CreateTank(id: 1), CreateTank(id: 2) };

        Assert.Throws<InvalidOperationException>(
            () => ProjectileHitResolution.Resolve(projectile, hit, tanks));
    }

    // -------------------- Purity --------------------

    [Fact]
    public void Resolve_DoesNotMutateOriginalProjectileOrTank()
    {
        ProjectileState projectile = CreateProjectile();
        TankState tank = CreateTank(id: 1);
        ProjectileHitResult hit = ProjectileHitResult.Tank(tank);

        ProjectileState projectileSnapshot = projectile;
        TankState tankSnapshot = tank;

        ProjectileHitResolution.Resolve(projectile, hit, new[] { tank });

        Assert.Equal(projectileSnapshot, projectile);
        Assert.Equal(tankSnapshot, tank);
    }
}
