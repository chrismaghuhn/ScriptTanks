using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Projectiles;

public sealed class ProjectileMovementTests
{
    private static ProjectileDefinition CreateDefinition(
        int rawDamage = 25,
        Fixed? speedPerTick = null,
        Fixed? maxRange = null,
        Fixed? radius = null)
    {
        return new ProjectileDefinition(
            rawDamage,
            speedPerTick ?? Fixed.FromInt(1),
            maxRange ?? Fixed.FromInt(40),
            radius ?? Fixed.FromRatio(1, 4));
    }

    private static ProjectileState CreateProjectile(
        FixedVec2? position = null,
        FixedVec2? velocityPerTick = null,
        Fixed? remainingRange = null,
        bool isActive = true,
        ProjectileDefinition? definition = null,
        SimTick? spawnTick = null)
    {
        ProjectileDefinition projectileDefinition = definition ?? CreateDefinition();

        return new ProjectileState(
            id: new ProjectileId(3),
            definition: projectileDefinition,
            ownerTankId: new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            spawnTick: spawnTick ?? new SimTick(10),
            position: position ?? FixedVec2.FromInts(10, 20),
            velocityPerTick: velocityPerTick ?? FixedVec2.FromInts(1, 0),
            remainingRange: remainingRange ?? Fixed.FromInt(40),
            isActive: isActive);
    }

    // -------------------- Active + Identity --------------------

    [Fact]
    public void Step_InactiveProjectile_ReturnsUnchangedState()
    {
        ProjectileState projectile = CreateProjectile(isActive: false);

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(projectile, result);
    }

    [Fact]
    public void Step_ActiveProjectile_AdvancesPositionByVelocityPerTick()
    {
        ProjectileState projectile = CreateProjectile(
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(FixedVec2.FromInts(11, 20), result.Position);
    }

    [Fact]
    public void Step_PreservesSpawnTick()
    {
        ProjectileState projectile = CreateProjectile(spawnTick: new SimTick(7));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(new SimTick(7), result.SpawnTick);
    }

    [Fact]
    public void Step_ActiveProjectile_ReducesRemainingRangeByDefinitionSpeedPerTick()
    {
        ProjectileState projectile = CreateProjectile(
            remainingRange: Fixed.FromInt(40),
            definition: CreateDefinition(speedPerTick: Fixed.FromInt(1)));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(Fixed.FromInt(39), result.RemainingRange);
    }

    [Fact]
    public void Step_ActiveProjectile_PreservesId()
    {
        ProjectileState projectile = CreateProjectile();

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(projectile.Id, result.Id);
    }

    [Fact]
    public void Step_ActiveProjectile_PreservesDefinitionReference()
    {
        ProjectileDefinition definition = CreateDefinition();
        ProjectileState projectile = CreateProjectile(definition: definition);

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Same(definition, result.Definition);
    }

    [Fact]
    public void Step_ActiveProjectile_PreservesOwnerTankId()
    {
        ProjectileState projectile = CreateProjectile();

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(projectile.OwnerTankId, result.OwnerTankId);
    }

    [Fact]
    public void Step_ActiveProjectile_PreservesOwnerWeaponSlot()
    {
        ProjectileState projectile = CreateProjectile();

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(projectile.OwnerWeaponSlot, result.OwnerWeaponSlot);
    }

    [Fact]
    public void Step_ActiveProjectile_PreservesVelocityPerTick()
    {
        FixedVec2 velocity = FixedVec2.FromInts(2, -3);
        ProjectileState projectile = CreateProjectile(velocityPerTick: velocity);

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(velocity, result.VelocityPerTick);
    }

    // -------------------- Range / Lifecycle --------------------

    [Fact]
    public void Step_RemainingRangeStillPositive_KeepsProjectileActive()
    {
        ProjectileState projectile = CreateProjectile(
            remainingRange: Fixed.FromInt(5),
            definition: CreateDefinition(speedPerTick: Fixed.FromInt(1)));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(Fixed.FromInt(4), result.RemainingRange);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void Step_RemainingRangeExactlyZero_DeactivatesProjectile()
    {
        ProjectileState projectile = CreateProjectile(
            remainingRange: Fixed.FromInt(1),
            definition: CreateDefinition(speedPerTick: Fixed.FromInt(1)));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(Fixed.Zero, result.RemainingRange);
        Assert.False(result.IsActive);
    }

    [Fact]
    public void Step_RemainingRangeBelowZero_ClampsToZeroAndDeactivatesProjectile()
    {
        ProjectileState projectile = CreateProjectile(
            remainingRange: Fixed.FromInt(1),
            definition: CreateDefinition(speedPerTick: Fixed.FromInt(2)));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(Fixed.Zero, result.RemainingRange);
        Assert.False(result.IsActive);
    }

    // -------------------- Velocity Edge Cases --------------------

    [Fact]
    public void Step_ZeroVelocity_DoesNotMovePosition()
    {
        FixedVec2 startPosition = FixedVec2.FromInts(10, 20);
        ProjectileState projectile = CreateProjectile(
            position: startPosition,
            velocityPerTick: FixedVec2.Zero);

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(startPosition, result.Position);
    }

    [Fact]
    public void Step_ZeroVelocity_StillReducesRemainingRange()
    {
        ProjectileState projectile = CreateProjectile(
            velocityPerTick: FixedVec2.Zero,
            remainingRange: Fixed.FromInt(10),
            definition: CreateDefinition(speedPerTick: Fixed.FromInt(3)));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(Fixed.FromInt(7), result.RemainingRange);
    }

    [Fact]
    public void Step_NegativeVelocity_MovesBackward()
    {
        ProjectileState projectile = CreateProjectile(
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(-1, -2));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(FixedVec2.FromInts(9, 18), result.Position);
    }

    [Fact]
    public void Step_UsesDefinitionSpeed_NotVelocityMagnitude()
    {
        ProjectileState projectile = CreateProjectile(
            velocityPerTick: FixedVec2.FromInts(10, 0),
            remainingRange: Fixed.FromInt(10),
            definition: CreateDefinition(speedPerTick: Fixed.FromInt(2)));

        ProjectileState result = ProjectileMovement.Step(projectile);

        Assert.Equal(Fixed.FromInt(8), result.RemainingRange);
        Assert.True(result.IsActive);
    }

    // -------------------- Immutability --------------------

    [Fact]
    public void Step_DoesNotMutateOriginalState()
    {
        FixedVec2 originalPosition = FixedVec2.FromInts(10, 20);
        FixedVec2 originalVelocity = FixedVec2.FromInts(1, 0);
        Fixed originalRange = Fixed.FromInt(40);

        ProjectileState projectile = CreateProjectile(
            position: originalPosition,
            velocityPerTick: originalVelocity,
            remainingRange: originalRange,
            isActive: true);

        ProjectileMovement.Step(projectile);

        Assert.Equal(originalPosition, projectile.Position);
        Assert.Equal(originalVelocity, projectile.VelocityPerTick);
        Assert.Equal(originalRange, projectile.RemainingRange);
        Assert.True(projectile.IsActive);
    }
}
