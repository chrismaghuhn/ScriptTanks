using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Projectiles;

public sealed class ProjectileSpawnFactoryTests
{
    private static ProjectileDefinition CreateDefinition()
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile()
        => ProjectileSpawnFactory.Create(
            projectileId: new ProjectileId(3),
            definition: CreateDefinition(),
            ownerTankId: new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            spawnTick: new SimTick(10));

    // -------------------- Constructor / Preservation --------------------

    [Fact]
    public void Create_PreservesProjectileId()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Equal(new ProjectileId(3), projectile.Id);
    }

    [Fact]
    public void Create_PreservesDefinitionReference()
    {
        ProjectileDefinition definition = CreateDefinition();

        ProjectileState projectile = ProjectileSpawnFactory.Create(
            projectileId: new ProjectileId(3),
            definition: definition,
            ownerTankId: new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            spawnTick: new SimTick(10));

        Assert.Same(definition, projectile.Definition);
    }

    [Fact]
    public void Create_PreservesOwnerTankId()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Equal(new TankId(7), projectile.OwnerTankId);
    }

    [Fact]
    public void Create_PreservesOwnerWeaponSlot()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Equal(new WeaponSlot(1), projectile.OwnerWeaponSlot);
    }

    [Fact]
    public void Create_SetsSpawnTick()
    {
        ProjectileState projectile = ProjectileSpawnFactory.Create(
            projectileId: new ProjectileId(3),
            definition: CreateDefinition(),
            ownerTankId: new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            spawnTick: new SimTick(42));

        Assert.Equal(new SimTick(42), projectile.SpawnTick);
    }

    [Fact]
    public void Create_PreservesPosition()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Equal(FixedVec2.FromInts(10, 20), projectile.Position);
    }

    [Fact]
    public void Create_PreservesVelocityPerTick()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Equal(FixedVec2.FromInts(1, 0), projectile.VelocityPerTick);
    }

    [Fact]
    public void Create_SetsRemainingRangeToDefinitionMaxRange()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Equal(Fixed.FromInt(40), projectile.RemainingRange);
    }

    [Fact]
    public void Create_SetsIsActiveTrue()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.True(projectile.IsActive);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Create_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(
            () => ProjectileSpawnFactory.Create(
                projectileId: new ProjectileId(3),
                definition: null!,
                ownerTankId: new TankId(7),
                ownerWeaponSlot: new WeaponSlot(1),
                position: FixedVec2.FromInts(10, 20),
                velocityPerTick: FixedVec2.FromInts(1, 0),
                spawnTick: new SimTick(10)));
    }

    // -------------------- Edge Cases --------------------

    [Fact]
    public void Create_AcceptsZeroVelocity()
    {
        ProjectileState projectile = ProjectileSpawnFactory.Create(
            projectileId: new ProjectileId(3),
            definition: CreateDefinition(),
            ownerTankId: new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.Zero,
            spawnTick: new SimTick(10));

        Assert.True(projectile.IsActive);
        Assert.Equal(FixedVec2.Zero, projectile.VelocityPerTick);
    }

    [Fact]
    public void Create_DoesNotRequireVelocityToMatchDefinitionSpeed()
    {
        ProjectileDefinition definition = CreateDefinition();

        ProjectileState projectile = ProjectileSpawnFactory.Create(
            projectileId: new ProjectileId(3),
            definition: definition,
            ownerTankId: new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(10, 0),
            spawnTick: new SimTick(10));

        Assert.True(projectile.IsActive);
        Assert.Equal(FixedVec2.FromInts(10, 0), projectile.VelocityPerTick);
        Assert.Equal(Fixed.FromInt(1), definition.SpeedPerTick);
    }
}
