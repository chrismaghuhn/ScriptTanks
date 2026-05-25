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

public sealed class CombinedRuntimeRunnerTests
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

    private static FixedVec2 ExpectedForward(Fixed rotation)
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.True(result.IsResolved);
        return result.Forward!.Value;
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

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position,
        TankDefinition definition,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            definition,
            new MovementState(position, FixedVec2.Zero),
            definition.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: turretRotation);
    }

    private static TankDefinition CreateStaticTurretTankDefinition()
    {
        return new TankDefinition(
            id: "static_turret_runner_test",
            displayName: "Static Turret Runner Test",
            description: "Test tank with zero turret turn rate.",
            stats: new BasicTankStats(
                maxHitPoints: TankCatalog.BasicTank.Stats.MaxHitPoints,
                armorReductionPercent: TankCatalog.BasicTank.Stats.ArmorReductionPercent,
                hitboxRadius: TankCatalog.BasicTank.Stats.HitboxRadius,
                maxVelocityPerTick: TankCatalog.BasicTank.Stats.MaxVelocityPerTick,
                bodyTurnRatePerTick: TankCatalog.BasicTank.Stats.BodyTurnRatePerTick,
                turretTurnRatePerTick: Fixed.Zero),
            tags: new[] { "test" });
    }

    private static MatchSensorRuntimeState CreateTwoTankAimRuntimeWithOwnerDefinition(
        SimTick tick,
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition,
        TankDefinition ownerDefinition,
        Fixed ownerTurretRotation = default)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, ownerPosition, ownerDefinition, ownerTurretRotation),
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

    private static MatchSensorRuntimeState CreateInitiallyEndedByTankDestroyedRuntime()
    {
        TankState alive = CreateTank(0, 0, FixedVec2.FromInts(10, 20), hp: 100);
        TankState destroyed = CreateTank(1, 1, FixedVec2.FromInts(90, 20), hp: 0);
        MatchState state = CreateMatchState(
            new SimTick(0),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            alive,
            destroyed);
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    #region Validation

    [Fact]
    public void RunUntilEnd_NullInitialRuntime_Throws_ParamName_initialRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeRunner.RunUntilEnd(
                null!,
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_NegativeMaxTicks_Throws_ParamName_maxTicks()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombinedRuntimeRunner.RunUntilEnd(
                CreateSingleTankRuntime(new SimTick(0)),
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0),
                maxTicks: -1));

        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_NullPrograms_Throws_ParamName_programs()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeRunner.RunUntilEnd(
                CreateTwoTankRuntime(new SimTick(0)),
                null!,
                new ProjectileIdSequence(0),
                maxTicks: 1));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Zero-tick / initially-ended

    [Fact]
    public void RunUntilEnd_ReturnsImmediately_WhenMaxTicksZero()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { ScanWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 0);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(0, result.RunResult.TicksExecuted);
        Assert.False(result.HasLastTickResult);
        Assert.Null(result.LastTickResult);
        Assert.Same(initialRuntime, result.FinalRuntime);
        Assert.Equal(new SimTick(0), result.FinalRuntime.State.CurrentTick);
        Assert.Same(initialRuntime.State, result.RunResult.FinalState);
    }

    [Fact]
    public void RunUntilEnd_ReturnsImmediately_WhenInitiallyEnded()
    {
        MatchSensorRuntimeState initialRuntime = CreateInitiallyEndedByTankDestroyedRuntime();
        TankState alive = initialRuntime.State.Tanks[0];

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 100);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.RunResult.EndCondition.Reason);
        Assert.True(result.RunResult.EndCondition.WinnerTankId.HasValue);
        Assert.Equal(alive.Id, result.RunResult.EndCondition.WinnerTankId!.Value);
        Assert.Equal(0, result.RunResult.TicksExecuted);
        Assert.False(result.HasLastTickResult);
        Assert.Same(initialRuntime, result.FinalRuntime);
        Assert.Equal(new SimTick(0), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Multi-tick run

    [Fact]
    public void RunUntilEnd_ExecutesTicksUntilTimeout()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
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
        Assert.Equal(new SimTick(2), result.FinalRuntime.State.CurrentTick);
        Assert.True(result.HasLastTickResult);
        Assert.NotNull(result.LastTickResult);
        Assert.Same(result.FinalRuntime, result.LastTickResult!.FinalRuntime);
    }

    [Fact]
    public void RunUntilEnd_ReturnsCombinedRuntimeRunResult_WithMatchingRunResult()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        ProjectileIdSequence initialSequence = new ProjectileIdSequence(7);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                FireWhenAlways(),
            },
            initialSequence,
            maxTicks: 1);

        Assert.Same(result.FinalRuntime.State, result.RunResult.FinalState);
        Assert.Same(result.FinalRuntime, result.LastTickResult!.FinalRuntime);
        Assert.Equal(result.FinalProjectileIdSequence, result.LastTickResult.FinalProjectileIdSequence);
        Assert.Equal(8, result.FinalProjectileIdSequence.NextValue);
        Assert.Equal(1, result.RunResult.TicksExecuted);
    }

    [Fact]
    public void RunUntilEnd_Fire_SpawnTickEnemyDamaged_OwnerUnharmed()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateEnemyHitTwoTankRuntime(startTick);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            ScanThenFirePrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.True(result.LastTickResult!.ScriptResult.FireApplicationResult.Records[1].DidApply);
        ProjectileState spawned =
            result.LastTickResult.ScriptResult.FinalRuntime.State.Projectiles[0];
        Assert.Equal(startTick, spawned.SpawnTick);
        Assert.Equal(new TankId(1), spawned.OwnerTankId);

        MatchState finalState = result.FinalRuntime.State;
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, finalState.Tanks[1].CurrentHitPoints);
        Assert.Equal(80, finalState.Tanks[0].CurrentHitPoints);
        Assert.Equal(new SimTick(1), finalState.CurrentTick);
        Assert.Empty(finalState.Projectiles);
    }

    #endregion

    #region Movement application regression (5.106)

    [Fact]
    public void RunUntilEnd_MovementCommand_FinalRuntimeMovedPosition()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 expectedPosition = initialPosition + ExpectedPatrolVelocity();

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(ExpectedPatrolVelocity(), tank.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank.Movement.Position);
        Assert.Equal(new SimTick(1), result.FinalRuntime.State.CurrentTick);
        Assert.Same(result.FinalRuntime.State, result.RunResult.FinalState);
    }

    [Fact]
    public void RunUntilEnd_MovementRepeatedTicks_IntegratesPositionEachTick()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedPatrolVelocity();
        FixedVec2 expectedPosition = initialPosition + velocity + velocity;

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 2);

        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(2, result.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(2), result.FinalRuntime.State.CurrentTick);
        Assert.Equal(velocity, tank.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank.Movement.Position);
    }

    [Fact]
    public void RunUntilEnd_RetreatCommand_IntegratesNegativePositionDelta()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateSingleTankRuntime(startTick);
        FixedVec2 initialPosition = initialRuntime.State.Tanks[0].Movement.Position;
        FixedVec2 velocity = ExpectedRetreatVelocity();
        FixedVec2 expectedPosition = initialPosition + velocity;

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { RetreatWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(velocity, tank.Movement.VelocityPerTick);
        Assert.Equal(expectedPosition, tank.Movement.Position);
        Assert.Equal(new SimTick(1), result.FinalRuntime.State.CurrentTick);
        Assert.Same(result.FinalRuntime.State, result.RunResult.FinalState);
    }

    [Fact]
    public void RunUntilEnd_MovementPastArenaBounds_FinalRuntimeIsClamped()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime =
            CreateRuntimeAtRightEdgeWithEastVelocity(startTick);
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { NoOpWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(maxX, tank.Movement.Position.X);
        Assert.Equal(velocity, tank.Movement.VelocityPerTick);
        Assert.Equal(new SimTick(1), result.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void RunUntilEnd_MovementBoundsProjectileHit_FinalRuntimeShowsDamageAndClamp()
    {
        SimTick startTick = new SimTick(0);
        MatchSensorRuntimeState initialRuntime = CreateBoundsProjectileHitInitialRuntime(startTick);
        Fixed maxX = ArenaCatalog.OpenTestArena.Bounds.Width
            - TankCatalog.BasicTank.Stats.HitboxRadius;
        FixedVec2 velocity = FixedVec2.FromInts(1, 0);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { NoOpWhenAlways(), NoOpWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        TankState tank = result.FinalRuntime.State.Tanks[0];

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(maxX, tank.Movement.Position.X);
        Assert.Equal(80, tank.CurrentHitPoints);
        Assert.Equal(velocity, tank.Movement.VelocityPerTick);
        Assert.Equal(new SimTick(1), result.FinalRuntime.State.CurrentTick);
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
            id: "runner_wall_regression_arena",
            displayName: "Runner Wall Regression Arena",
            description: "Arena for combined runner wall regression tests.",
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
    public void RunUntilEnd_WallBlockedMovement_FinalRuntimeShowsRollback()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 prePosition = WallRegressionPrePosition();
        MatchSensorRuntimeState initialRuntime = CreateWallBlockedMovementRuntime(startTick);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram> { NoOpWhenAlways() },
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(prePosition, result.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(1), result.FinalRuntime.State.CurrentTick);
    }

    #endregion

    #region Gradual turret rotation runner regression (5.145)

    [Fact]
    public void RunUntilEnd_RepeatedAimAtEnemy_ConvergesToDesiredRotation()
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

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            AimAtEnemyThenNoOpPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(2), result.FinalRuntime.State.CurrentTick);
        Assert.NotEqual(desiredRotation, afterTick1);
        Assert.Equal(afterTick1, ExpectedTurnStep(Fixed.Zero, desiredRotation, ownerTank));
        Assert.Equal(afterTick2, desiredRotation);
        Assert.Equal(124, afterTick2.Raw);
        Assert.Equal(
            afterTick2,
            result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void RunUntilEnd_OneTickAimAtEnemy_StopsAtFirstTurnStep()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 ownerPosition = Vec(10, 10);
        FixedVec2 enemyPosition = Vec(20, 20);
        MatchSensorRuntimeState initialRuntime =
            CreateTwoTankAimRuntime(startTick, ownerPosition, enemyPosition);
        TankState ownerTank = initialRuntime.State.Tanks[0];
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed afterTick1 = ExpectedTurnStep(Fixed.Zero, desiredRotation, ownerTank);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            AimAtEnemyThenNoOpPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 1);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(1, result.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(1), result.FinalRuntime.State.CurrentTick);
        Assert.Equal(100, afterTick1.Raw);
        Assert.Equal(afterTick1, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.NotEqual(desiredRotation, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void RunUntilEnd_RepeatedAimAtEnemy_WithZeroTurnRate_DoesNotMoveTurret()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 ownerPosition = Vec(10, 10);
        FixedVec2 enemyPosition = Vec(20, 20);
        Fixed initialTurret = Fixed.FromRatio(1, 8);
        TankDefinition staticTurret = CreateStaticTurretTankDefinition();
        MatchSensorRuntimeState initialRuntime = CreateTwoTankAimRuntimeWithOwnerDefinition(
            startTick,
            ownerPosition,
            enemyPosition,
            staticTurret,
            initialTurret);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            AimAtEnemyThenNoOpPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.RunResult.TicksExecuted);
        Assert.NotEqual(Fixed.Zero, initialTurret);
        Assert.NotEqual(desiredRotation, initialTurret);
        Assert.Equal(initialTurret, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    #endregion

    #region Aim-until-fire runner regression (5.153)

    [Fact]
    public void RunUntilEnd_AimUntilAlignedThenFire_NoFireBeforeAlignment()
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

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            AimUntilFireTankZeroPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(2, result.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(2), result.FinalRuntime.State.CurrentTick);
        Assert.Equal(afterTick2, desiredRotation);
        Assert.Equal(124, afterTick2.Raw);
        Assert.Equal(afterTick2, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
        Assert.True(result.HasLastTickResult);

        MatchScriptIntentIntegrationRecord tank0 =
            result.LastTickResult!.ScriptResult.IntegrationResult.GetRecordAtIndex(0);
        Assert.Equal(1, tank0.EvaluationRecord.Result.Decision.RoutineIndex);
        Assert.Equal(
            ScriptCommandType.AimAtEnemy,
            tank0.EvaluationRecord.Result.Decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, tank0.Context.TurretAimStatus.Status);
    }

    [Fact]
    public void RunUntilEnd_AimUntilAlignedThenFire_FiresOnThirdTick()
    {
        SimTick startTick = new SimTick(0);
        FixedVec2 ownerPosition = Vec(10, 10);
        FixedVec2 enemyPosition = Vec(20, 20);
        MatchSensorRuntimeState initialRuntime =
            CreateTwoTankAimRuntime(startTick, ownerPosition, enemyPosition);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);

        CombinedRuntimeRunResult result = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            AimUntilFireTankZeroPrograms(),
            new ProjectileIdSequence(0),
            maxTicks: 3);

        Assert.True(result.RunResult.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.RunResult.EndCondition.Reason);
        Assert.Equal(3, result.RunResult.TicksExecuted);
        Assert.Equal(new SimTick(3), result.FinalRuntime.State.CurrentTick);
        Assert.Equal(desiredRotation, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.True(result.HasLastTickResult);

        MatchScriptIntentIntegrationRecord tank0 =
            result.LastTickResult!.ScriptResult.IntegrationResult.GetRecordAtIndex(0);
        Assert.Equal(0, tank0.EvaluationRecord.Result.Decision.RoutineIndex);
        Assert.Equal(
            ScriptCommandType.Fire,
            tank0.EvaluationRecord.Result.Decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, tank0.Context.TurretAimStatus.Status);

        MatchFireRequest fireRequest =
            result.LastTickResult.ScriptResult.FireConstructionPipelineResult
                .ConstructionResult.Records[0].FireRequest!.Value;
        FixedVec2 expectedForward = ExpectedForward(desiredRotation);
        Assert.Equal(
            expectedForward * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick,
            fireRequest.FireVelocity);
        Assert.Equal(new TankId(0), result.FinalRuntime.State.Projectiles[0].OwnerTankId);
    }

    #endregion

    #region Purity / determinism

    [Fact]
    public void RunUntilEnd_DoesNotMutateInitialRuntimeReference()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        SimTick tickBefore = initialRuntime.State.CurrentTick;
        TankState tank0Before = initialRuntime.State.Tanks[0];
        MatchSensorLoadoutState loadoutsBefore = initialRuntime.SensorLoadouts;

        _ = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0),
            maxTicks: 2);

        Assert.Equal(tickBefore, initialRuntime.State.CurrentTick);
        Assert.Equal(tank0Before, initialRuntime.State.Tanks[0]);
        Assert.Same(loadoutsBefore, initialRuntime.SensorLoadouts);
    }

    [Fact]
    public void RunUntilEnd_Deterministic_repeated_calls()
    {
        MatchSensorRuntimeState initialRuntime = CreateTwoTankRuntime(new SimTick(0));
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            ScanWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        CombinedRuntimeRunResult a = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 2);
        CombinedRuntimeRunResult b = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            sequence,
            maxTicks: 2);

        Assert.Equal(a.RunResult.TicksExecuted, b.RunResult.TicksExecuted);
        Assert.Equal(a.FinalProjectileIdSequence, b.FinalProjectileIdSequence);
        Assert.Equal(a.RunResult.EndCondition.Reason, b.RunResult.EndCondition.Reason);
        Assert.Equal(
            a.FinalRuntime.State.CurrentTick,
            b.FinalRuntime.State.CurrentTick);
    }

    #endregion
}
