using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationRequestTests
{
    [Fact]
    public void Constructor_rejects_negative_tankIndex_with_ParamName_tankIndex()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptRuntimeEvaluationRequest(
                tankIndex: -1,
                program,
                context));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_program_with_ParamName_program()
    {
        ScriptProgram? program = null;
        ScriptEvaluationContext context = CreateContext();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeEvaluationRequest(
                tankIndex: 1,
                program!,
                context));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_tankIndex()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        var request = new ScriptRuntimeEvaluationRequest(
            tankIndex: 1,
            program,
            context);

        Assert.Equal(1, request.TankIndex);
    }

    [Fact]
    public void Constructor_preserves_program_reference()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        var request = new ScriptRuntimeEvaluationRequest(
            tankIndex: 1,
            program,
            context);

        Assert.Same(program, request.Program);
    }

    [Fact]
    public void Constructor_preserves_context_value()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        var request = new ScriptRuntimeEvaluationRequest(
            tankIndex: 1,
            program,
            context);

        Assert.Equal(context, request.Context);
    }

    [Fact]
    public void Constructor_allows_tankIndex_zero()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        var request = new ScriptRuntimeEvaluationRequest(
            tankIndex: 0,
            program,
            context);

        Assert.Equal(0, request.TankIndex);
    }

    [Fact]
    public void Two_requests_with_same_values_are_not_reference_equal()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        var first = new ScriptRuntimeEvaluationRequest(
            1,
            program,
            context);

        var second = new ScriptRuntimeEvaluationRequest(
            1,
            program,
            context);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_request_reference_equals_itself()
    {
        ScriptProgram program = CreateProgram();
        ScriptEvaluationContext context = CreateContext();

        var first = new ScriptRuntimeEvaluationRequest(
            1,
            program,
            context);

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
}
