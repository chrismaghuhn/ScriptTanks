using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class ProjectileCombatLogFactoryTests
{
    private static ProjectileDefinition CreateProjectileDefinition()
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile()
    {
        ProjectileDefinition definition = CreateProjectileDefinition();

        return new ProjectileState(
            new ProjectileId(123),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            new SimTick(0),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            definition.MaxRange,
            isActive: true);
    }

    private static TankState CreateTank(int id)
    {
        MovementState movement = new MovementState(
            FixedVec2.FromInts(10, 20),
            FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(id),
            TankCatalog.BasicTank,
            movement,
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static WallBlock CreateWall(string id)
        => new WallBlock(
            id,
            FixedRect.FromMinSize(
                FixedVec2.FromInts(0, 0),
                Fixed.FromInt(5),
                Fixed.FromInt(5)));

    private static ProjectileHitResult CreateHitResultTank(int tankId)
        => ProjectileHitResult.Tank(CreateTank(tankId));

    private static ProjectileHitResult CreateHitResultWall(string id)
        => ProjectileHitResult.Wall(CreateWall(id));

    [Fact]
    public void CreateProjectileSpawnedLog_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateProjectileSpawnedLog(
            new SimTick(5),
            CreateProjectile());

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Projectile, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileSpawned, log.Entries[0].EventType);
        Assert.Equal(
            "Projectile 123 spawned from tank 0.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateProjectileHitLog_WhenTankHit_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateProjectileHitLog(
            new SimTick(5),
            CreateProjectile(),
            CreateHitResultTank(1));

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Projectile, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileHit, log.Entries[0].EventType);
        Assert.Equal(
            "Projectile 123 hit tank 1.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateProjectileHitLog_WhenWallHit_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateProjectileHitLog(
            new SimTick(5),
            CreateProjectile(),
            CreateHitResultWall("center_wall"));

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Projectile, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileHit, log.Entries[0].EventType);
        Assert.Equal(
            "Projectile 123 hit wall center_wall.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateProjectileHitLog_WhenNoneHit_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateProjectileHitLog(
            new SimTick(5),
            CreateProjectile(),
            ProjectileHitResult.None);

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Projectile, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileHit, log.Entries[0].EventType);
        Assert.Equal(
            "Projectile 123 hit nothing.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateProjectileExpiredLog_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateProjectileExpiredLog(
            new SimTick(5),
            CreateProjectile());

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Projectile, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileExpired, log.Entries[0].EventType);
        Assert.Equal(
            "Projectile 123 expired.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateProjectileCleanedUpLog_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateProjectileCleanedUpLog(
            new SimTick(5),
            CreateProjectile());

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Projectile, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileCleanedUp, log.Entries[0].EventType);
        Assert.Equal(
            "Projectile 123 was cleaned up.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateDamageDealtLog_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateDamageDealtLog(
            new SimTick(5),
            CreateTank(1),
            damage: 20);

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Damage, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.DamageDealt, log.Entries[0].EventType);
        Assert.Equal(
            "Tank 1 took 20 damage.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateTankDestroyedLog_ReturnsExpectedEntry()
    {
        CombatLog log = ProjectileCombatLogFactory.CreateTankDestroyedLog(
            new SimTick(5),
            CreateTank(1));

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogCategory.Tank, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.TankDestroyed, log.Entries[0].EventType);
        Assert.Equal(
            "Tank 1 was destroyed.",
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateLogs_UseProvidedTick()
    {
        SimTick tick = new SimTick(42);
        ProjectileState projectile = CreateProjectile();
        TankState tank = CreateTank(1);

        CombatLog spawned = ProjectileCombatLogFactory.CreateProjectileSpawnedLog(tick, projectile);
        CombatLog hit = ProjectileCombatLogFactory.CreateProjectileHitLog(
            tick,
            projectile,
            CreateHitResultTank(1));
        CombatLog expired = ProjectileCombatLogFactory.CreateProjectileExpiredLog(tick, projectile);
        CombatLog cleanedUp = ProjectileCombatLogFactory.CreateProjectileCleanedUpLog(tick, projectile);
        CombatLog damage = ProjectileCombatLogFactory.CreateDamageDealtLog(tick, tank, damage: 10);
        CombatLog destroyed = ProjectileCombatLogFactory.CreateTankDestroyedLog(tick, tank);

        Assert.Equal(tick, spawned.Entries[0].Tick);
        Assert.Equal(tick, hit.Entries[0].Tick);
        Assert.Equal(tick, expired.Entries[0].Tick);
        Assert.Equal(tick, cleanedUp.Entries[0].Tick);
        Assert.Equal(tick, damage.Entries[0].Tick);
        Assert.Equal(tick, destroyed.Entries[0].Tick);
    }

    [Fact]
    public void CreateProjectileHitLog_RejectsNullHit()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => ProjectileCombatLogFactory.CreateProjectileHitLog(
                new SimTick(5),
                CreateProjectile(),
                null!));
        Assert.Equal("hit", ex.ParamName);
    }
}
