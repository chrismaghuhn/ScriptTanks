using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class CombinedRuntimeTickPipelineTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: turretRotation);
    }

    private static TankWeaponLoadout CreateWeaponLoadout(params WeaponState[] weapons)
    {
        return new TankWeaponLoadout(weapons);
    }

    private static MatchState CreateMatchState(
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return CreateMatchState(tick, weaponLoadouts, Array.Empty<ProjectileState>(), tanks);
    }

    private static MatchState CreateMatchState(
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        ProjectileState[] projectiles,
        params TankState[] tanks)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            weaponLoadouts,
            projectiles);
    }

    private static TankState CreateTankWithMovement(
        int id,
        int ownerSlot,
        FixedVec2 position,
        FixedVec2 velocityPerTick,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position, velocityPerTick),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: turretRotation);
    }

    private static ProjectileDefinition CreateSeededProjectileDefinition()
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateSeededProjectile(
        FixedVec2 position,
        SimTick spawnTick,
        int ownerTankId = 1)
    {
        ProjectileDefinition definition = CreateSeededProjectileDefinition();

        return new ProjectileState(
            new ProjectileId(0),
            definition,
            new TankId(ownerTankId),
            new WeaponSlot(0),
            spawnTick,
            position,
            FixedVec2.Zero,
            definition.MaxRange,
            isActive: true);
    }

    private static FixedVec2 MovementBeforeHitTargetStartPosition()
        => new FixedVec2(Fixed.FromRatio(77, 10), Fixed.FromInt(20));

    private static FixedVec2 MovementBeforeHitTargetPositionAfterOneTick()
        => new FixedVec2(Fixed.FromRatio(78, 10), Fixed.FromInt(20));

    private static MatchState CreateMovementBeforeHitState(SimTick tick)
    {
        FixedVec2 projectilePosition = FixedVec2.FromInts(10, 20);
        TankWeaponLoadout[] loadouts =
        {
            CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
        };

        return CreateMatchState(
            tick,
            loadouts,
            new[] { CreateSeededProjectile(projectilePosition, new SimTick(tick.Value - 1), ownerTankId: 1) },
            CreateTank(0, 0, MovementBeforeHitTargetStartPosition()),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)));
    }

    private static MatchSensorRuntimeState CreateMovementBeforeHitRuntime(SimTick tick)
    {
        MatchState state = CreateMovementBeforeHitState(tick);
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateOwnerSurvivalWithPreVelocityRuntime(SimTick tick)
    {
        FixedVec2 ownerPosition = FixedVec2.FromInts(11, 20);
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
            CreateTankWithMovement(1, 1, ownerPosition, ExpectedPatrolVelocity()));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static TankSensorLoadout CreateSensorLoadout(
        SensorState first,
        SensorState? second = null)
    {
        return second.HasValue
            ? new TankSensorLoadout(new[] { first, second.Value })
            : new TankSensorLoadout(new[] { first });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    private static ScriptProgram ScanWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "scan",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.ScanEnemy, "default")),
            });
    }

    private static ScriptProgram FireWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Fire, argument: string.Empty)),
            });
    }

    private static ScriptProgram AimAtEnemyWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "aim",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.AimAtEnemy, "nearest_visible")),
            });
    }

    private static ScriptProgram NoOpWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "noop",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.NoOp, string.Empty)),
            });
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankAimRuntime(
        SimTick tick,
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, ownerPosition),
            CreateTank(1, 1, enemyPosition));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static FixedVec2 OwnerPosition() => FixedVec2.FromInts(10, 20);

    private static readonly FixedVec2 FullAngleOwner = FixedVec2.FromInts(10, 10);

    private static Fixed ExpectedAimRotation(FixedVec2 owner, FixedVec2 target)
    {
        FixedVec2 delta = target - owner;
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(delta);

        Assert.True(resolution.IsResolved);
        return resolution.Rotation;
    }

    private static Fixed ExpectedGradualAimRotation(
        Fixed initialTurret,
        FixedVec2 owner,
        FixedVec2 target)
    {
        Fixed desired = ExpectedAimRotation(owner, target);
        return FixedRotationTurnStepResolver.ResolveStep(
            initialTurret,
            desired,
            TankCatalog.BasicTank.Stats.TurretTurnRatePerTick).FinalRotation;
    }

    private static MatchSensorRuntimeState CreateOwnerSurvivalTwoTankRuntime(SimTick tick)
        => CreateTwoTankRuntime(tick);

    private static MatchSensorRuntimeState CreateEnemyHitTwoTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(13, 20)),
            CreateTank(1, 1, FixedVec2.FromInts(11, 20)));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static List<ScriptProgram> ScanThenFirePrograms()
        => new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };

    private static ScriptProgram MoveToPatrolPointWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "patrol",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, "next")),
            });
    }

    private static ScriptProgram RetreatWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "retreat",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Retreat, "away_from_nearest_visible")),
            });
    }

    private static FixedVec2 ExpectedPatrolVelocity()
    {
        Fixed maxSpeed = TankCatalog.BasicTank.Stats.MaxVelocityPerTick;
        return FixedVec2.FromInts(1, 0) * maxSpeed;
    }

    private static FixedVec2 ExpectedRetreatVelocity() => -ExpectedPatrolVelocity();

    #region Validation

    [Fact]
    public void Step_NullRuntime_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeTickPipeline.Step(
                null!,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Step_NullPrograms_ThrowsArgumentNullException()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeTickPipeline.Step(
                runtime,
                null!,
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Step_program_count_mismatch_less_than_tanks()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Step_program_count_mismatch_greater_than_tanks()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Step_null_program_element()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram?> { null }!,
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Result chain

    [Fact]
    public void Step_ReturnsCombinedRuntimeTickResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.NotNull(result);
        Assert.Same(runtime, result.InitialRuntime);
        Assert.NotNull(result.ScriptResult);
        Assert.NotNull(result.SteppedState);
        Assert.NotNull(result.FinalRuntime);
    }

    [Fact]
    public void Step_result_chains_references_and_sequence()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Same(runtime, result.InitialRuntime);
        Assert.Same(runtime, result.ScriptResult.InitialRuntime);
        Assert.Same(result.SteppedState, result.FinalRuntime.State);
        Assert.Same(
            result.ScriptResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
        Assert.Equal(
            result.ScriptResult.FinalProjectileIdSequence,
            result.FinalProjectileIdSequence);
        Assert.NotNull(result.TankBoundsResult);
        Assert.NotNull(result.TankObstacleCollisionResult);
        Assert.Same(
            result.TankMovementResult.FinalState,
            result.TankBoundsResult.InitialState);
        Assert.Same(
            result.TankMovementResult.InitialState,
            result.TankObstacleCollisionResult.PreMovementState);
        Assert.Same(
            result.TankBoundsResult.FinalState,
            result.TankObstacleCollisionResult.CandidateState);
    }

    #endregion

    #region Tank obstacle collision integration (5.129)

    private static FixedVec2 Vec(int x, int y)
        => FixedVec2.FromInts(x, y);

    private static WallBlock CreateWall(string id, FixedRect bounds)
        => new WallBlock(id, bounds);

    private static FixedRect UnitWallAtTenByTen()
        => FixedRect.FromMinSize(Vec(10, 10), Fixed.FromInt(10), Fixed.FromInt(10));

    private static ArenaDefinition CreateWallArena(params WallBlock[] wallBlocks)
    {
        return new ArenaDefinition(
            id: "combined_obstacle_test_arena",
            displayName: "Combined Obstacle Test Arena",
            description: "Arena for combined runtime obstacle integration tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), Vec(10, 10)),
            },
            wallBlocks: wallBlocks,
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    private static MatchState CreateMatchState(
        ArenaDefinition arena,
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return new MatchState(arena, tick, tanks, weaponLoadouts, Array.Empty<ProjectileState>());
    }

    private static MatchSensorRuntimeState CreateWallBlockedCandidateRuntime(SimTick tick)
    {
        FixedVec2 preMovementPosition = Vec(5, 15);
        FixedVec2 velocityIntoWall = Vec(7, 0);
        ArenaDefinition arena = CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        MatchState state = CreateMatchState(
            arena,
            tick,
            new[] { CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)) },
            CreateTankWithMovement(0, 0, preMovementPosition, velocityIntoWall));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void Step_ResultChainsTankObstacleAfterBounds()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Same(
            result.TankMovementResult.FinalState,
            result.TankBoundsResult.InitialState);
        Assert.Same(
            result.TankBoundsResult.FinalState,
            result.TankObstacleCollisionResult.CandidateState);
        Assert.Same(
            result.TankMovementResult.InitialState,
            result.TankObstacleCollisionResult.PreMovementState);
    }

    [Fact]
    public void Step_WallBlockedCandidate_RollsBackBeforeProjectilePhase()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 preMovementPosition = Vec(5, 15);
        FixedVec2 candidateInWallPosition = Vec(12, 15);
        MatchSensorRuntimeState runtime = CreateWallBlockedCandidateRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        MatchStateTankObstacleCollisionRecord obstacleRecord =
            result.TankObstacleCollisionResult.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, obstacleRecord.Status);
        Assert.True(obstacleRecord.DidBlockMovement);
        Assert.Equal("wall_a", obstacleRecord.BlockingWallBlockId);
        Assert.Equal(preMovementPosition, obstacleRecord.InitialPosition);
        Assert.Equal(candidateInWallPosition, obstacleRecord.CandidatePosition);
        Assert.Equal(preMovementPosition, obstacleRecord.FinalPosition);
        Assert.Equal(preMovementPosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.NotEqual(candidateInWallPosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(Vec(7, 0), result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    private static FixedVec2 WallRegressionPrePosition()
        => Vec(5, 15);

    private static FixedVec2 WallRegressionCandidatePosition()
        => Vec(8, 15);

    private static FixedVec2 WallRegressionVelocity()
        => Vec(3, 0);

    private static MatchState CreateWallRegressionMatchState(
        ArenaDefinition arena,
        SimTick tick,
        ProjectileState[] projectiles,
        params TankState[] tanks)
    {
        TankWeaponLoadout[] loadouts = new TankWeaponLoadout[tanks.Length];
        for (int i = 0; i < tanks.Length; i++)
        {
            loadouts[i] = CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon));
        }

        return new MatchState(arena, tick, tanks, loadouts, projectiles);
    }

    private static MatchSensorRuntimeState CreateWallProjectileRegressionRuntime(SimTick tick)
    {
        ArenaDefinition arena = CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        SimTick spawnTick = tick.Value > 0
            ? new SimTick(tick.Value - 1)
            : new SimTick(0);
        ProjectileState projectile = CreateSeededProjectile(
            WallRegressionCandidatePosition(),
            spawnTick,
            ownerTankId: 1);
        MatchState state = CreateWallRegressionMatchState(
            arena,
            tick,
            new[] { projectile },
            CreateTankWithMovement(
                0,
                0,
                WallRegressionPrePosition(),
                WallRegressionVelocity()),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)));

        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    #endregion

    #region Wall obstacle integration regression (5.130)

    [Fact]
    public void Step_WallRollbackPreventsProjectileHitThatCandidatePositionWouldHaveAllowed()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 prePosition = WallRegressionPrePosition();
        FixedVec2 candidatePosition = WallRegressionCandidatePosition();
        int maxHp = TankCatalog.BasicTank.Stats.MaxHitPoints;
        MatchSensorRuntimeState runtime = CreateWallProjectileRegressionRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        MatchStateTankObstacleCollisionRecord obstacleRecord =
            result.TankObstacleCollisionResult.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, obstacleRecord.Status);
        Assert.Equal(prePosition, obstacleRecord.InitialPosition);
        Assert.Equal(candidatePosition, obstacleRecord.CandidatePosition);
        Assert.Equal(prePosition, obstacleRecord.FinalPosition);
        Assert.Equal(maxHp, result.FinalRuntime.State.Tanks[0].CurrentHitPoints);
        Assert.Equal(prePosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.NotEqual(candidatePosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_ProjectileHitDetectionUsesPostWallTankPosition()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 prePosition = WallRegressionPrePosition();
        FixedVec2 candidatePosition = WallRegressionCandidatePosition();
        int maxHp = TankCatalog.BasicTank.Stats.MaxHitPoints;
        MatchSensorRuntimeState runtime = CreateWallProjectileRegressionRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(candidatePosition, result.TankBoundsResult.GetRecordAtIndex(0).FinalPosition);
        Assert.Equal(prePosition, result.TankObstacleCollisionResult.GetRecordAtIndex(0).FinalPosition);
        Assert.Equal(maxHp, result.FinalRuntime.State.Tanks[0].CurrentHitPoints);
        Assert.Equal(prePosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_WallRollbackPreservesProjectilePhaseAndAdvance()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 prePosition = WallRegressionPrePosition();
        MatchSensorRuntimeState runtime = CreateWallProjectileRegressionRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, result.TankObstacleCollisionResult.GetRecordAtIndex(0).Status);
        Assert.Equal(prePosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
        Assert.Same(result.SteppedState, result.FinalRuntime.State);
    }

    #endregion

    #region Script-before-tick behavior

    [Fact]
    public void MatchTickPipeline_Step_does_not_mutate_composer_final_state()
    {
        CombinedScriptRuntimeComposerResult composer =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(new SimTick(10)),
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0));

        MatchState composerState = composer.FinalRuntime.State;
        Assert.Single(composerState.Projectiles);

        MatchTickPipeline.Step(composerState);

        Assert.Single(composerState.Projectiles);
        Assert.Equal(new SimTick(10), composerState.CurrentTick);
    }

    [Fact]
    public void Step_fire_only_creates_projectile_then_moves_it_same_tick()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.ScriptResult.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.True(result.ScriptResult.FireApplicationResult.Records[1].DidApply);
        Assert.Single(result.ScriptResult.FinalRuntime.State.Projectiles);
        Assert.Equal(FixedVec2.FromInts(12, 20), result.ScriptResult.FinalRuntime.State.Projectiles[0].Position);

        Assert.Same(result.SteppedState, result.FinalRuntime.State);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.True(result.FinalRuntime.State.Projectiles[0].IsActive);
        Assert.Equal(new SimTick(10), result.FinalRuntime.State.Projectiles[0].SpawnTick);
        Assert.Equal(FixedVec2.FromInts(13, 20), result.FinalRuntime.State.Projectiles[0].Position);
    }

    [Fact]
    public void Step_scan_only_advances_tick_without_projectiles()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Empty(result.ScriptResult.FinalRuntime.State.Projectiles);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Spawn-tick owner filter regression (5.99)

    [Fact]
    public void Step_Fire_SpawnTickOwnerOverlap_OwnerHpUnchanged()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateOwnerSurvivalTwoTankRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                ScanThenFirePrograms(),
                new ProjectileIdSequence(0));

        ProjectileState projectile = result.FinalRuntime.State.Projectiles[0];

        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, result.FinalRuntime.State.Tanks[1].CurrentHitPoints);
        Assert.True(projectile.IsActive);
        Assert.Equal(startTick, projectile.SpawnTick);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
        Assert.Equal(new TankId(1), projectile.OwnerTankId);
    }

    [Fact]
    public void Step_Fire_SpawnTickOwnerFiltered_EnemyStillDamaged()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateEnemyHitTwoTankRuntime(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                ScanThenFirePrograms(),
                new ProjectileIdSequence(0));

        Assert.True(result.ScriptResult.FireApplicationResult.Records[1].DidApply);
        ProjectileState spawned = result.ScriptResult.FinalRuntime.State.Projectiles[0];
        Assert.Equal(startTick, spawned.SpawnTick);
        Assert.Equal(new TankId(1), spawned.OwnerTankId);

        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, result.FinalRuntime.State.Tanks[1].CurrentHitPoints);
        Assert.Equal(80, result.FinalRuntime.State.Tanks[0].CurrentHitPoints);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
    }

    #endregion

    #region Sensor preservation

    [Fact]
    public void Step_scan_preserves_sensor_loadouts_through_tick()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.True(result.ScriptResult.SensorApplicationResult.Records[0].DidApply);
        Assert.NotSame(runtime, result.ScriptResult.FinalRuntime);
        Assert.Same(
            result.ScriptResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
    }

    #endregion

    #region Sequence carry

    [Fact]
    public void Step_fire_advances_projectile_sequence_tick_does_not()
    {
        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                CreateSingleTankRuntime(new SimTick(10)),
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(42));

        Assert.Equal(
            new ProjectileId(42),
            result.ScriptResult.FireConstructionPipelineResult
                .ConstructionResult.Records[0].FireRequest!.Value.ProjectileId);
        Assert.Equal(43, result.FinalProjectileIdSequence.NextValue);
        Assert.Equal(
            result.ScriptResult.FinalProjectileIdSequence,
            result.FinalProjectileIdSequence);
    }

    [Fact]
    public void Step_scan_leaves_projectile_sequence_unchanged()
    {
        ProjectileIdSequence input = new ProjectileIdSequence(42);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                CreateSingleTankRuntime(new SimTick(10)),
                new List<ScriptProgram> { ScanWhenAlways() },
                input);

        Assert.Equal(input, result.FinalProjectileIdSequence);
        Assert.Equal(42, result.FinalProjectileIdSequence.NextValue);
        Assert.Equal(
            result.ScriptResult.FinalProjectileIdSequence,
            result.FinalProjectileIdSequence);
    }

    #endregion

    #region Determinism / purity

    [Fact]
    public void Step_deterministic_repeated_calls()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(new SimTick(10));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        CombinedRuntimeTickResult a =
            CombinedRuntimeTickPipeline.Step(runtime, programs, sequence);
        CombinedRuntimeTickResult b =
            CombinedRuntimeTickPipeline.Step(runtime, programs, sequence);

        Assert.Equal(a.FinalProjectileIdSequence, b.FinalProjectileIdSequence);
        Assert.Equal(
            a.FinalRuntime.State.Projectiles.Count,
            b.FinalRuntime.State.Projectiles.Count);
        Assert.Equal(
            a.ScriptResult.SensorApplicationResult.Records[0].DidApply,
            b.ScriptResult.SensorApplicationResult.Records[0].DidApply);
        Assert.Equal(
            a.ScriptResult.FireConstructionPipelineResult.ConstructionResult.Records[1].Status,
            b.ScriptResult.FireConstructionPipelineResult.ConstructionResult.Records[1].Status);
        Assert.Equal(
            a.ScriptResult.FireApplicationResult.Records[1].DidApply,
            b.ScriptResult.FireApplicationResult.Records[1].DidApply);
        Assert.Equal(
            a.FinalRuntime.State.CurrentTick,
            b.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_does_not_mutate_original_runtime_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(new SimTick(10));
        SimTick tickBefore = runtime.State.CurrentTick;
        TankState tankBefore = runtime.State.Tanks[0];
        MatchSensorLoadoutState loadoutsBefore = runtime.SensorLoadouts;

        _ = CombinedRuntimeTickPipeline.Step(
            runtime,
            new List<ScriptProgram> { ScanWhenAlways() },
            new ProjectileIdSequence(0));

        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(tankBefore, runtime.State.Tanks[0]);
        Assert.Same(loadoutsBefore, runtime.SensorLoadouts);
    }

    #endregion

    #region Movement application regression (5.106 / 5.115)

    [Fact]
    public void Step_MovementCommand_AppliesVelocityAndMovesPosition()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = runtime.State.Tanks[0].Movement.Position;
        FixedVec2 expectedPosition = initialPosition + ExpectedPatrolVelocity();

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0));

        TankState tank = result.FinalRuntime.State.Tanks[0];
        MatchStateTankMovementRecord movementRecord =
            result.TankMovementResult.GetRecordAtIndex(0);

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            result.ScriptResult.MovementApplicationResult.Records[0].Status);
        Assert.Equal(MatchStateTankMovementStatus.Moved, movementRecord.Status);
        Assert.True(movementRecord.DidMove);
        Assert.Equal(ExpectedPatrolVelocity(), tank.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank.Movement.Position);
        Assert.Same(result.ScriptResult.FinalRuntime.State, result.TankMovementResult.InitialState);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_RetreatCommand_MovesPositionOppositeBodyForward()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = runtime.State.Tanks[0].Movement.Position;
        FixedVec2 expectedPosition = initialPosition + ExpectedRetreatVelocity();

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { RetreatWhenAlways() },
                new ProjectileIdSequence(0));

        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(ExpectedRetreatVelocity(), tank.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank.Movement.Position);
        Assert.Equal(MatchStateTankMovementStatus.Moved, result.TankMovementResult.GetRecordAtIndex(0).Status);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_MovementAndFire_MovesTankBeforeProjectileResolution()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(startTick);
        FixedVec2 tank0InitialPosition = runtime.State.Tanks[0].Movement.Position;
        FixedVec2 expectedPosition = tank0InitialPosition + ExpectedPatrolVelocity();

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    MoveToPatrolPointWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        TankState tank0 = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(MatchStateTankMovementStatus.Moved, result.TankMovementResult.GetRecordAtIndex(0).Status);
        Assert.Equal(ExpectedPatrolVelocity(), tank0.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank0.Movement.Position);
        Assert.True(result.ScriptResult.FireApplicationResult.Records[1].DidApply);
        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.Equal(1, result.FinalProjectileIdSequence.NextValue);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_ZeroVelocityTank_ProducesStayedStill()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = runtime.State.Tanks[0].Movement.Position;

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(
            MatchStateTankMovementStatus.StayedStill,
            result.TankMovementResult.GetRecordAtIndex(0).Status);
        Assert.Equal(initialPosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(FixedVec2.Zero, result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_DestroyedTank_ProducesSkippedDestroyed()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 position = FixedVec2.FromInts(10, 20);
        TankState destroyedTank = new TankState(
            new TankId(0),
            new PlayerSlot(0),
            TankCatalog.BasicTank,
            new MovementState(position, ExpectedPatrolVelocity()),
            currentHitPoints: 0,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
        MatchState state = CreateMatchState(
            startTick,
            new[] { CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)) },
            destroyedTank);
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(
            MatchStateTankMovementStatus.SkippedDestroyed,
            result.TankMovementResult.GetRecordAtIndex(0).Status);
        Assert.Equal(position, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Tank bounds integration (5.122)

    private static Fixed ExpectedMaxLegalX(MatchState state)
        => state.Arena.Bounds.Width - TankCatalog.BasicTank.Stats.HitboxRadius;

    private static MatchSensorRuntimeState CreateRuntimeAtRightEdgeWithEastVelocity(SimTick tick)
    {
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        FixedVec2 position = new FixedVec2(maxX, Fixed.FromInt(20));
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);
        MatchState state = CreateMatchState(
            tick,
            new[] { CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)) },
            CreateTankWithMovement(0, 0, position, velocity));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void Step_MovementCommand_ClampsTankInsideArenaBounds()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateRuntimeAtRightEdgeWithEastVelocity(startTick);
        Fixed maxX = ExpectedMaxLegalX(runtime.State);
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);
        FixedVec2 positionAfterMovement = new FixedVec2(maxX + Fixed.FromInt(1), Fixed.FromInt(20));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        MatchStateTankMovementRecord movementRecord =
            result.TankMovementResult.GetRecordAtIndex(0);
        MatchStateTankBoundsRecord boundsRecord =
            result.TankBoundsResult.GetRecordAtIndex(0);
        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(MatchStateTankMovementStatus.Moved, movementRecord.Status);
        Assert.Equal(positionAfterMovement, movementRecord.FinalMovement.Position);
        Assert.Equal(MatchStateTankBoundsStatus.Clamped, boundsRecord.Status);
        Assert.True(boundsRecord.DidClamp);
        Assert.Equal(positionAfterMovement, boundsRecord.InitialPosition);
        Assert.Equal(maxX, boundsRecord.FinalPosition.X);
        Assert.Equal(maxX, tank.Movement.Position.X);
        Assert.Equal(velocity, tank.Movement.VelocityPerTick);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_MovementCommand_InsideBoundsProducesInsideBounds()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(startTick);
        FixedVec2 expectedPosition = runtime.State.Tanks[0].Movement.Position
            + ExpectedPatrolVelocity();

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0));

        MatchStateTankBoundsRecord boundsRecord =
            result.TankBoundsResult.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankBoundsStatus.InsideBounds, boundsRecord.Status);
        Assert.False(boundsRecord.DidClamp);
        Assert.Equal(expectedPosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_DestroyedTank_OutOfBounds_BoundsResultSkippedDestroyed()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 outsidePosition = FixedVec2.FromInts(-1, 20);
        TankState destroyedTank = new TankState(
            new TankId(0),
            new PlayerSlot(0),
            TankCatalog.BasicTank,
            new MovementState(outsidePosition, FixedVec2.Zero),
            currentHitPoints: 0,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
        MatchState state = CreateMatchState(
            startTick,
            new[] { CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)) },
            destroyedTank);
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(
            MatchStateTankMovementStatus.SkippedDestroyed,
            result.TankMovementResult.GetRecordAtIndex(0).Status);
        Assert.Equal(
            MatchStateTankBoundsStatus.SkippedDestroyed,
            result.TankBoundsResult.GetRecordAtIndex(0).Status);
        Assert.Equal(outsidePosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_BoundsRunBeforeProjectileHitDetection_ProjectileUsesClampedPosition()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateRuntimeAtRightEdgeWithEastVelocity(startTick);
        Fixed maxX = ExpectedMaxLegalX(runtime.State);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        MatchStateTankBoundsRecord boundsRecord =
            result.TankBoundsResult.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, boundsRecord.Status);
        Assert.Equal(maxX, result.FinalRuntime.State.Tanks[0].Movement.Position.X);
        Assert.NotEqual(boundsRecord.InitialPosition.X, boundsRecord.FinalPosition.X);
    }

    [Fact]
    public void Step_ResultChainsTankBoundsAfterMovement()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateRuntimeAtRightEdgeWithEastVelocity(startTick);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Same(
            result.TankMovementResult.FinalState,
            result.TankBoundsResult.InitialState);
    }

    #endregion

    #region Movement bounds projectile regression (5.123)

    private static FixedVec2 ProjectilePositionClampedHit()
        => FixedVec2.FromInts(100, 20);

    private static FixedVec2 ProjectilePositionBoundedOnlyHit()
        => new FixedVec2(Fixed.FromRatio(479, 5), Fixed.FromInt(20));

    private static FixedVec2 RawMovedPositionAfterEastStep(Fixed maxX)
        => new FixedVec2(maxX + Fixed.FromInt(1), Fixed.FromInt(20));

    private static MatchSensorRuntimeState CreateBoundsProjectileHitRuntime(
        SimTick tick,
        FixedVec2 projectilePosition,
        int ownerTankId = 1)
    {
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        TankWeaponLoadout[] loadouts =
        {
            CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
        };

        MatchState state = CreateMatchState(
            tick,
            loadouts,
            new[] { CreateSeededProjectile(projectilePosition, new SimTick(tick.Value - 1), ownerTankId) },
            CreateTankWithMovement(
                0,
                0,
                new FixedVec2(maxX, Fixed.FromInt(20)),
                FixedVec2.FromInts(1, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)));

        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateBoundsOwnerFilterRuntime(SimTick tick)
    {
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;

        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
            CreateTankWithMovement(
                1,
                1,
                new FixedVec2(maxX, Fixed.FromInt(20)),
                FixedVec2.FromInts(1, 0)));

        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void Step_BoundsRunBeforeProjectileHitDetection_ProjectileHitsClampedTankPosition()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateBoundsProjectileHitRuntime(
            startTick,
            ProjectilePositionClampedHit());
        Fixed maxX = ExpectedMaxLegalX(runtime.State);
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);
        FixedVec2 positionAfterMovement = RawMovedPositionAfterEastStep(maxX);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        MatchStateTankMovementRecord movementRecord =
            result.TankMovementResult.GetRecordAtIndex(0);
        MatchStateTankBoundsRecord boundsRecord =
            result.TankBoundsResult.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankMovementStatus.Moved, movementRecord.Status);
        Assert.Equal(positionAfterMovement, movementRecord.FinalMovement.Position);
        Assert.Equal(MatchStateTankBoundsStatus.Clamped, boundsRecord.Status);
        Assert.Equal(maxX, boundsRecord.FinalPosition.X);
        Assert.Equal(80, result.FinalRuntime.State.Tanks[0].CurrentHitPoints);
        Assert.Equal(maxX, result.FinalRuntime.State.Tanks[0].Movement.Position.X);
        Assert.Equal(velocity, result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_ProjectileHitDetection_UsesBoundedPosition_NotRawMovedPosition()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateBoundsProjectileHitRuntime(
            startTick,
            ProjectilePositionBoundedOnlyHit());
        Fixed maxX = ExpectedMaxLegalX(runtime.State);
        FixedVec2 positionAfterMovement = RawMovedPositionAfterEastStep(maxX);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(positionAfterMovement, result.TankMovementResult.GetRecordAtIndex(0).FinalMovement.Position);
        Assert.Equal(MatchStateTankBoundsStatus.Clamped, result.TankBoundsResult.GetRecordAtIndex(0).Status);
        Assert.Equal(maxX, result.TankBoundsResult.GetRecordAtIndex(0).FinalPosition.X);
        Assert.Equal(80, result.FinalRuntime.State.Tanks[0].CurrentHitPoints);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
    }

    [Fact]
    public void Step_FireMovementAndBounds_SpawnTickOwnerFilterStillPreventsOwnerSelfHit()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateBoundsOwnerFilterRuntime(startTick);
        Fixed maxX = ExpectedMaxLegalX(runtime.State);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    NoOpWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.True(result.ScriptResult.FireApplicationResult.Records[1].DidApply);
        Assert.Equal(MatchStateTankMovementStatus.Moved, result.TankMovementResult.GetRecordAtIndex(1).Status);
        Assert.Equal(
            MatchStateTankBoundsStatus.Clamped,
            result.TankBoundsResult.GetRecordAtIndex(1).Status);
        Assert.Equal(
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            result.FinalRuntime.State.Tanks[1].CurrentHitPoints);
        Assert.Equal(maxX, result.FinalRuntime.State.Tanks[1].Movement.Position.X);

        ProjectileState projectile = result.FinalRuntime.State.Projectiles[0];
        Assert.True(projectile.IsActive);
        Assert.Equal(startTick, projectile.SpawnTick);
        Assert.Equal(new TankId(1), projectile.OwnerTankId);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Movement before projectile hit (5.117)

    [Fact]
    public void Step_MovementRunsBeforeProjectileHitDetection_MovedTargetCanBeHit()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateMovementBeforeHitRuntime(startTick);
        FixedVec2 initialPosition = MovementBeforeHitTargetStartPosition();
        FixedVec2 expectedPosition = MovementBeforeHitTargetPositionAfterOneTick();

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    MoveToPatrolPointWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        MatchStateTankMovementRecord movementRecord =
            result.TankMovementResult.GetRecordAtIndex(0);
        TankState tank0 = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(MatchStateTankMovementStatus.Moved, movementRecord.Status);
        Assert.True(movementRecord.DidMove);
        Assert.Equal(expectedPosition, tank0.Movement.Position);
        Assert.Equal(initialPosition + ExpectedPatrolVelocity(), tank0.Movement.Position);
        Assert.Equal(80, tank0.CurrentHitPoints);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_FireAndMovement_SpawnTickOwnerFilterStillPreventsOwnerSelfHit()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateOwnerSurvivalWithPreVelocityRuntime(startTick);
        FixedVec2 ownerInitialPosition = FixedVec2.FromInts(11, 20);
        FixedVec2 expectedOwnerPosition = ownerInitialPosition + ExpectedPatrolVelocity();

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    NoOpWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.True(result.ScriptResult.FireApplicationResult.Records[1].DidApply);
        ProjectileState projectile = result.FinalRuntime.State.Projectiles[0];

        Assert.Equal(MatchStateTankMovementStatus.Moved, result.TankMovementResult.GetRecordAtIndex(1).Status);
        Assert.Equal(expectedOwnerPosition, result.FinalRuntime.State.Tanks[1].Movement.Position);
        Assert.Equal(
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            result.FinalRuntime.State.Tanks[1].CurrentHitPoints);
        Assert.True(projectile.IsActive);
        Assert.Equal(startTick, projectile.SpawnTick);
        Assert.Equal(new TankId(1), projectile.OwnerTankId);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Turret (5.110)

    [Fact]
    public void Step_AimAtEnemy_AppliesTurretRotation_AndAdvancesTick()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            startTick,
            OwnerPosition(),
            FixedVec2.FromInts(20, 20));

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.ScriptResult.TurretApplicationResult.Records[0].Status);
        Assert.Equal(Fixed.Zero, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_AimAtEnemy_DoesNotChangeBodyMovementPosition()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            startTick,
            OwnerPosition(),
            FixedVec2.FromInts(20, 20));
        FixedVec2 positionBefore = runtime.State.Tanks[0].Movement.Position;

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(positionBefore, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(FixedVec2.Zero, result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Full-angle AimAtEnemy combined tick regression (5.137)

    [Fact]
    public void Step_AimAtEnemy_DiagonalEnemy_FinalRuntimeHasGradualTurretRotation()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed expected = ExpectedGradualAimRotation(Fixed.Zero, FullAngleOwner, enemyPosition);

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                CreateTwoTankAimRuntime(startTick, FullAngleOwner, enemyPosition),
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.ScriptResult.TurretApplicationResult.Records[0].Status);
        Assert.Equal(expected, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Step_AimAtEnemy_TargetAtSamePosition_ReturnsMissingAimSolution()
    {
        SimTick startTick = new SimTick(10);
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(startTick, FullAngleOwner, FullAngleOwner);
        Fixed ownerTurretBefore = runtime.State.Tanks[0].TurretRotation;

        CombinedRuntimeTickResult result =
            CombinedRuntimeTickPipeline.Step(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.MissingAimSolution,
            result.ScriptResult.TurretApplicationResult.Records[0].Status);
        Assert.Equal(ownerTurretBefore, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(new SimTick(11), result.FinalRuntime.State.CurrentTick);
    }

    #endregion
}
