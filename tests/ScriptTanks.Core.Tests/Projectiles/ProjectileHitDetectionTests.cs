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

public sealed class ProjectileHitDetectionTests
{
    private static ProjectileDefinition CreateProjectileDefinition(Fixed? radius = null)
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: radius ?? Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(
        FixedVec2? position = null,
        bool isActive = true,
        ProjectileDefinition? definition = null,
        TankId? ownerTankId = null,
        SimTick? spawnTick = null)
    {
        return new ProjectileState(
            id: new ProjectileId(3),
            definition: definition ?? CreateProjectileDefinition(),
            ownerTankId: ownerTankId ?? new TankId(7),
            ownerWeaponSlot: new WeaponSlot(1),
            spawnTick: spawnTick ?? new SimTick(5),
            position: position ?? FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(40),
            isActive: isActive);
    }

    private static TankState CreateTank(int id, FixedVec2 position)
    {
        MovementState movement = new MovementState(position, FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(id),
            TankCatalog.BasicTank,
            movement,
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static ArenaDefinition CreateWallArena()
    {
        return new ArenaDefinition(
            id: "hit_test_arena",
            displayName: "Hit Test Arena",
            description: "Arena for projectile hit detection tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
            },
            wallBlocks: new[]
            {
                new WallBlock(
                    "first_wall",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(20, 20),
                        Fixed.FromInt(5),
                        Fixed.FromInt(5))),
                new WallBlock(
                    "second_wall",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(20, 20),
                        Fixed.FromInt(10),
                        Fixed.FromInt(10))),
            },
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    private static ArenaDefinition CreateOpenArena()
    {
        return new ArenaDefinition(
            id: "open_arena",
            displayName: "Open Arena",
            description: "Arena without walls for projectile hit tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
            },
            wallBlocks: Array.Empty<WallBlock>(),
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    // -------------------- Result Model --------------------

    [Fact]
    public void None_HasExpectedValues()
    {
        ProjectileHitResult result = ProjectileHitResult.None;

        Assert.Equal(ProjectileHitKind.None, result.Kind);
        Assert.False(result.HasHit);
        Assert.Null(result.TankId);
        Assert.Null(result.WallBlockId);
    }

    [Fact]
    public void Wall_HasExpectedValues()
    {
        WallBlock wall = new WallBlock(
            "test_wall",
            FixedRect.FromMinSize(
                FixedVec2.FromInts(0, 0),
                Fixed.FromInt(5),
                Fixed.FromInt(5)));

        ProjectileHitResult result = ProjectileHitResult.Wall(wall);

        Assert.Equal(ProjectileHitKind.Wall, result.Kind);
        Assert.True(result.HasHit);
        Assert.Equal("test_wall", result.WallBlockId);
        Assert.Null(result.TankId);
    }

    [Fact]
    public void Tank_HasExpectedValues()
    {
        TankState tank = CreateTank(id: 5, position: FixedVec2.FromInts(10, 10));

        ProjectileHitResult result = ProjectileHitResult.Tank(tank);

        Assert.Equal(ProjectileHitKind.Tank, result.Kind);
        Assert.True(result.HasHit);
        Assert.Equal(new TankId(5), result.TankId);
        Assert.Null(result.WallBlockId);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Detect_RejectsNullArena()
    {
        ProjectileState projectile = CreateProjectile();

        Assert.Throws<ArgumentNullException>(
            () => ProjectileHitDetection.Detect(projectile, null!, Array.Empty<TankState>()));
    }

    [Fact]
    public void Detect_RejectsNullTanks()
    {
        ProjectileState projectile = CreateProjectile();
        ArenaDefinition arena = CreateOpenArena();

        Assert.Throws<ArgumentNullException>(
            () => ProjectileHitDetection.Detect(projectile, arena, null!));
    }

    // -------------------- Inactive / None --------------------

    [Fact]
    public void Detect_InactiveProjectile_ReturnsNone()
    {
        ArenaDefinition arena = CreateWallArena();
        ProjectileState projectile = CreateProjectile(
            position: FixedVec2.FromInts(20, 20),
            isActive: false);
        TankState tank = CreateTank(id: 1, position: FixedVec2.FromInts(20, 20));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { tank });

        Assert.Equal(ProjectileHitKind.None, result.Kind);
        Assert.False(result.HasHit);
    }

    [Fact]
    public void Detect_NoWallOrTankHit_ReturnsNone()
    {
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(50, 50));
        TankState tank = CreateTank(id: 1, position: FixedVec2.FromInts(5, 5));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { tank });

        Assert.Equal(ProjectileHitKind.None, result.Kind);
        Assert.False(result.HasHit);
    }

    // -------------------- Wall --------------------

    [Fact]
    public void Detect_WallHit_ReturnsWallResult()
    {
        ArenaDefinition arena = CreateWallArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(20, 20));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            Array.Empty<TankState>());

        Assert.Equal(ProjectileHitKind.Wall, result.Kind);
        Assert.Equal("first_wall", result.WallBlockId);
    }

    [Fact]
    public void Detect_WallHit_UsesWallOrder()
    {
        ArenaDefinition arena = CreateWallArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(20, 20));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            Array.Empty<TankState>());

        Assert.Equal("first_wall", result.WallBlockId);
    }

    [Fact]
    public void Detect_WallHit_HasPriorityOverTankHit()
    {
        ArenaDefinition arena = CreateWallArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(20, 20));
        TankState tank = CreateTank(id: 1, position: FixedVec2.FromInts(20, 20));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { tank });

        Assert.Equal(ProjectileHitKind.Wall, result.Kind);
        Assert.Equal("first_wall", result.WallBlockId);
        Assert.Null(result.TankId);
    }

    // -------------------- Tank --------------------

    [Fact]
    public void Detect_TankHit_ReturnsTankResult()
    {
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(10, 10));
        TankState tank = CreateTank(id: 1, position: FixedVec2.FromInts(10, 10));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { tank });

        Assert.Equal(ProjectileHitKind.Tank, result.Kind);
        Assert.Equal(new TankId(1), result.TankId);
    }

    [Fact]
    public void Detect_TankHit_UsesTankOrder()
    {
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(10, 10));
        TankState first = CreateTank(id: 1, position: FixedVec2.FromInts(10, 10));
        TankState second = CreateTank(id: 2, position: FixedVec2.FromInts(11, 10));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { first, second });

        Assert.Equal(ProjectileHitKind.Tank, result.Kind);
        Assert.Equal(new TankId(1), result.TankId);
    }

    [Fact]
    public void Detect_TankTangent_CountsAsHit()
    {
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState projectile = CreateProjectile(
            position: FixedVec2.FromInts(13, 10),
            definition: CreateProjectileDefinition(radius: Fixed.FromInt(1)));
        TankState tank = CreateTank(id: 1, position: FixedVec2.FromInts(10, 10));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { tank });

        Assert.Equal(ProjectileHitKind.Tank, result.Kind);
        Assert.Equal(new TankId(1), result.TankId);
    }

    [Fact]
    public void Detect_NonOverlappingTank_ReturnsNone()
    {
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState projectile = CreateProjectile(position: FixedVec2.FromInts(10, 10));
        TankState tank = CreateTank(id: 1, position: FixedVec2.FromInts(50, 10));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { tank });

        Assert.Equal(ProjectileHitKind.None, result.Kind);
    }

    [Fact]
    public void Detect_WhenOwnerInCandidateList_DetectsOwnerRegardlessOfSpawnTick()
    {
        // Owner filtering belongs in MatchStateProjectileHitSystem, not Detect.
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState projectile = CreateProjectile(
            position: FixedVec2.FromInts(10, 10),
            ownerTankId: new TankId(7),
            spawnTick: new SimTick(5));
        TankState ownerTank = CreateTank(id: 7, position: FixedVec2.FromInts(10, 10));

        ProjectileHitResult result = ProjectileHitDetection.Detect(
            projectile,
            arena,
            new[] { ownerTank });

        Assert.Equal(ProjectileHitKind.Tank, result.Kind);
        Assert.Equal(new TankId(7), result.TankId);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void Detect_DoesNotMutateInputs()
    {
        ArenaDefinition arena = CreateOpenArena();
        ProjectileState originalProjectile = CreateProjectile(position: FixedVec2.FromInts(10, 10));
        TankState originalTank = CreateTank(id: 1, position: FixedVec2.FromInts(10, 10));

        ProjectileState projectileSnapshot = originalProjectile;
        TankState tankSnapshot = originalTank;

        ProjectileHitDetection.Detect(originalProjectile, arena, new[] { originalTank });

        Assert.Equal(projectileSnapshot, originalProjectile);
        Assert.Equal(tankSnapshot, originalTank);
    }
}
