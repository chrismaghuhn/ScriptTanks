using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationRecordTests
{
    [Fact]
    public void Constructor_rejects_negative_tankIndex_with_ParamName_tankIndex()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptRuntimeEvaluationRecord(
                new SimTick(42),
                tankIndex: -1,
                program,
                context,
                result));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_program_with_ParamName_program()
    {
        ScriptProgram? program = null;
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeEvaluationRecord(
                new SimTick(42),
                tankIndex: 1,
                program!,
                context,
                result));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_result_with_ParamName_result()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult? result = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeEvaluationRecord(
                new SimTick(42),
                tankIndex: 1,
                program,
                context,
                result!));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_tick()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var record = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            tankIndex: 1,
            program,
            context,
            result);

        Assert.Equal(new SimTick(42), record.Tick);
    }

    [Fact]
    public void Constructor_preserves_tankIndex()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var record = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            tankIndex: 1,
            program,
            context,
            result);

        Assert.Equal(1, record.TankIndex);
    }

    [Fact]
    public void Constructor_preserves_program_reference()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var record = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            tankIndex: 1,
            program,
            context,
            result);

        Assert.Same(program, record.Program);
    }

    [Fact]
    public void Constructor_preserves_context_value()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var record = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            tankIndex: 1,
            program,
            context,
            result);

        Assert.Equal(context, record.Context);
    }

    [Fact]
    public void Constructor_preserves_result_reference()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var record = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            tankIndex: 1,
            program,
            context,
            result);

        Assert.Same(result, record.Result);
    }

    [Fact]
    public void Constructor_allows_tankIndex_zero()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var record = new ScriptRuntimeEvaluationRecord(
            new SimTick(0),
            tankIndex: 0,
            program,
            context,
            result);

        Assert.Equal(0, record.TankIndex);
    }

    [Fact]
    public void Two_records_with_same_values_are_not_reference_equal()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();
        ScriptRuntimeDecisionResult result = CreateResult();

        var first = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            1,
            program,
            context,
            result);

        var second = new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            1,
            program,
            context,
            result);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }

    private static ScriptProgram CreateProgram()
    {
        ScriptRoutine routine = new ScriptRoutine(
            "fire",
            ScriptCondition.Always(),
            new ScriptCommand(
                ScriptCommandType.Fire,
                string.Empty));

        return new ScriptProgram(new[] { routine });
    }

    private static ScriptEvaluationContext CreateContext()
    {
        return new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptRuntimeDecisionResult CreateResult()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();
        ScriptCommandIntent intent = ScriptCommandIntent.FromDecision(decision);
        ScriptCommandTranslationResult translation =
            ScriptCommandTranslator.Translate(intent);

        return new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);
    }
}
