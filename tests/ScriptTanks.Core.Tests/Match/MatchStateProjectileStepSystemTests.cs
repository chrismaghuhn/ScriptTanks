using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateProjectileStepSystemTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        MovementState movement = new MovementState(
            FixedVec2.FromInts(10 + id, 20),
            FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileDefinition CreateProjectileDefinition(
        Fixed? speedPerTick = null,
        Fixed? maxRange = null)
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: speedPerTick ?? Fixed.FromInt(1),
            maxRange: maxRange ?? Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(
        int id,
        FixedVec2? position = null,
        FixedVec2? velocityPerTick = null,
        Fixed? remainingRange = null,
        bool isActive = true,
        ProjectileDefinition? definition = null)
    {
        ProjectileDefinition projectileDefinition =
            definition ?? CreateProjectileDefinition();

        return new ProjectileState(
            id: new ProjectileId(id),
            definition: projectileDefinition,
            ownerTankId: new TankId(0),
            ownerWeaponSlot: new WeaponSlot(0),
            spawnTick: new SimTick(0),
            position: position ?? FixedVec2.FromInts(10, 20),
            velocityPerTick: velocityPerTick ?? FixedVec2.FromInts(1, 0),
            remainingRange: remainingRange ?? Fixed.FromInt(40),
            isActive: isActive);
    }

    private static MatchState CreateState(params ProjectileState[] projectiles)
        => new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0), CreateTank(1, 1) },
            new[] { CreateLoadout(), CreateLoadout() },
            projectiles);

    // -------------------- Validation --------------------

    [Fact]
    public void StepProjectiles_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchStateProjectileStepSystem.StepProjectiles(null!));
    }

    // -------------------- Preservation --------------------

    [Fact]
    public void StepProjectiles_PreservesArenaReference()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Same(state.Arena, updated.Arena);
    }

    [Fact]
    public void StepProjectiles_PreservesCurrentTick()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(state.CurrentTick, updated.CurrentTick);
    }

    [Fact]
    public void StepProjectiles_PreservesTanks()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(state.Tanks.Count, updated.Tanks.Count);
        Assert.Equal(state.Tanks[0], updated.Tanks[0]);
        Assert.Equal(state.Tanks[1], updated.Tanks[1]);
    }

    [Fact]
    public void StepProjectiles_PreservesLoadouts()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void StepProjectiles_ReturnsNewMatchStateInstance()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.NotSame(state, updated);
    }

    // -------------------- Projectile Stepping --------------------

    [Fact]
    public void StepProjectiles_AdvancesSingleActiveProjectile()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0)));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(FixedVec2.FromInts(11, 20), updated.Projectiles[0].Position);
    }

    [Fact]
    public void StepProjectiles_ReducesProjectileRemainingRange()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            remainingRange: Fixed.FromInt(40)));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(Fixed.FromInt(39), updated.Projectiles[0].RemainingRange);
    }

    [Fact]
    public void StepProjectiles_KeepsProjectileOrder()
    {
        MatchState state = CreateState(
            CreateProjectile(1, position: FixedVec2.FromInts(10, 20)),
            CreateProjectile(2, position: FixedVec2.FromInts(20, 20)),
            CreateProjectile(3, position: FixedVec2.FromInts(30, 20)));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(3, updated.Projectiles.Count);
        Assert.Equal(new ProjectileId(1), updated.Projectiles[0].Id);
        Assert.Equal(new ProjectileId(2), updated.Projectiles[1].Id);
        Assert.Equal(new ProjectileId(3), updated.Projectiles[2].Id);
    }

    [Fact]
    public void StepProjectiles_StepsAllProjectiles()
    {
        MatchState state = CreateState(
            CreateProjectile(1, position: FixedVec2.FromInts(10, 20)),
            CreateProjectile(2, position: FixedVec2.FromInts(20, 20)),
            CreateProjectile(3, position: FixedVec2.FromInts(30, 20)));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(FixedVec2.FromInts(11, 20), updated.Projectiles[0].Position);
        Assert.Equal(FixedVec2.FromInts(21, 20), updated.Projectiles[1].Position);
        Assert.Equal(FixedVec2.FromInts(31, 20), updated.Projectiles[2].Position);
    }

    [Fact]
    public void StepProjectiles_DoesNotChangeInactiveProjectile()
    {
        ProjectileState inactive = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(40),
            isActive: false);
        MatchState state = CreateState(inactive);

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(inactive, updated.Projectiles[0]);
    }

    [Fact]
    public void StepProjectiles_DeactivatesProjectileWhenRangeReachesZero()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            remainingRange: Fixed.FromInt(1),
            definition: CreateProjectileDefinition(speedPerTick: Fixed.FromInt(1))));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(Fixed.Zero, updated.Projectiles[0].RemainingRange);
        Assert.False(updated.Projectiles[0].IsActive);
    }

    [Fact]
    public void StepProjectiles_ClampsProjectileRangeBelowZero()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            remainingRange: Fixed.FromInt(1),
            definition: CreateProjectileDefinition(speedPerTick: Fixed.FromInt(2))));

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Equal(Fixed.Zero, updated.Projectiles[0].RemainingRange);
        Assert.False(updated.Projectiles[0].IsActive);
    }

    // -------------------- Empty / Purity --------------------

    [Fact]
    public void StepProjectiles_AcceptsEmptyProjectileList()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Empty(updated.Projectiles);
    }

    [Fact]
    public void StepProjectiles_DoesNotMutateOriginalState()
    {
        ProjectileState originalProjectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(40));
        MatchState state = CreateState(originalProjectile);
        ArenaDefinition originalArena = state.Arena;
        SimTick originalTick = state.CurrentTick;

        MatchStateProjectileStepSystem.StepProjectiles(state);

        Assert.Single(state.Projectiles);
        Assert.Equal(originalProjectile, state.Projectiles[0]);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTick, state.CurrentTick);
    }
}
