using System;
using System.Collections.Generic;
using System.Reflection;
using ScriptTanks.Core.Arena;
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

public sealed class CombinedRuntimeTickResultTests
{
    private sealed class ValidTickShape
    {
        public MatchSensorRuntimeState Runtime { get; init; } = null!;

        public CombinedScriptRuntimeComposerResult ScriptResult { get; init; } = null!;

        public MatchStateTankMovementResult TankMovementResult { get; init; } = null!;

        public MatchStateTankBoundsResult TankBoundsResult { get; init; } = null!;

        public MatchStateTankObstacleCollisionResult TankObstacleCollisionResult { get; init; } = null!;

        public MatchState SteppedState { get; init; } = null!;

        public MatchSensorRuntimeState FinalRuntime { get; init; } = null!;

        public ProjectileIdSequence FinalSequence { get; init; }
    }

    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
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

    private static MatchSensorRuntimeState CreateSingleTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
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

    private static (
        MatchStateTankMovementResult TankMovement,
        MatchStateTankBoundsResult TankBounds,
        MatchStateTankObstacleCollisionResult TankObstacleCollision,
        MatchState SteppedState)
        BuildTankMovementBoundsObstacleAndSteppedState(CombinedScriptRuntimeComposerResult scriptResult)
    {
        MatchStateTankMovementResult tankMovement =
            MatchStateTankMovementPipeline.Step(scriptResult.FinalRuntime.State);

        MatchStateTankBoundsResult tankBounds =
            MatchStateTankBoundsPipeline.Step(tankMovement.FinalState);

        MatchStateTankObstacleCollisionResult tankObstacleCollision =
            MatchStateTankObstacleCollisionPipeline.Step(
                tankMovement.InitialState,
                tankBounds.FinalState);

        MatchState projectileResolved =
            MatchTickProjectilePipeline.StepProjectilesAndResolveHits(
                tankObstacleCollision.FinalState);

        MatchState steppedState = MatchStateTickAdvanceSystem.AdvanceTick(projectileResolved);

        return (tankMovement, tankBounds, tankObstacleCollision, steppedState);
    }

    private static ValidTickShape BuildValidTickShape(
        MatchSensorRuntimeState runtime,
        ProjectileIdSequence sequence,
        params ScriptProgram[] programs)
    {
        CombinedScriptRuntimeComposerResult scriptResult =
            CombinedScriptRuntimeComposer.Run(runtime, programs, sequence);

        (MatchStateTankMovementResult tankMovement, MatchStateTankBoundsResult tankBounds, MatchStateTankObstacleCollisionResult tankObstacleCollision, MatchState steppedState) =
            BuildTankMovementBoundsObstacleAndSteppedState(scriptResult);

        MatchSensorRuntimeState finalRuntime =
            scriptResult.FinalRuntime.WithState(steppedState);

        return new ValidTickShape
        {
            Runtime = runtime,
            ScriptResult = scriptResult,
            TankMovementResult = tankMovement,
            TankBoundsResult = tankBounds,
            TankObstacleCollisionResult = tankObstacleCollision,
            SteppedState = steppedState,
            FinalRuntime = finalRuntime,
            FinalSequence = scriptResult.FinalProjectileIdSequence,
        };
    }

    private static CombinedRuntimeTickResult CreateResult(ValidTickShape shape)
    {
        return new CombinedRuntimeTickResult(
            shape.Runtime,
            shape.ScriptResult,
            shape.TankMovementResult,
            shape.TankBoundsResult,
            shape.TankObstacleCollisionResult,
            shape.SteppedState,
            shape.FinalRuntime,
            shape.FinalSequence);
    }

    [Fact]
    public void Constructor_preserves_all_references_and_values()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedRuntimeTickResult result = CreateResult(shape);

        Assert.Same(shape.Runtime, result.InitialRuntime);
        Assert.Same(shape.ScriptResult, result.ScriptResult);
        Assert.Same(shape.TankMovementResult, result.TankMovementResult);
        Assert.Same(shape.TankBoundsResult, result.TankBoundsResult);
        Assert.Same(shape.TankObstacleCollisionResult, result.TankObstacleCollisionResult);
        Assert.Same(shape.SteppedState, result.SteppedState);
        Assert.Same(shape.FinalRuntime, result.FinalRuntime);
        Assert.Same(shape.Runtime, result.ScriptResult.InitialRuntime);
        Assert.Same(
            shape.ScriptResult.FinalRuntime.State,
            result.TankMovementResult.InitialState);
        Assert.Same(
            result.TankMovementResult.FinalState,
            result.TankBoundsResult.InitialState);
        Assert.Same(
            result.TankMovementResult.InitialState,
            result.TankObstacleCollisionResult.PreMovementState);
        Assert.Same(
            result.TankBoundsResult.FinalState,
            result.TankObstacleCollisionResult.CandidateState);
        Assert.Same(shape.SteppedState, result.FinalRuntime.State);
        Assert.Same(
            shape.ScriptResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
        Assert.Equal(shape.FinalSequence, result.FinalProjectileIdSequence);
        Assert.Equal(
            shape.ScriptResult.FinalProjectileIdSequence.NextValue,
            result.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void Constructor_NullInitialRuntime_Throws_ParamName_initialRuntime()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                null!,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullScriptResult_Throws_ParamName_scriptResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                null!,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("scriptResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullTankMovementResult_Throws_ParamName_tankMovementResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                null!,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankMovementResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullTankBoundsResult_Throws_ParamName_tankBoundsResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                null!,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankBoundsResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullTankObstacleCollisionResult_Throws_ParamName_tankObstacleCollisionResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                null!,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankObstacleCollisionResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullSteppedState_Throws_ParamName_steppedState()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                null!,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("steppedState", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullFinalRuntime_Throws_ParamName_finalRuntime()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                null!,
                shape.FinalSequence));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedInitialRuntime_Throws_ParamName_initialRuntime()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchSensorRuntimeState otherRuntime = CreateTwoTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                otherRuntime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_TankMovementInitialStateMismatch_Throws_ParamName_tankMovementResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchState otherState = shape.ScriptResult.InitialRuntime.State;

        MatchStateTankMovementResult wrongTankMovement = new MatchStateTankMovementResult(
            otherState,
            shape.TankMovementResult.FinalState,
            shape.TankMovementResult.Records);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                wrongTankMovement,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankMovementResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_TankBoundsInitialStateMismatch_Throws_ParamName_tankBoundsResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchState wrongInitial = shape.ScriptResult.FinalRuntime.State;

        MatchStateTankBoundsResult wrongTankBounds = new MatchStateTankBoundsResult(
            wrongInitial,
            shape.TankBoundsResult.FinalState,
            shape.TankBoundsResult.Records);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                wrongTankBounds,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankBoundsResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_TankObstaclePreMovementMismatch_Throws_ParamName_tankObstacleCollisionResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchStateTankObstacleCollisionResult wrongObstacle =
            MatchStateTankObstacleCollisionPipeline.Step(
                shape.TankMovementResult.FinalState,
                shape.TankBoundsResult.FinalState);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                wrongObstacle,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankObstacleCollisionResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_TankObstacleCandidateMismatch_Throws_ParamName_tankObstacleCollisionResult()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchStateTankObstacleCollisionResult wrongObstacle =
            MatchStateTankObstacleCollisionPipeline.Step(
                shape.TankObstacleCollisionResult.PreMovementState,
                shape.TankMovementResult.FinalState);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                wrongObstacle,
                shape.SteppedState,
                shape.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("tankObstacleCollisionResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalRuntimeWrongState_Throws_ParamName_finalRuntime()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                shape.Runtime,
                shape.ScriptResult,
                shape.TankMovementResult,
                shape.TankBoundsResult,
                shape.TankObstacleCollisionResult,
                shape.SteppedState,
                shape.ScriptResult.FinalRuntime,
                shape.FinalSequence));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalRuntimeWrongSensorLoadouts_Throws_ParamName_finalRuntime()
    {
        ValidTickShape validA = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ValidTickShape validB = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchSensorRuntimeState wrongFinalRuntime = validA.FinalRuntime.WithSensorLoadouts(
            validB.ScriptResult.FinalRuntime.SensorLoadouts);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                validA.Runtime,
                validA.ScriptResult,
                validA.TankMovementResult,
                validA.TankBoundsResult,
                validA.TankObstacleCollisionResult,
                validA.SteppedState,
                wrongFinalRuntime,
                validA.FinalSequence));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalProjectileIdSequenceMismatch_Throws_ParamName_finalProjectileIdSequence()
    {
        ValidTickShape valid = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ProjectileIdSequence wrongSequence = new ProjectileIdSequence(
            valid.ScriptResult.FinalProjectileIdSequence.NextValue + 1);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeTickResult(
                valid.Runtime,
                valid.ScriptResult,
                valid.TankMovementResult,
                valid.TankBoundsResult,
                valid.TankObstacleCollisionResult,
                valid.SteppedState,
                valid.FinalRuntime,
                wrongSequence));

        Assert.Equal("finalProjectileIdSequence", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_tank_movement_result()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedRuntimeTickResult result = CreateResult(shape);

        Assert.Same(shape.TankMovementResult, result.TankMovementResult);
        Assert.Same(shape.ScriptResult.FinalRuntime.State, result.TankMovementResult.InitialState);
    }

    [Fact]
    public void Constructor_preserves_tank_bounds_result()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedRuntimeTickResult result = CreateResult(shape);

        Assert.Same(shape.TankBoundsResult, result.TankBoundsResult);
        Assert.Same(shape.TankMovementResult.FinalState, result.TankBoundsResult.InitialState);
    }

    [Fact]
    public void Constructor_preserves_tank_obstacle_collision_result()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedRuntimeTickResult result = CreateResult(shape);

        Assert.Same(shape.TankObstacleCollisionResult, result.TankObstacleCollisionResult);
        Assert.Same(
            shape.TankMovementResult.InitialState,
            result.TankObstacleCollisionResult.PreMovementState);
        Assert.Same(
            shape.TankBoundsResult.FinalState,
            result.TankObstacleCollisionResult.CandidateState);
    }

    [Fact]
    public void Constructor_AcceptsCorrectTickShape()
    {
        ValidTickShape shape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedRuntimeTickResult result = CreateResult(shape);

        Assert.Same(shape.FinalRuntime, result.FinalRuntime);
        Assert.Same(shape.SteppedState, result.FinalRuntime.State);
        Assert.Same(
            shape.ScriptResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
        Assert.Equal(shape.FinalSequence, result.FinalProjectileIdSequence);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equals = typeof(CombinedRuntimeTickResult).GetMethod(
            "Equals",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equals);
        Assert.Equal(typeof(object), equals!.DeclaringType);
    }
}
