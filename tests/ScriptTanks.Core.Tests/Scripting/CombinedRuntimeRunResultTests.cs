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

public sealed class CombinedRuntimeRunResultTests
{
    private sealed class ValidRunShape
    {
        public MatchSensorRuntimeState FinalRuntime { get; init; } = null!;

        public MatchRunResult RunResult { get; init; } = null!;

        public ProjectileIdSequence FinalSequence { get; init; }

        public CombinedRuntimeTickResult? LastTickResult { get; init; }
    }

    private sealed class ValidTickShape
    {
        public MatchSensorRuntimeState Runtime { get; init; } = null!;

        public CombinedScriptRuntimeComposerResult ScriptResult { get; init; } = null!;

        public MatchStateTankMovementResult TankMovementResult { get; init; } = null!;

        public MatchStateTankBoundsResult TankBoundsResult { get; init; } = null!;

        public MatchState SteppedState { get; init; } = null!;

        public MatchSensorRuntimeState FinalRuntime { get; init; } = null!;

        public ProjectileIdSequence FinalSequence { get; init; }

        public CombinedRuntimeTickResult LastTickResult { get; init; } = null!;
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

    private static ValidTickShape BuildValidTickShape(
        MatchSensorRuntimeState runtime,
        ProjectileIdSequence sequence,
        params ScriptProgram[] programs)
    {
        CombinedScriptRuntimeComposerResult scriptResult =
            CombinedScriptRuntimeComposer.Run(runtime, programs, sequence);

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

        MatchSensorRuntimeState finalRuntime =
            scriptResult.FinalRuntime.WithState(steppedState);

        CombinedRuntimeTickResult tickResult = new CombinedRuntimeTickResult(
            runtime,
            scriptResult,
            tankMovement,
            tankBounds,
            tankObstacleCollision,
            steppedState,
            finalRuntime,
            scriptResult.FinalProjectileIdSequence);

        return new ValidTickShape
        {
            Runtime = runtime,
            ScriptResult = scriptResult,
            TankMovementResult = tankMovement,
            TankBoundsResult = tankBounds,
            SteppedState = steppedState,
            FinalRuntime = finalRuntime,
            FinalSequence = scriptResult.FinalProjectileIdSequence,
            LastTickResult = tickResult,
        };
    }

    private static ValidRunShape CreateZeroTickShape()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ProjectileIdSequence sequence = new ProjectileIdSequence(42);

        return new ValidRunShape
        {
            FinalRuntime = runtime,
            RunResult = new MatchRunResult(
                runtime.State,
                MatchEndConditionResult.TimeoutDraw(),
                ticksExecuted: 0),
            FinalSequence = sequence,
            LastTickResult = null,
        };
    }

    private static ValidRunShape CreatePostTickShape()
    {
        ValidTickShape tickShape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        return new ValidRunShape
        {
            FinalRuntime = tickShape.FinalRuntime,
            RunResult = new MatchRunResult(
                tickShape.FinalRuntime.State,
                MatchEndConditionResult.TimeoutDraw(),
                ticksExecuted: 1),
            FinalSequence = tickShape.FinalSequence,
            LastTickResult = tickShape.LastTickResult,
        };
    }

    private static CombinedRuntimeRunResult CreateResult(ValidRunShape shape)
    {
        return new CombinedRuntimeRunResult(
            shape.FinalRuntime,
            shape.RunResult,
            shape.FinalSequence,
            shape.LastTickResult);
    }

    [Fact]
    public void Constructor_preserves_all_values()
    {
        ValidRunShape shape = CreatePostTickShape();

        CombinedRuntimeRunResult result = CreateResult(shape);

        Assert.Same(shape.FinalRuntime, result.FinalRuntime);
        Assert.Same(shape.RunResult, result.RunResult);
        Assert.Same(shape.LastTickResult, result.LastTickResult);
        Assert.Equal(shape.FinalSequence, result.FinalProjectileIdSequence);
        Assert.Equal(
            shape.FinalSequence.NextValue,
            result.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void HasLastTickResult_false_when_last_tick_null()
    {
        ValidRunShape shape = CreateZeroTickShape();

        CombinedRuntimeRunResult result = CreateResult(shape);

        Assert.False(result.HasLastTickResult);
        Assert.Null(result.LastTickResult);
    }

    [Fact]
    public void HasLastTickResult_true_when_last_tick_present()
    {
        ValidRunShape shape = CreatePostTickShape();

        CombinedRuntimeRunResult result = CreateResult(shape);

        Assert.True(result.HasLastTickResult);
        Assert.NotNull(result.LastTickResult);
    }

    [Fact]
    public void Constructor_NullFinalRuntime_Throws_ParamName_finalRuntime()
    {
        ValidRunShape shape = CreatePostTickShape();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeRunResult(
                null!,
                shape.RunResult,
                shape.FinalSequence,
                shape.LastTickResult));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullRunResult_Throws_ParamName_runResult()
    {
        ValidRunShape shape = CreatePostTickShape();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedRuntimeRunResult(
                shape.FinalRuntime,
                null!,
                shape.FinalSequence,
                shape.LastTickResult));

        Assert.Equal("runResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalRuntimeStateMismatch_Throws_ParamName_finalRuntime()
    {
        ValidRunShape shape = CreatePostTickShape();
        MatchState otherState = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));

        MatchRunResult mismatchedRunResult = new MatchRunResult(
            otherState,
            MatchEndConditionResult.TimeoutDraw(),
            ticksExecuted: 1);

        Assert.False(
            ReferenceEquals(shape.FinalRuntime.State, mismatchedRunResult.FinalState));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeRunResult(
                shape.FinalRuntime,
                mismatchedRunResult,
                shape.FinalSequence,
                shape.LastTickResult));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_LastTickFinalRuntimeMismatch_Throws_ParamName_finalRuntime()
    {
        ValidTickShape tickShape = BuildValidTickShape(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchSensorRuntimeState aliasedWrapper = new MatchSensorRuntimeState(
            tickShape.FinalRuntime.State,
            tickShape.FinalRuntime.SensorLoadouts);

        Assert.True(
            ReferenceEquals(
                aliasedWrapper.State,
                tickShape.LastTickResult!.FinalRuntime.State));
        Assert.False(
            ReferenceEquals(aliasedWrapper, tickShape.LastTickResult.FinalRuntime));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeRunResult(
                aliasedWrapper,
                new MatchRunResult(
                    tickShape.FinalRuntime.State,
                    MatchEndConditionResult.TimeoutDraw(),
                    ticksExecuted: 1),
                tickShape.FinalSequence,
                tickShape.LastTickResult));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_LastTickSequenceMismatch_Throws_ParamName_finalProjectileIdSequence()
    {
        ValidRunShape shape = CreatePostTickShape();
        ProjectileIdSequence wrongSequence = new ProjectileIdSequence(
            shape.FinalSequence.NextValue + 1);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedRuntimeRunResult(
                shape.FinalRuntime,
                shape.RunResult,
                wrongSequence,
                shape.LastTickResult));

        Assert.Equal("finalProjectileIdSequence", ex.ParamName);
    }

    [Fact]
    public void Constructor_Accepts_zero_tick_shape_with_null_last_tick()
    {
        ValidRunShape shape = CreateZeroTickShape();

        CombinedRuntimeRunResult result = CreateResult(shape);

        Assert.Same(shape.FinalRuntime.State, result.RunResult.FinalState);
        Assert.Same(shape.FinalRuntime, result.FinalRuntime);
        Assert.Equal(42, result.FinalProjectileIdSequence.NextValue);
        Assert.Null(result.LastTickResult);
    }

    [Fact]
    public void Constructor_Accepts_post_tick_shape_with_last_tick()
    {
        ValidRunShape shape = CreatePostTickShape();

        CombinedRuntimeRunResult result = CreateResult(shape);

        Assert.Same(shape.FinalRuntime, result.FinalRuntime);
        Assert.Same(shape.LastTickResult, result.LastTickResult);
        Assert.Same(shape.FinalRuntime.State, result.RunResult.FinalState);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equals = typeof(CombinedRuntimeRunResult).GetMethod(
            "Equals",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equals);
        Assert.Equal(typeof(object), equals!.DeclaringType);
    }
}
