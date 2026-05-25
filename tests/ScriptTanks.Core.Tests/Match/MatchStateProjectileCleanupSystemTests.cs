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

public sealed class MatchStateProjectileCleanupSystemTests
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

    private static ProjectileDefinition CreateProjectileDefinition()
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(int id, bool isActive)
    {
        ProjectileDefinition definition = CreateProjectileDefinition();

        return new ProjectileState(
            id: new ProjectileId(id),
            definition: definition,
            ownerTankId: new TankId(0),
            ownerWeaponSlot: new WeaponSlot(0),
            spawnTick: new SimTick(0),
            position: FixedVec2.FromInts(10 + id, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: definition.MaxRange,
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
    public void RemoveInactiveProjectiles_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(null!));
    }

    // -------------------- Preservation --------------------

    [Fact]
    public void RemoveInactiveProjectiles_PreservesArenaReference()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: true),
            CreateProjectile(2, isActive: false));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Same(state.Arena, updated.Arena);
    }

    [Fact]
    public void RemoveInactiveProjectiles_PreservesCurrentTick()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: true),
            CreateProjectile(2, isActive: false));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Equal(state.CurrentTick, updated.CurrentTick);
    }

    [Fact]
    public void RemoveInactiveProjectiles_PreservesTanks()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: true),
            CreateProjectile(2, isActive: false));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Equal(state.Tanks.Count, updated.Tanks.Count);
        Assert.Equal(state.Tanks[0], updated.Tanks[0]);
        Assert.Equal(state.Tanks[1], updated.Tanks[1]);
    }

    [Fact]
    public void RemoveInactiveProjectiles_PreservesLoadouts()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: true),
            CreateProjectile(2, isActive: false));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void RemoveInactiveProjectiles_ReturnsNewMatchStateInstance()
    {
        MatchState state = CreateState(CreateProjectile(1, isActive: true));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.NotSame(state, updated);
    }

    // -------------------- Cleanup Behavior --------------------

    [Fact]
    public void RemoveInactiveProjectiles_KeepsActiveProjectiles()
    {
        ProjectileState active = CreateProjectile(1, isActive: true);
        MatchState state = CreateState(active);

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        ProjectileState remaining = Assert.Single(updated.Projectiles);
        Assert.Equal(active, remaining);
    }

    [Fact]
    public void RemoveInactiveProjectiles_RemovesInactiveProjectiles()
    {
        MatchState state = CreateState(CreateProjectile(1, isActive: false));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Empty(updated.Projectiles);
    }

    [Fact]
    public void RemoveInactiveProjectiles_PreservesActiveProjectileOrder()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: true),
            CreateProjectile(2, isActive: false),
            CreateProjectile(3, isActive: true),
            CreateProjectile(4, isActive: true));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Equal(3, updated.Projectiles.Count);
        Assert.Equal(new ProjectileId(1), updated.Projectiles[0].Id);
        Assert.Equal(new ProjectileId(3), updated.Projectiles[1].Id);
        Assert.Equal(new ProjectileId(4), updated.Projectiles[2].Id);
    }

    [Fact]
    public void RemoveInactiveProjectiles_AllActive_KeepsAllProjectiles()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: true),
            CreateProjectile(2, isActive: true),
            CreateProjectile(3, isActive: true));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Equal(3, updated.Projectiles.Count);
        Assert.Equal(new ProjectileId(1), updated.Projectiles[0].Id);
        Assert.Equal(new ProjectileId(2), updated.Projectiles[1].Id);
        Assert.Equal(new ProjectileId(3), updated.Projectiles[2].Id);
    }

    [Fact]
    public void RemoveInactiveProjectiles_AllInactive_ReturnsEmptyProjectiles()
    {
        MatchState state = CreateState(
            CreateProjectile(1, isActive: false),
            CreateProjectile(2, isActive: false),
            CreateProjectile(3, isActive: false));

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Empty(updated.Projectiles);
    }

    [Fact]
    public void RemoveInactiveProjectiles_EmptyProjectiles_RemainsEmpty()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Empty(updated.Projectiles);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void RemoveInactiveProjectiles_DoesNotMutateOriginalState()
    {
        ProjectileState p1 = CreateProjectile(1, isActive: true);
        ProjectileState p2 = CreateProjectile(2, isActive: false);
        ProjectileState p3 = CreateProjectile(3, isActive: true);
        MatchState state = CreateState(p1, p2, p3);
        ArenaDefinition originalArena = state.Arena;
        SimTick originalTick = state.CurrentTick;

        MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(state);

        Assert.Equal(3, state.Projectiles.Count);
        Assert.Equal(p1, state.Projectiles[0]);
        Assert.Equal(p2, state.Projectiles[1]);
        Assert.Equal(p3, state.Projectiles[2]);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTick, state.CurrentTick);
    }
}
