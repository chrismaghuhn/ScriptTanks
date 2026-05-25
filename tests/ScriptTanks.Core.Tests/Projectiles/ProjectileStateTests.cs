using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Projectiles;

public sealed class ProjectileStateTests
{
    private static ProjectileDefinition CreateDefinition()
    {
        return new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(5),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));
    }

    private static ProjectileDefinition CreateDefinitionClone()
    {
        return new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(5),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));
    }

    private static ProjectileState CreateState(
        ProjectileId? id = null,
        ProjectileDefinition? definition = null,
        TankId? ownerTankId = null,
        WeaponSlot? ownerWeaponSlot = null,
        SimTick? spawnTick = null,
        FixedVec2? position = null,
        FixedVec2? velocityPerTick = null,
        Fixed? remainingRange = null,
        bool isActive = true)
    {
        return new ProjectileState(
            id ?? new ProjectileId(3),
            definition ?? CreateDefinition(),
            ownerTankId ?? new TankId(7),
            ownerWeaponSlot ?? new WeaponSlot(1),
            spawnTick ?? new SimTick(10),
            position ?? new FixedVec2(Fixed.FromInt(2), Fixed.FromInt(4)),
            velocityPerTick ?? new FixedVec2(Fixed.FromInt(1), Fixed.Zero),
            remainingRange ?? Fixed.FromInt(40),
            isActive);
    }

    // -------------------- Block A: Constructor --------------------

    [Fact]
    public void Constructor_PreservesId()
    {
        ProjectileState state = CreateState(id: new ProjectileId(42));

        Assert.Equal(new ProjectileId(42), state.Id);
    }

    [Fact]
    public void Constructor_PreservesDefinitionReference()
    {
        ProjectileDefinition definition = CreateDefinition();

        ProjectileState state = CreateState(definition: definition);

        Assert.Same(definition, state.Definition);
    }

    [Fact]
    public void Constructor_PreservesOwnerTankId()
    {
        ProjectileState state = CreateState(ownerTankId: new TankId(11));

        Assert.Equal(new TankId(11), state.OwnerTankId);
    }

    [Fact]
    public void Constructor_PreservesOwnerWeaponSlot()
    {
        ProjectileState state = CreateState(ownerWeaponSlot: new WeaponSlot(2));

        Assert.Equal(new WeaponSlot(2), state.OwnerWeaponSlot);
    }

    [Fact]
    public void Constructor_PreservesSpawnTick()
    {
        ProjectileState state = CreateState(spawnTick: new SimTick(42));

        Assert.Equal(new SimTick(42), state.SpawnTick);
    }

    [Fact]
    public void Constructor_PreservesPosition()
    {
        FixedVec2 position = new FixedVec2(Fixed.FromInt(9), Fixed.FromInt(13));

        ProjectileState state = CreateState(position: position);

        Assert.Equal(position, state.Position);
    }

    [Fact]
    public void Constructor_PreservesVelocityPerTick()
    {
        FixedVec2 velocity = new FixedVec2(Fixed.FromInt(-2), Fixed.FromInt(3));

        ProjectileState state = CreateState(velocityPerTick: velocity);

        Assert.Equal(velocity, state.VelocityPerTick);
    }

    [Fact]
    public void Constructor_PreservesRemainingRange()
    {
        Fixed range = Fixed.FromInt(17);

        ProjectileState state = CreateState(remainingRange: range);

        Assert.Equal(range, state.RemainingRange);
    }

    [Fact]
    public void Constructor_PreservesIsActive()
    {
        ProjectileState active = CreateState(isActive: true);
        ProjectileState inactive = CreateState(isActive: false);

        Assert.True(active.IsActive);
        Assert.False(inactive.IsActive);
    }

    [Fact]
    public void Constructor_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ProjectileState(
                new ProjectileId(1),
                null!,
                new TankId(7),
                new WeaponSlot(0),
                new SimTick(0),
                FixedVec2.Zero,
                FixedVec2.Zero,
                Fixed.FromInt(10),
                isActive: true));
    }

    [Fact]
    public void Constructor_RejectsNegativeRemainingRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateState(remainingRange: Fixed.FromInt(-1)));
    }

    [Fact]
    public void Constructor_AcceptsZeroRemainingRange()
    {
        ProjectileState state = CreateState(remainingRange: Fixed.Zero);

        Assert.Equal(Fixed.Zero, state.RemainingRange);
    }

    [Fact]
    public void Constructor_AcceptsInactiveProjectile()
    {
        ProjectileState state = CreateState(isActive: false);

        Assert.False(state.IsActive);
    }

    [Fact]
    public void Constructor_AcceptsZeroVelocity()
    {
        ProjectileState state = CreateState(velocityPerTick: FixedVec2.Zero);

        Assert.Equal(FixedVec2.Zero, state.VelocityPerTick);
    }

    // -------------------- Block B: With Methods --------------------

    [Fact]
    public void WithPosition_ChangesOnlyPosition()
    {
        ProjectileState original = CreateState();
        FixedVec2 newPosition = new FixedVec2(Fixed.FromInt(99), Fixed.FromInt(-3));

        ProjectileState updated = original.WithPosition(newPosition);

        Assert.Equal(newPosition, updated.Position);
        Assert.Equal(original.Id, updated.Id);
        Assert.Same(original.Definition, updated.Definition);
        Assert.Equal(original.OwnerTankId, updated.OwnerTankId);
        Assert.Equal(original.OwnerWeaponSlot, updated.OwnerWeaponSlot);
        Assert.Equal(original.SpawnTick, updated.SpawnTick);
        Assert.Equal(original.VelocityPerTick, updated.VelocityPerTick);
        Assert.Equal(original.RemainingRange, updated.RemainingRange);
        Assert.Equal(original.IsActive, updated.IsActive);
    }

    [Fact]
    public void WithVelocityPerTick_ChangesOnlyVelocity()
    {
        ProjectileState original = CreateState();
        FixedVec2 newVelocity = new FixedVec2(Fixed.FromInt(-7), Fixed.FromInt(7));

        ProjectileState updated = original.WithVelocityPerTick(newVelocity);

        Assert.Equal(newVelocity, updated.VelocityPerTick);
        Assert.Equal(original.Id, updated.Id);
        Assert.Same(original.Definition, updated.Definition);
        Assert.Equal(original.OwnerTankId, updated.OwnerTankId);
        Assert.Equal(original.OwnerWeaponSlot, updated.OwnerWeaponSlot);
        Assert.Equal(original.SpawnTick, updated.SpawnTick);
        Assert.Equal(original.Position, updated.Position);
        Assert.Equal(original.RemainingRange, updated.RemainingRange);
        Assert.Equal(original.IsActive, updated.IsActive);
    }

    [Fact]
    public void WithRemainingRange_ChangesOnlyRemainingRange()
    {
        ProjectileState original = CreateState();
        Fixed newRange = Fixed.FromInt(7);

        ProjectileState updated = original.WithRemainingRange(newRange);

        Assert.Equal(newRange, updated.RemainingRange);
        Assert.Equal(original.Id, updated.Id);
        Assert.Same(original.Definition, updated.Definition);
        Assert.Equal(original.OwnerTankId, updated.OwnerTankId);
        Assert.Equal(original.OwnerWeaponSlot, updated.OwnerWeaponSlot);
        Assert.Equal(original.SpawnTick, updated.SpawnTick);
        Assert.Equal(original.Position, updated.Position);
        Assert.Equal(original.VelocityPerTick, updated.VelocityPerTick);
        Assert.Equal(original.IsActive, updated.IsActive);
    }

    [Fact]
    public void WithIsActive_ChangesOnlyIsActive()
    {
        ProjectileState original = CreateState(isActive: true);

        ProjectileState updated = original.WithIsActive(false);

        Assert.False(updated.IsActive);
        Assert.Equal(original.Id, updated.Id);
        Assert.Same(original.Definition, updated.Definition);
        Assert.Equal(original.OwnerTankId, updated.OwnerTankId);
        Assert.Equal(original.OwnerWeaponSlot, updated.OwnerWeaponSlot);
        Assert.Equal(original.SpawnTick, updated.SpawnTick);
        Assert.Equal(original.Position, updated.Position);
        Assert.Equal(original.VelocityPerTick, updated.VelocityPerTick);
        Assert.Equal(original.RemainingRange, updated.RemainingRange);
    }

    [Fact]
    public void WithRemainingRange_AcceptsZero()
    {
        ProjectileState original = CreateState();

        ProjectileState updated = original.WithRemainingRange(Fixed.Zero);

        Assert.Equal(Fixed.Zero, updated.RemainingRange);
    }

    [Fact]
    public void WithRemainingRange_RejectsNegative()
    {
        ProjectileState original = CreateState();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => original.WithRemainingRange(Fixed.FromInt(-1)));
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        ProjectileState original = CreateState(isActive: true);

        ProjectileState deactivated = original.Deactivate();

        Assert.False(deactivated.IsActive);
    }

    [Fact]
    public void Deactivate_PreservesAllOtherFields()
    {
        ProjectileState original = CreateState(isActive: true);

        ProjectileState deactivated = original.Deactivate();

        Assert.Equal(original.Id, deactivated.Id);
        Assert.Same(original.Definition, deactivated.Definition);
        Assert.Equal(original.OwnerTankId, deactivated.OwnerTankId);
        Assert.Equal(original.OwnerWeaponSlot, deactivated.OwnerWeaponSlot);
        Assert.Equal(original.SpawnTick, deactivated.SpawnTick);
        Assert.Equal(original.Position, deactivated.Position);
        Assert.Equal(original.VelocityPerTick, deactivated.VelocityPerTick);
        Assert.Equal(original.RemainingRange, deactivated.RemainingRange);
    }

    // -------------------- Block C: Equality --------------------

    [Fact]
    public void Equals_True_ForSameValuesAndDefinitionReference()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared);
        ProjectileState b = CreateState(definition: shared);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
    }

    [Fact]
    public void Equals_False_ForDifferentId()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, id: new ProjectileId(1));
        ProjectileState b = CreateState(definition: shared, id: new ProjectileId(2));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentDefinitionReferenceEvenIfDataIsEqual()
    {
        ProjectileState a = CreateState(definition: CreateDefinition());
        ProjectileState b = CreateState(definition: CreateDefinitionClone());

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentOwnerTankId()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, ownerTankId: new TankId(7));
        ProjectileState b = CreateState(definition: shared, ownerTankId: new TankId(8));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentOwnerWeaponSlot()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, ownerWeaponSlot: new WeaponSlot(0));
        ProjectileState b = CreateState(definition: shared, ownerWeaponSlot: new WeaponSlot(1));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentSpawnTick()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, spawnTick: new SimTick(1));
        ProjectileState b = CreateState(definition: shared, spawnTick: new SimTick(2));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentPosition()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, position: new FixedVec2(Fixed.FromInt(1), Fixed.FromInt(2)));
        ProjectileState b = CreateState(definition: shared, position: new FixedVec2(Fixed.FromInt(3), Fixed.FromInt(2)));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentVelocity()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, velocityPerTick: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));
        ProjectileState b = CreateState(definition: shared, velocityPerTick: new FixedVec2(Fixed.FromInt(2), Fixed.Zero));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentRemainingRange()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, remainingRange: Fixed.FromInt(10));
        ProjectileState b = CreateState(definition: shared, remainingRange: Fixed.FromInt(11));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_False_ForDifferentIsActive()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared, isActive: true);
        ProjectileState b = CreateState(definition: shared, isActive: false);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Object_ReturnsFalse_ForNull()
    {
        ProjectileState state = CreateState();

        Assert.False(state.Equals(null));
    }

    [Fact]
    public void Equals_Object_ReturnsFalse_ForWrongType()
    {
        ProjectileState state = CreateState();

        Assert.False(state.Equals("not-a-projectile"));
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        ProjectileDefinition shared = CreateDefinition();

        ProjectileState a = CreateState(definition: shared);
        ProjectileState b = CreateState(definition: shared);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // -------------------- Block D: ToString --------------------

    [Fact]
    public void ToString_ContainsProjectileId()
    {
        ProjectileState state = CreateState(id: new ProjectileId(3));

        Assert.Contains("3", state.ToString());
    }

    [Fact]
    public void ToString_ContainsOwnerTankId()
    {
        ProjectileState state = CreateState(ownerTankId: new TankId(7));

        Assert.Contains("7", state.ToString());
    }

    [Fact]
    public void ToString_ContainsOwnerWeaponSlot()
    {
        ProjectileState state = CreateState(ownerWeaponSlot: new WeaponSlot(1));

        Assert.Contains("1", state.ToString());
    }

    [Fact]
    public void ToString_ContainsActiveFlag()
    {
        ProjectileState state = CreateState(isActive: true);

        Assert.Contains("True", state.ToString());
    }
}
