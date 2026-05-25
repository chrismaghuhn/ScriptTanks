using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Replay;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Replay;

public sealed class CombinedRuntimeReplayRecorderTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
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
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            weaponLoadouts,
            Array.Empty<ProjectileState>());
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

    private static FixedVec2 ExpectedPatrolVelocity()
    {
        Fixed maxSpeed = TankCatalog.BasicTank.Stats.MaxVelocityPerTick;
        return FixedVec2.FromInts(1, 0) * maxSpeed;
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

    private static FixedVec2 Vec(int x, int y) => FixedVec2.FromInts(x, y);

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

    private static List<ScriptProgram> AimAtEnemyThenNoOpPrograms()
        => new List<ScriptProgram>
        {
            AimAtEnemyWhenAlways(),
            NoOpWhenAlways(),
        };

    private static ScriptCondition Condition(ScriptConditionType type) =>
        new ScriptCondition(type, string.Empty);

    private static ScriptCommand FireCommand() =>
        new ScriptCommand(ScriptCommandType.Fire, string.Empty);

    private static ScriptCommand AimCommand() =>
        new ScriptCommand(ScriptCommandType.AimAtEnemy, "nearest_visible");

    private static ScriptCommand NoOpCommand() =>
        new ScriptCommand(ScriptCommandType.NoOp, string.Empty);

    private static ScriptProgram AimUntilAlignedThenFireProgram()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire-when-aligned",
                    Condition(ScriptConditionType.TurretAligned),
                    FireCommand()),
                new ScriptRoutine(
                    "aim-when-target",
                    Condition(ScriptConditionType.HasAimTarget),
                    AimCommand()),
                new ScriptRoutine(
                    "fallback",
                    ScriptCondition.Always(),
                    NoOpCommand()),
            });
    }

    private static List<ScriptProgram> AimUntilFireTankZeroPrograms()
        => new List<ScriptProgram>
        {
            AimUntilAlignedThenFireProgram(),
            NoOpWhenAlways(),
        };

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

    private static Fixed ExpectedAimRotation(FixedVec2 owner, FixedVec2 target)
    {
        FixedVec2 delta = target - owner;
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(delta);

        Assert.True(resolution.IsResolved);
        return resolution.Rotation;
    }

    private static Fixed ExpectedTurnStep(Fixed current, Fixed desired, TankState owner)
    {
        return FixedRotationTurnStepResolver.ResolveStep(
            current,
            desired,
            owner.Definition.Stats.TurretTurnRatePerTick).FinalRotation;
    }

    private static TankState CreateTankWithMovement(
        int id,
        int ownerSlot,
        FixedVec2 position,
        FixedVec2 velocityPerTick)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position, velocityPerTick),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static ProjectileState CreateSeededProjectile(
        FixedVec2 position,
        SimTick spawnTick,
        int ownerTankId = 1)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

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

    private static MatchSensorRuntimeState CreateRuntimeAtRightEdgeWithEastVelocity(SimTick tick)
    {
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        MatchState state = CreateMatchState(
            tick,
            new[] { CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)) },
            CreateTankWithMovement(
                0,
                0,
                new FixedVec2(maxX, Fixed.FromInt(20)),
                FixedVec2.FromInts(1, 0)));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateBoundsProjectileHitInitialRuntime(SimTick tick)
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
            CreateTankWithMovement(
                0,
                0,
                new FixedVec2(maxX, Fixed.FromInt(20)),
                FixedVec2.FromInts(1, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)));

        SimTick spawnTick = tick.Value > 0
            ? new SimTick(tick.Value - 1)
            : new SimTick(0);
        ProjectileState[] projectiles =
        {
            CreateSeededProjectile(
                FixedVec2.FromInts(100, 20),
                spawnTick,
                ownerTankId: 1),
        };

        return CreateRuntime(
            new MatchState(
                state.Arena,
                state.CurrentTick,
                state.Tanks,
                state.Loadouts,
                projectiles),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
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

    #region Validation

    [Fact]
    public void RecordUntilEnd_NullInitialRuntime_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeReplayRecorder.RecordUntilEnd(
                null!,
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_NegativeMaxTicks_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: -1));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_NullPrograms_WithRunningFixture_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                null!,
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void RecordUntilEnd_program_count_mismatch()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedRuntimeReplayRecorder.RecordUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Zero-tick / multi-tick

    [Fact]
    public void RecordUntilEnd_MaxTicksZero_RecordsOnlyInitialFrame()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 0);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(0, result.RunResult.TicksExecuted);
        Assert.Single(result.Recording.Frames);
        Assert.Equal(0, result.Recording.Frames[0].FrameIndex);
        Assert.Same(initialRuntime.State, result.Recording.Frames[0].State);
        Assert.Same(initialRuntime.State, result.RunResult.FinalState);
    }

    [Fact]
    public void RecordUntilEnd_ScanOnly_RecordsFramePerTickPlusInitial()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.RunResult.TicksExecuted);
        Assert.Equal(3, result.Recording.Frames.Count);

        for (int i = 0; i <= 2; i++)
        {
            Assert.Equal(i, result.Recording.Frames[i].FrameIndex);
            Assert.Equal(new SimTick(i), result.Recording.Frames[i].State.CurrentTick);
        }

        Assert.Same(
            result.RunResult.FinalState,
            result.Recording.Frames[^1].State);
    }

    [Fact]
    public void RecordUntilEnd_FireProgram_RecordsPostTickState()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(2, result.Recording.Frames.Count);
        Assert.Equal(new SimTick(1), result.Recording.Frames[^1].State.CurrentTick);
        Assert.Same(
            result.RunResult.FinalState,
            result.Recording.Frames[^1].State);
        Assert.Single(result.Recording.Frames[^1].State.Projectiles);
        Assert.True(result.Recording.Frames[^1].State.Projectiles[0].IsActive);
        Assert.Equal(new SimTick(0), result.Recording.Frames[^1].State.Projectiles[0].SpawnTick);
    }

    [Fact]
    public void RecordUntilEnd_Fire_OwnerHpUnchanged_OnSpawnTick()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        MatchState finalState = result.Recording.Frames[^1].State;

        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, finalState.Tanks[1].CurrentHitPoints);
        Assert.Single(finalState.Projectiles);
        Assert.True(finalState.Projectiles[0].IsActive);
        Assert.Equal(new SimTick(0), finalState.Projectiles[0].SpawnTick);
        Assert.Equal(new TankId(1), finalState.Projectiles[0].OwnerTankId);
    }

    #endregion

    #region Determinism / replay policy

    [Fact]
    public void RecordUntilEnd_Deterministic_repeated_calls()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            ScanWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        MatchRecordedRunResult a = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 2);
        MatchRecordedRunResult b = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 2);

        Assert.Equal(a.RunResult.TicksExecuted, b.RunResult.TicksExecuted);
        Assert.Equal(a.RunResult.EndCondition.Reason, b.RunResult.EndCondition.Reason);
        Assert.Equal(a.Recording.Frames.Count, b.Recording.Frames.Count);
        for (int i = 0; i < a.Recording.Frames.Count; i++)
        {
            Assert.Equal(
                a.Recording.Frames[i].State.CurrentTick,
                b.Recording.Frames[i].State.CurrentTick);
        }
    }

    [Fact]
    public void Recording_FramesStoreMatchStateOnly()
    {
        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            CreateTwoTankRuntime(new SimTick(0)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.All(
            result.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    #endregion

    #region Movement application regression (5.106)

    [Fact]
    public void RecordUntilEnd_MovementCommand_FinalFrameHasVelocityAndMovedPosition()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedPatrolVelocity();
        FixedVec2 expectedFinalPosition = initialPosition + velocity;

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.Equal(2, result.Recording.Frames.Count);
        Assert.Equal(initialPosition, result.Recording.Frames[0].State.Tanks[0].Movement.Position);

        MatchState finalState = result.Recording.Frames[^1].State;
        Assert.Equal(velocity, finalState.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(expectedFinalPosition, finalState.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(1), finalState.CurrentTick);
        Assert.Same(result.RunResult.FinalState, finalState);
    }

    [Fact]
    public void RecordUntilEnd_MovementRepeatedTicks_FramesShowIntegratedPosition()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedPatrolVelocity();

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(3, result.Recording.Frames.Count);
        Assert.Equal(initialPosition, result.Recording.Frames[0].State.Tanks[0].Movement.Position);
        Assert.Equal(
            initialPosition + velocity,
            result.Recording.Frames[1].State.Tanks[0].Movement.Position);
        Assert.Equal(
            initialPosition + velocity + velocity,
            result.Recording.Frames[2].State.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(2), result.Recording.Frames[2].State.CurrentTick);
        Assert.Same(result.RunResult.FinalState, result.Recording.Frames[^1].State);
    }

    [Fact]
    public void RecordUntilEnd_MovementFramesRemainMatchStateOnly()
    {
        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            CreateSingleTankRuntime(new SimTick(0)),
            new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.All(
            result.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    [Fact]
    public void RecordUntilEnd_MovementPastArenaBounds_FinalFrameIsClamped()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime =
            CreateRuntimeAtRightEdgeWithEastVelocity(startTick);
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        FixedVec2 edgePosition = new FixedVec2(maxX, Fixed.FromInt(20));

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { NoOpWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.Equal(2, result.Recording.Frames.Count);
        Assert.Equal(edgePosition, result.Recording.Frames[0].State.Tanks[0].Movement.Position);
        Assert.Equal(maxX, result.Recording.Frames[^1].State.Tanks[0].Movement.Position.X);
        Assert.All(
            result.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    [Fact]
    public void RecordUntilEnd_MovementBoundsProjectileHit_FinalFrameShowsDamageAndClamp()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateBoundsProjectileHitInitialRuntime(startTick);
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        FixedVec2 edgePosition = new FixedVec2(maxX, Fixed.FromInt(20));

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.Equal(2, result.Recording.Frames.Count);
        Assert.Equal(edgePosition, result.Recording.Frames[0].State.Tanks[0].Movement.Position);
        Assert.Equal(maxX, result.Recording.Frames[^1].State.Tanks[0].Movement.Position.X);
        Assert.Equal(80, result.Recording.Frames[^1].State.Tanks[0].CurrentHitPoints);
        Assert.Same(result.RunResult.FinalState, result.Recording.Frames[^1].State);
        Assert.All(
            result.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    #endregion

    #region Wall obstacle integration regression (5.130)

    private static FixedVec2 WallRegressionPrePosition()
        => FixedVec2.FromInts(5, 15);

    private static FixedVec2 WallRegressionVelocity()
        => FixedVec2.FromInts(3, 0);

    private static WallBlock CreateWall(string id, FixedRect bounds)
        => new WallBlock(id, bounds);

    private static FixedRect UnitWallAtTenByTen()
        => FixedRect.FromMinSize(
            FixedVec2.FromInts(10, 10),
            Fixed.FromInt(10),
            Fixed.FromInt(10));

    private static ArenaDefinition CreateWallArena(params WallBlock[] wallBlocks)
    {
        return new ArenaDefinition(
            id: "replay_wall_regression_arena",
            displayName: "Replay Wall Regression Arena",
            description: "Arena for combined replay wall regression tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
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

    private static MatchSensorRuntimeState CreateWallBlockedMovementRuntime(SimTick tick)
    {
        ArenaDefinition arena = CreateWallArena(CreateWall("wall_a", UnitWallAtTenByTen()));
        MatchState state = CreateMatchState(
            arena,
            tick,
            new[] { CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)) },
            CreateTankWithMovement(
                0,
                0,
                WallRegressionPrePosition(),
                WallRegressionVelocity()));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void RecordUntilEnd_WallBlockedMovement_FinalFrameShowsRollback()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 prePosition = WallRegressionPrePosition();
        MatchSensorRuntimeState initialRuntime = CreateWallBlockedMovementRuntime(startTick);

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { NoOpWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.Equal(2, result.Recording.Frames.Count);
        Assert.Equal(prePosition, result.Recording.Frames[^1].State.Tanks[0].Movement.Position);
        Assert.All(
            result.Recording.Frames,
            frame => Assert.IsType<MatchState>(frame.State));
    }

    #endregion

    #region Gradual turret rotation replay regression (5.145)

    [Fact]
    public void RecordUntilEnd_RepeatedAimAtEnemy_FramesShowGradualTurretRotation()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 ownerPosition = Vec(10, 10);
        FixedVec2 enemyPosition = Vec(20, 20);
        MatchSensorRuntimeState initialRuntime =
            CreateTwoTankAimRuntime(startTick, ownerPosition, enemyPosition);
        TankState ownerTank = initialRuntime.State.Tanks[0];
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed afterTick1 = ExpectedTurnStep(Fixed.Zero, desiredRotation, ownerTank);
        Fixed afterTick2 = ExpectedTurnStep(afterTick1, desiredRotation, ownerTank);

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            AimAtEnemyThenNoOpPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.RunResult.TicksExecuted);
        Assert.Equal(3, result.Recording.Frames.Count);

        for (int i = 0; i <= 2; i++)
        {
            Assert.Equal(i, result.Recording.Frames[i].FrameIndex);
            Assert.Equal(new SimTick(i), result.Recording.Frames[i].State.CurrentTick);
        }

        Assert.Equal(Fixed.Zero, result.Recording.Frames[0].State.Tanks[0].TurretRotation);
        Assert.Equal(100, afterTick1.Raw);
        Assert.Equal(
            afterTick1,
            result.Recording.Frames[1].State.Tanks[0].TurretRotation);
        Assert.Equal(124, afterTick2.Raw);
        Assert.Equal(
            afterTick2,
            result.Recording.Frames[2].State.Tanks[0].TurretRotation);
        Assert.Same(
            result.RunResult.FinalState,
            result.Recording.Frames[^1].State);
    }

    [Fact]
    public void RecordUntilEnd_OneTickAimAtEnemy_FrameShowsFirstTurnStepOnly()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 ownerPosition = Vec(10, 10);
        FixedVec2 enemyPosition = Vec(20, 20);
        MatchSensorRuntimeState initialRuntime =
            CreateTwoTankAimRuntime(startTick, ownerPosition, enemyPosition);
        TankState ownerTank = initialRuntime.State.Tanks[0];
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed afterTick1 = ExpectedTurnStep(Fixed.Zero, desiredRotation, ownerTank);

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            AimAtEnemyThenNoOpPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(2, result.Recording.Frames.Count);
        Assert.Equal(Fixed.Zero, result.Recording.Frames[0].State.Tanks[0].TurretRotation);
        Assert.Equal(afterTick1, result.Recording.Frames[1].State.Tanks[0].TurretRotation);
        Assert.NotEqual(
            desiredRotation,
            result.Recording.Frames[1].State.Tanks[0].TurretRotation);
        Assert.Same(
            result.RunResult.FinalState,
            result.Recording.Frames[^1].State);
    }

    #endregion

    #region Aim-until-fire replay regression (5.153)

    [Fact]
    public void RecordUntilEnd_AimUntilAlignedThenFire_FramesShowAimThenFire()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 ownerPosition = Vec(10, 10);
        FixedVec2 enemyPosition = Vec(20, 20);
        MatchSensorRuntimeState initialRuntime =
            CreateTwoTankAimRuntime(startTick, ownerPosition, enemyPosition);
        TankState ownerTank = initialRuntime.State.Tanks[0];
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed afterTick1 = ExpectedTurnStep(Fixed.Zero, desiredRotation, ownerTank);
        Fixed afterTick2 = ExpectedTurnStep(afterTick1, desiredRotation, ownerTank);

        MatchRecordedRunResult result = CombinedRuntimeReplayRecorder.RecordUntilEnd(
            initialRuntime,
            AimUntilFireTankZeroPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 3);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(3, result.RunResult.TicksExecuted);
        Assert.Equal(4, result.Recording.Frames.Count);

        for (int i = 0; i <= 3; i++)
        {
            Assert.Equal(i, result.Recording.Frames[i].FrameIndex);
            Assert.Equal(new SimTick(i), result.Recording.Frames[i].State.CurrentTick);
        }

        Assert.Equal(Fixed.Zero, result.Recording.Frames[0].State.Tanks[0].TurretRotation);
        Assert.Empty(result.Recording.Frames[0].State.Projectiles);
        Assert.Equal(100, afterTick1.Raw);
        Assert.Equal(
            afterTick1,
            result.Recording.Frames[1].State.Tanks[0].TurretRotation);
        Assert.Empty(result.Recording.Frames[1].State.Projectiles);
        Assert.Equal(124, afterTick2.Raw);
        Assert.Equal(
            afterTick2,
            result.Recording.Frames[2].State.Tanks[0].TurretRotation);
        Assert.Empty(result.Recording.Frames[2].State.Projectiles);
        Assert.Equal(
            afterTick2,
            result.Recording.Frames[3].State.Tanks[0].TurretRotation);
        Assert.Single(result.Recording.Frames[3].State.Projectiles);
        Assert.True(result.Recording.Frames[3].State.Projectiles[0].IsActive);
        Assert.Equal(
            new SimTick(2),
            result.Recording.Frames[3].State.Projectiles[0].SpawnTick);
        Assert.Same(
            result.RunResult.FinalState,
            result.Recording.Frames[^1].State);
    }

    #endregion
}
