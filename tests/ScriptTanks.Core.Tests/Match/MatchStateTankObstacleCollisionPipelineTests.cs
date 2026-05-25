using System;
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

public sealed class MatchStateTankObstacleCollisionPipelineTests
{
    private static readonly FixedVec2 UnitVelocityEast = FixedVec2.FromInts(1, 0);

    private static FixedVec2 Vec(int x, int y)
        => FixedVec2.FromInts(x, y);

    private static MovementState CreateMovement(FixedVec2 position, FixedVec2 velocity)
        => new MovementState(position, velocity);

    private static TankState CreateTank(
        int id,
        FixedVec2 position,
        FixedVec2 velocity,
        int hitPoints = 100)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(id),
            TankCatalog.BasicTank,
            CreateMovement(position, velocity),
            hitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankState CreateDestroyedTank(int id, FixedVec2 position)
        => CreateTank(id, position, FixedVec2.Zero, hitPoints: 0);

    private static WallBlock CreateWall(string id, FixedRect bounds)
        => new WallBlock(id, bounds);

    private static FixedRect UnitWallAtTenByTen()
        => FixedRect.FromMinSize(Vec(10, 10), Fixed.FromInt(10), Fixed.FromInt(10));

    private static ArenaDefinition CreateWallArena(params WallBlock[] wallBlocks)
    {
        return new ArenaDefinition(
            id: "obstacle_pipeline_test_arena",
            displayName: "Obstacle Pipeline Test Arena",
            description: "Arena for tank obstacle collision pipeline tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), Vec(10, 10)),
            },
            wallBlocks: wallBlocks,
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileState CreateProjectile(int id)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        return ProjectileSpawnFactory.Create(
            new ProjectileId(id),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            FixedVec2.FromInts(50, 60),
            FixedVec2.FromInts(2, 0),
            new SimTick(5));
    }

    private static MatchState CreateState(
        ArenaDefinition arena,
        SimTick tick,
        TankState[] tanks,
        ProjectileState[]? projectiles = null)
    {
        TankWeaponLoadout[] loadouts = new TankWeaponLoadout[tanks.Length];
        for (int i = 0; i < tanks.Length; i++)
        {
            loadouts[i] = CreateLoadout();
        }

        return new MatchState(
            arena,
            tick,
            tanks,
            loadouts,
            projectiles ?? Array.Empty<ProjectileState>());
    }

    private static (MatchState PreMovement, MatchState Candidate) CreatePreAndCandidateStates(
        ArenaDefinition arena,
        SimTick tick,
        TankState[] preTanks,
        TankState[] candidateTanks,
        ProjectileState[]? projectiles = null)
    {
        ProjectileState[] projectileArray = projectiles ?? Array.Empty<ProjectileState>();
        MatchState preMovement = CreateState(arena, tick, preTanks, projectileArray);
        MatchState candidate = CreateState(arena, tick, candidateTanks, projectileArray);
        return (preMovement, candidate);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Step_NullPreMovementState_Throws()
    {
        MatchState candidate = CreateState(
            CreateWallArena(),
            new SimTick(5),
            new[] { CreateTank(0, Vec(5, 15), FixedVec2.Zero) });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchStateTankObstacleCollisionPipeline.Step(null!, candidate));

        Assert.Equal("preMovementState", ex.ParamName);
    }

    [Fact]
    public void Step_NullCandidateState_Throws()
    {
        MatchState preMovement = CreateState(
            CreateWallArena(),
            new SimTick(5),
            new[] { CreateTank(0, Vec(5, 15), FixedVec2.Zero) });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, null!));

        Assert.Equal("candidateState", ex.ParamName);
    }

    [Fact]
    public void Step_TankCountMismatch_Throws()
    {
        ArenaDefinition arena = CreateWallArena();
        SimTick tick = new SimTick(5);
        MatchState preMovement = CreateState(
            arena,
            tick,
            new[] { CreateTank(0, Vec(5, 15), FixedVec2.Zero) });
        MatchState candidate = CreateState(
            arena,
            tick,
            new[]
            {
                CreateTank(0, Vec(5, 15), FixedVec2.Zero),
                CreateTank(1, Vec(30, 30), FixedVec2.Zero),
            });

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate));

        Assert.Equal("candidateState", ex.ParamName);
    }

    [Fact]
    public void Step_TankIdMismatch_Throws()
    {
        ArenaDefinition arena = CreateWallArena();
        SimTick tick = new SimTick(5);
        MatchState preMovement = CreateState(
            arena,
            tick,
            new[] { CreateTank(0, Vec(5, 15), FixedVec2.Zero) });
        MatchState candidate = CreateState(
            arena,
            tick,
            new[] { CreateTank(1, Vec(5, 15), FixedVec2.Zero) });

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate));

        Assert.Equal("candidateState", ex.ParamName);
    }

    // -------------------- No walls / unchanged --------------------

    [Fact]
    public void Step_NoWalls_ReturnsUnchangedRecordAndCandidateStatePosition()
    {
        FixedVec2 position = Vec(15, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(),
            new SimTick(5),
            new[] { CreateTank(0, position, FixedVec2.Zero) },
            new[] { CreateTank(0, position, FixedVec2.Zero) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.Unchanged, record.Status);
        Assert.False(record.DidBlockMovement);
        Assert.Null(record.BlockingWallBlockId);
        Assert.Equal(position, record.FinalPosition);
        Assert.Equal(position, result.FinalState.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_WallsButCandidateClear_ReturnsUnchanged()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(30, 30);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.Unchanged, record.Status);
        Assert.False(record.DidBlockMovement);
        Assert.Null(record.BlockingWallBlockId);
        Assert.Equal(candidatePosition, result.FinalState.Tanks[0].Movement.Position);
        Assert.Equal(UnitVelocityEast, result.FinalState.Tanks[0].Movement.VelocityPerTick);
    }

    // -------------------- Blocked movement --------------------

    [Fact]
    public void Step_CandidateOverlapsWall_RollsBackToPreMovementPosition()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, record.Status);
        Assert.True(record.DidBlockMovement);
        Assert.Equal("wall_a", record.BlockingWallBlockId);
        Assert.Equal(prePosition, record.InitialPosition);
        Assert.Equal(candidatePosition, record.CandidatePosition);
        Assert.Equal(prePosition, record.FinalPosition);
        Assert.Equal(prePosition, result.FinalState.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_CandidateTouchesWallEdge_RollsBack()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(8, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, record.Status);
        Assert.True(record.DidBlockMovement);
        Assert.Equal(prePosition, result.FinalState.Tanks[0].Movement.Position);
    }

    // -------------------- Started inside obstacle --------------------

    [Fact]
    public void Step_PreMovementAlreadyInsideWall_ReturnsStartedInsideObstacle()
    {
        FixedVec2 prePosition = Vec(15, 15);
        FixedVec2 candidatePosition = Vec(16, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.StartedInsideObstacle, record.Status);
        Assert.True(record.DidBlockMovement);
        Assert.Equal("wall_a", record.BlockingWallBlockId);
        Assert.Equal(prePosition, record.FinalPosition);
        Assert.Equal(prePosition, result.FinalState.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_PreMovementInsideWall_DoesNotPreferCandidateWall()
    {
        FixedRect bounds = UnitWallAtTenByTen();
        FixedVec2 prePosition = Vec(15, 15);
        FixedVec2 candidatePosition = Vec(50, 50);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(
                CreateWall("wall_a", bounds),
                CreateWall("wall_b", bounds)),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, FixedVec2.Zero) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.StartedInsideObstacle, record.Status);
        Assert.Equal("wall_a", record.BlockingWallBlockId);
        Assert.Equal(prePosition, result.FinalState.Tanks[0].Movement.Position);
    }

    // -------------------- Destroyed tanks --------------------

    [Fact]
    public void Step_DestroyedTank_IsSkipped()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateDestroyedTank(0, prePosition) },
            new[] { CreateDestroyedTank(0, candidatePosition) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.SkippedDestroyed, record.Status);
        Assert.False(record.DidBlockMovement);
        Assert.Null(record.BlockingWallBlockId);
        Assert.Equal(candidatePosition, record.FinalPosition);
        Assert.Equal(candidatePosition, result.FinalState.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_DestroyedTankInsideWall_DoesNotReturnStartedInsideObstacle()
    {
        FixedVec2 insideWall = Vec(15, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateDestroyedTank(0, insideWall) },
            new[] { CreateDestroyedTank(0, insideWall) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.SkippedDestroyed, record.Status);
        Assert.Null(record.BlockingWallBlockId);
        Assert.Equal(insideWall, result.FinalState.Tanks[0].Movement.Position);
    }

    // -------------------- Ordering --------------------

    [Fact]
    public void Step_MultipleBlockingWalls_UsesFirstWallInArenaOrder()
    {
        FixedRect bounds = UnitWallAtTenByTen();
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(
                CreateWall("first_wall", bounds),
                CreateWall("second_wall", bounds)),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, FixedVec2.Zero) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        Assert.Equal("first_wall", result.GetRecordAtIndex(0).BlockingWallBlockId);
    }

    [Fact]
    public void Step_ReorderedWalls_UsesNewFirstWall()
    {
        FixedRect bounds = UnitWallAtTenByTen();
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        SimTick tick = new SimTick(5);
        TankState[] preTanks = new[] { CreateTank(0, prePosition, FixedVec2.Zero) };
        TankState[] candidateTanks = new[] { CreateTank(0, candidatePosition, FixedVec2.Zero) };

        (MatchState preMovementFirst, MatchState candidateFirst) = CreatePreAndCandidateStates(
            CreateWallArena(
                CreateWall("first_wall", bounds),
                CreateWall("second_wall", bounds)),
            tick,
            preTanks,
            candidateTanks);

        (MatchState preMovementSwapped, MatchState candidateSwapped) = CreatePreAndCandidateStates(
            CreateWallArena(
                CreateWall("second_wall", bounds),
                CreateWall("first_wall", bounds)),
            tick,
            preTanks,
            candidateTanks);

        MatchStateTankObstacleCollisionResult firstResult =
            MatchStateTankObstacleCollisionPipeline.Step(preMovementFirst, candidateFirst);
        MatchStateTankObstacleCollisionResult swappedResult =
            MatchStateTankObstacleCollisionPipeline.Step(preMovementSwapped, candidateSwapped);

        Assert.Equal("first_wall", firstResult.GetRecordAtIndex(0).BlockingWallBlockId);
        Assert.Equal("second_wall", swappedResult.GetRecordAtIndex(0).BlockingWallBlockId);
    }

    // -------------------- FinalState preservation --------------------

    [Fact]
    public void Step_BlockedMovement_PreservesVelocityPerTick()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        Assert.Equal(UnitVelocityEast, result.FinalState.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Step_BlockedMovement_PreservesArenaCurrentTickAndProjectiles()
    {
        ProjectileState projectile = CreateProjectile(0);
        SimTick tick = new SimTick(7);
        ArenaDefinition arena = CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            arena,
            tick,
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) },
            new[] { projectile });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        Assert.Same(arena, result.FinalState.Arena);
        Assert.Equal(tick, result.FinalState.CurrentTick);
        Assert.Equal(candidate.Projectiles.Count, result.FinalState.Projectiles.Count);
        Assert.Equal(projectile, result.FinalState.Projectiles[0]);
    }

    [Fact]
    public void Step_MultipleTanks_ProcessesEachTankIndependently()
    {
        FixedVec2 blockedPre = Vec(5, 15);
        FixedVec2 blockedCandidate = Vec(12, 15);
        FixedVec2 clearPosition = Vec(30, 30);
        FixedVec2 insidePre = Vec(15, 15);
        FixedVec2 insideCandidate = Vec(16, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[]
            {
                CreateTank(0, blockedPre, FixedVec2.Zero),
                CreateTank(1, clearPosition, FixedVec2.Zero),
                CreateTank(2, insidePre, FixedVec2.Zero),
            },
            new[]
            {
                CreateTank(0, blockedCandidate, UnitVelocityEast),
                CreateTank(1, clearPosition, FixedVec2.Zero),
                CreateTank(2, insideCandidate, UnitVelocityEast),
            });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, result.GetRecordAtIndex(0).Status);
        Assert.Equal(MatchStateTankObstacleCollisionStatus.Unchanged, result.GetRecordAtIndex(1).Status);
        Assert.Equal(MatchStateTankObstacleCollisionStatus.StartedInsideObstacle, result.GetRecordAtIndex(2).Status);

        Assert.Equal(blockedPre, result.FinalState.Tanks[0].Movement.Position);
        Assert.Equal(clearPosition, result.FinalState.Tanks[1].Movement.Position);
        Assert.Equal(insidePre, result.FinalState.Tanks[2].Movement.Position);
    }

    // -------------------- Purity / references --------------------

    [Fact]
    public void Step_DoesNotModifyInputStates()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        TankState preTank = CreateTank(0, prePosition, FixedVec2.Zero);
        TankState candidateTank = CreateTank(0, candidatePosition, UnitVelocityEast);
        MovementState preMovementBefore = preTank.Movement;
        MovementState candidateMovementBefore = candidateTank.Movement;
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { preTank },
            new[] { candidateTank });

        _ = MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        Assert.Equal(preMovementBefore, preMovement.Tanks[0].Movement);
        Assert.Equal(candidateMovementBefore, candidate.Tanks[0].Movement);
        Assert.Equal(prePosition, preMovement.Tanks[0].Movement.Position);
        Assert.Equal(candidatePosition, candidate.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_ResultStoresStateReferences()
    {
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, Vec(5, 15), FixedVec2.Zero) },
            new[] { CreateTank(0, Vec(12, 15), UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        Assert.Same(preMovement, result.PreMovementState);
        Assert.Same(candidate, result.CandidateState);
        Assert.NotSame(candidate, result.FinalState);
    }

    [Fact]
    public void Step_RecordsMatchResultStateChain()
    {
        FixedVec2 prePosition = Vec(5, 15);
        FixedVec2 candidatePosition = Vec(12, 15);
        (MatchState preMovement, MatchState candidate) = CreatePreAndCandidateStates(
            CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen())),
            new SimTick(5),
            new[] { CreateTank(0, prePosition, FixedVec2.Zero) },
            new[] { CreateTank(0, candidatePosition, UnitVelocityEast) });

        MatchStateTankObstacleCollisionResult result =
            MatchStateTankObstacleCollisionPipeline.Step(preMovement, candidate);

        for (int i = 0; i < result.Count; i++)
        {
            MatchStateTankObstacleCollisionRecord record = result.GetRecordAtIndex(i);

            Assert.Equal(
                result.PreMovementState.Tanks[i].Movement.Position,
                record.InitialPosition);
            Assert.Equal(
                result.CandidateState.Tanks[i].Movement.Position,
                record.CandidatePosition);
            Assert.Equal(
                result.FinalState.Tanks[i].Movement.Position,
                record.FinalPosition);
        }
    }
}
