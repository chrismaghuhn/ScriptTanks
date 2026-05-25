using System;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
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

public sealed class MatchStateProjectileHitSystemTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null)
    {
        MovementState movement = new MovementState(
            position ?? FixedVec2.FromInts(10 + id, 20),
            FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileDefinition CreateProjectileDefinition(
        int rawDamage = 25,
        Fixed? radius = null)
        => new ProjectileDefinition(
            rawDamage,
            Fixed.FromInt(1),
            Fixed.FromInt(40),
            radius ?? Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(
        int id,
        FixedVec2? position = null,
        bool isActive = true,
        ProjectileDefinition? definition = null,
        int ownerTankId = 99,
        SimTick? spawnTick = null)
    {
        ProjectileDefinition projectileDefinition =
            definition ?? CreateProjectileDefinition();

        return new ProjectileState(
            new ProjectileId(id),
            projectileDefinition,
            new TankId(ownerTankId),
            new WeaponSlot(0),
            spawnTick ?? new SimTick(4),
            position ?? FixedVec2.FromInts(50, 50),
            FixedVec2.Zero,
            projectileDefinition.MaxRange,
            isActive);
    }

    private static MatchState CreateOpenState(
        ProjectileState[] projectiles,
        TankState[]? tanks = null)
    {
        TankState[] tankArray = tanks ??
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
                CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
            };

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            tankArray,
            Enumerable.Range(0, tankArray.Length).Select(_ => CreateLoadout()).ToArray(),
            projectiles);
    }

    private static ArenaDefinition CreateSingleWallArena()
        => new ArenaDefinition(
            id: "single_wall_arena",
            displayName: "Single Wall Arena",
            description: "Arena for projectile hit system tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 20)),
            },
            wallBlocks: new[]
            {
                new WallBlock(
                    "wall",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(20, 20),
                        Fixed.FromInt(10),
                        Fixed.FromInt(10))),
            },
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });

    private static MatchState CreateWallState(params ProjectileState[] projectiles)
        => new MatchState(
            CreateSingleWallArena(),
            new SimTick(5),
            new[] { CreateTank(0, 0, FixedVec2.FromInts(10, 20)) },
            new[] { CreateLoadout() },
            projectiles);

    // -------------------- Validation --------------------

    [Fact]
    public void ResolveProjectileHits_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchStateProjectileHitSystem.ResolveProjectileHits(null!));
    }

    // -------------------- Preservation --------------------

    [Fact]
    public void ResolveProjectileHits_PreservesArenaReference()
    {
        MatchState state = CreateOpenState(new[] { CreateProjectile(1) });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Same(state.Arena, updated.Arena);
    }

    [Fact]
    public void ResolveProjectileHits_PreservesCurrentTick()
    {
        MatchState state = CreateOpenState(new[] { CreateProjectile(1) });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(state.CurrentTick, updated.CurrentTick);
    }

    [Fact]
    public void ResolveProjectileHits_PreservesLoadouts()
    {
        MatchState state = CreateOpenState(new[] { CreateProjectile(1) });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void ResolveProjectileHits_ReturnsNewMatchStateInstance()
    {
        MatchState state = CreateOpenState(new[] { CreateProjectile(1) });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.NotSame(state, updated);
    }

    // -------------------- None / Inactive --------------------

    [Fact]
    public void ResolveProjectileHits_NoHit_PreservesProjectile()
    {
        ProjectileState projectile = CreateProjectile(1, position: FixedVec2.FromInts(50, 50));
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Single(updated.Projectiles);
        Assert.Equal(projectile, updated.Projectiles[0]);
    }

    [Fact]
    public void ResolveProjectileHits_InactiveProjectile_RemainsUnchanged()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            isActive: false);
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Single(updated.Projectiles);
        Assert.Equal(projectile, updated.Projectiles[0]);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void ResolveProjectileHits_EmptyProjectiles_ReturnsEmptyProjectiles()
    {
        MatchState state = CreateOpenState(Array.Empty<ProjectileState>());

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Empty(updated.Projectiles);
    }

    // -------------------- Wall Hits --------------------

    [Fact]
    public void ResolveProjectileHits_WallHit_DeactivatesProjectile()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(25, 25));
        MatchState state = CreateWallState(projectile);

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.False(updated.Projectiles[0].IsActive);
    }

    [Fact]
    public void ResolveProjectileHits_WallHit_DoesNotDamageTank()
    {
        int initialHp = CreateTank(0, 0, FixedVec2.FromInts(10, 20)).CurrentHitPoints;
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(25, 25));
        MatchState state = CreateWallState(projectile);

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(initialHp, updated.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void ResolveProjectileHits_WallHit_PreservesProjectileOrder()
    {
        ProjectileState p1 = CreateProjectile(1, position: FixedVec2.FromInts(5, 5));
        ProjectileState p2 = CreateProjectile(2, position: FixedVec2.FromInts(25, 25));
        ProjectileState p3 = CreateProjectile(3, position: FixedVec2.FromInts(80, 40));
        MatchState state = CreateWallState(p1, p2, p3);

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(3, updated.Projectiles.Count);
        Assert.Equal(new ProjectileId(1), updated.Projectiles[0].Id);
        Assert.Equal(new ProjectileId(2), updated.Projectiles[1].Id);
        Assert.Equal(new ProjectileId(3), updated.Projectiles[2].Id);
    }

    // -------------------- Tank Hits --------------------

    [Fact]
    public void ResolveProjectileHits_TankHit_DeactivatesProjectile()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20));
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.False(updated.Projectiles[0].IsActive);
    }

    [Fact]
    public void ResolveProjectileHits_TankHit_DamagesTank()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20));
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(80, updated.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void ResolveProjectileHits_TankHit_PreservesOtherTanks()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20));
        TankState tank1Before = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        MatchState state = CreateOpenState(
            new[] { projectile },
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
                tank1Before,
            });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(tank1Before, updated.Tanks[1]);
    }

    [Fact]
    public void ResolveProjectileHits_TankHit_PreservesTankOrder()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20));
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(new TankId(0), updated.Tanks[0].Id);
        Assert.Equal(new TankId(1), updated.Tanks[1].Id);
    }

    [Fact]
    public void ResolveProjectileHits_TankHit_UsesProjectileRawDamage()
    {
        ProjectileDefinition definition = CreateProjectileDefinition(rawDamage: 10);
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            definition: definition);
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(92, updated.Tanks[0].CurrentHitPoints);
    }

    // -------------------- Multiple Projectiles --------------------

    [Fact]
    public void ResolveProjectileHits_MultipleProjectiles_DamageAccumulatesInOrder()
    {
        ProjectileState p1 = CreateProjectile(1, position: FixedVec2.FromInts(10, 20));
        ProjectileState p2 = CreateProjectile(2, position: FixedVec2.FromInts(10, 20));
        MatchState state = CreateOpenState(new[] { p1, p2 });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(60, updated.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void ResolveProjectileHits_MultipleProjectiles_ProjectileOrderPreserved()
    {
        ProjectileState p3 = CreateProjectile(3, position: FixedVec2.FromInts(50, 50));
        ProjectileState p4 = CreateProjectile(4, position: FixedVec2.FromInts(51, 50));
        ProjectileState p5 = CreateProjectile(5, position: FixedVec2.FromInts(52, 50));
        MatchState state = CreateOpenState(new[] { p3, p4, p5 });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(new ProjectileId(3), updated.Projectiles[0].Id);
        Assert.Equal(new ProjectileId(4), updated.Projectiles[1].Id);
        Assert.Equal(new ProjectileId(5), updated.Projectiles[2].Id);
    }

    [Fact]
    public void ResolveProjectileHits_MultipleProjectiles_EachHitProjectileDeactivated()
    {
        ProjectileState p1 = CreateProjectile(1, position: FixedVec2.FromInts(10, 20));
        ProjectileState p2 = CreateProjectile(2, position: FixedVec2.FromInts(10, 20));
        MatchState state = CreateOpenState(new[] { p1, p2 });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.False(updated.Projectiles[0].IsActive);
        Assert.False(updated.Projectiles[1].IsActive);
    }

    // -------------------- Spawn-Tick Owner Policy / Purity --------------------

    [Fact]
    public void ResolveProjectileHits_SkipsOwnerOnSpawnTick_WhenOverlapping()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            ownerTankId: 0,
            spawnTick: new SimTick(5));
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
        Assert.True(updated.Projectiles[0].IsActive);
    }

    [Fact]
    public void ResolveProjectileHits_HitsOwner_WhenSpawnTickBeforeCurrentTick()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            ownerTankId: 0,
            spawnTick: new SimTick(4));
        MatchState state = CreateOpenState(new[] { projectile });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(80, updated.Tanks[0].CurrentHitPoints);
        Assert.False(updated.Projectiles[0].IsActive);
    }

    [Fact]
    public void ResolveProjectileHits_HitsEnemy_OnSpawnTick_WhenOwnerFiltered()
    {
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20),
            ownerTankId: 0,
            spawnTick: new SimTick(5));
        MatchState state = CreateOpenState(
            new[] { projectile },
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
                CreateTank(1, 1, FixedVec2.FromInts(10, 20)),
            });

        MatchState updated = MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
        Assert.Equal(80, updated.Tanks[1].CurrentHitPoints);
        Assert.False(updated.Projectiles[0].IsActive);
    }

    [Fact]
    public void ResolveProjectileHits_DoesNotMutateOriginalState()
    {
        ProjectileState originalProjectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(10, 20));
        TankState tank0Before = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState tank1Before = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        MatchState state = CreateOpenState(
            new[] { originalProjectile },
            new[] { tank0Before, tank1Before });

        MatchStateProjectileHitSystem.ResolveProjectileHits(state);

        Assert.Single(state.Projectiles);
        Assert.Equal(originalProjectile, state.Projectiles[0]);
        Assert.Equal(tank0Before, state.Tanks[0]);
        Assert.Equal(tank1Before, state.Tanks[1]);
    }
}
