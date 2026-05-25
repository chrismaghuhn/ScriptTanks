using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationPipelineTests
{
    [Fact]
    public void Evaluate_rejects_negative_tankIndex_with_ParamName_tankIndex()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                Command(ScriptCommandType.Fire)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: -1,
                program,
                CreateContext()));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Evaluate_rejects_null_program_with_ParamName_program()
    {
        ScriptProgram? program = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program!,
                CreateContext()));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void Evaluate_preserves_tick()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                fire));

        ScriptRuntimeEvaluationRecord record =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                CreateContext());

        Assert.Equal(new SimTick(42), record.Tick);
    }

    [Fact]
    public void Evaluate_preserves_tankIndex()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                fire));

        ScriptRuntimeEvaluationRecord record =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                CreateContext());

        Assert.Equal(1, record.TankIndex);
    }

    [Fact]
    public void Evaluate_preserves_program_reference()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                fire));

        ScriptRuntimeEvaluationRecord record =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                CreateContext());

        Assert.Same(program, record.Program);
    }

    [Fact]
    public void Evaluate_preserves_context_value()
    {
        ScriptEvaluationContext context = CreateContext();

        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                Command(ScriptCommandType.Fire)));

        ScriptRuntimeEvaluationRecord record =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                context);

        Assert.Equal(context, record.Context);
    }

    [Fact]
    public void Evaluate_returns_result_with_translated_command_when_routine_matches()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                fire));

        ScriptRuntimeEvaluationRecord record =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                CreateContext());

        Assert.Equal(new SimTick(42), record.Tick);
        Assert.Equal(1, record.TankIndex);
        Assert.Same(program, record.Program);
        Assert.Equal(fire, record.Result.Translation.Command);
        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            record.Result.Translation.Status);
    }

    [Fact]
    public void Evaluate_returns_NoIntent_when_no_routine_matches()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "enemy",
                EnemyVisible(),
                Command(ScriptCommandType.Fire)));

        ScriptRuntimeEvaluationRecord record =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                CreateContext(enemyVisible: false));

        Assert.False(record.Result.Decision.HasCommand);
        Assert.False(record.Result.Intent.HasIntent);
        Assert.Equal(
            ScriptCommandTranslationStatus.NoIntent,
            record.Result.Translation.Status);
    }

    [Fact]
    public void Evaluate_propagates_invalid_condition_argument_with_ParamName_condition()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "invalid",
                MyHpBelow("abc"),
                Command(ScriptCommandType.Fire)));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                tankIndex: 1,
                program,
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void Evaluate_does_not_mutate_program()
    {
        ScriptRoutine first = CreateRoutine(
            "first",
            Always(),
            Command(ScriptCommandType.Fire));

        ScriptRoutine second = CreateRoutine(
            "second",
            EnemyVisible(),
            Command(ScriptCommandType.ScanEnemy));

        ScriptProgram program = CreateProgram(first, second);

        _ = ScriptRuntimeEvaluationPipeline.Evaluate(
            new SimTick(42),
            tankIndex: 1,
            program,
            CreateContext());

        Assert.Equal(2, program.Count);
        Assert.Equal(first, program.GetRoutineAtIndex(0));
        Assert.Equal(second, program.GetRoutineAtIndex(1));
    }

    private static ScriptEvaluationContext CreateContext(
        bool enemyVisible = true,
        bool weaponReady = true,
        int myHitPoints = 50,
        Fixed? enemyDistance = null,
        bool sensorReady = true)
    {
        return new ScriptEvaluationContext(
            enemyVisible,
            weaponReady,
            myHitPoints,
            enemyDistance ?? Fixed.FromInt(10),
            sensorReady,
            ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptCommand Command(
        ScriptCommandType type,
        string argument = "")
    {
        return new ScriptCommand(type, argument);
    }

    private static ScriptCondition Always()
    {
        return ScriptCondition.Always();
    }

    private static ScriptCondition EnemyVisible()
    {
        return new ScriptCondition(
            ScriptConditionType.EnemyVisible,
            string.Empty);
    }

    private static ScriptCondition MyHpBelow(string threshold)
    {
        return new ScriptCondition(
            ScriptConditionType.MyHpBelow,
            threshold);
    }

    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptCondition condition,
        ScriptCommand command)
    {
        return new ScriptRoutine(
            name,
            condition,
            command);
    }

    private static ScriptProgram CreateProgram(
        params ScriptRoutine[] routines)
    {
        return new ScriptProgram(routines);
    }
}
