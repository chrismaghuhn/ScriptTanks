using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptDecisionPipelineTests
{
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

    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptCondition condition,
        ScriptCommand? command = null)
    {
        return new ScriptRoutine(
            name,
            condition,
            command ?? ScriptCommand.NoOp());
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

    private static ScriptCondition WeaponReady()
    {
        return new ScriptCondition(
            ScriptConditionType.WeaponReady,
            string.Empty);
    }

    private static ScriptCondition MyHpBelow(string threshold)
    {
        return new ScriptCondition(
            ScriptConditionType.MyHpBelow,
            threshold);
    }

    private static ScriptCommand Fire()
    {
        return new ScriptCommand(
            ScriptCommandType.Fire,
            string.Empty);
    }

    private static ScriptProgram CreateProgram(params ScriptRoutine[] routines)
    {
        return new ScriptProgram(routines);
    }

    [Fact]
    public void Decide_RejectsNullProgram_WithParamNameProgram()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => ScriptDecisionPipeline.Decide(
                null!,
                CreateContext()));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void Decide_ReturnsNoCommand_WhenNoRoutineMatches()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine("enemy", EnemyVisible()),
            CreateRoutine("weapon", WeaponReady()));

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(program, context);

        Assert.False(decision.HasCommand);
        Assert.Equal(-1, decision.RoutineIndex);
        Assert.Null(decision.Routine);
        Assert.Null(decision.Command);
    }

    [Fact]
    public void Decide_ReturnsCommandDecision_WhenRoutineMatches()
    {
        ScriptCommand command = Fire();
        ScriptRoutine routine = CreateRoutine(
            "engage",
            Always(),
            command);

        ScriptProgram program = CreateProgram(routine);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(program, CreateContext());

        Assert.True(decision.HasCommand);
        Assert.Equal(0, decision.RoutineIndex);
        Assert.Equal(routine, decision.Routine);
        Assert.Equal(command, decision.Command);
    }

    [Fact]
    public void Decide_PreservesSelectedRoutineIndex()
    {
        ScriptRoutine third = CreateRoutine("third", Always());

        ScriptProgram program = CreateProgram(
            CreateRoutine("first", EnemyVisible()),
            CreateRoutine("second", WeaponReady()),
            third);

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(program, context);

        Assert.True(decision.HasCommand);
        Assert.Equal(2, decision.RoutineIndex);
    }

    [Fact]
    public void Decide_PreservesSelectedRoutineValue()
    {
        ScriptRoutine expected = CreateRoutine("engage", Always(), Fire());

        ScriptProgram program = CreateProgram(expected);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(program, CreateContext());

        Assert.Equal(expected, decision.Routine);
    }

    [Fact]
    public void Decide_ExposesSelectedRoutineCommand()
    {
        ScriptCommand command = Fire();
        ScriptRoutine routine = CreateRoutine("engage", Always(), command);

        ScriptProgram program = CreateProgram(routine);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(program, CreateContext());

        Assert.Equal(routine.Command, decision.Command);
    }

    [Fact]
    public void Decide_FirstMatchingRoutineWins()
    {
        ScriptRoutine first = CreateRoutine(
            "first",
            Always(),
            new ScriptCommand(ScriptCommandType.ScanEnemy, string.Empty));

        ScriptRoutine second = CreateRoutine(
            "second",
            Always(),
            new ScriptCommand(ScriptCommandType.Fire, string.Empty));

        ScriptProgram program = CreateProgram(first, second);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(program, CreateContext());

        Assert.True(decision.HasCommand);
        Assert.Equal(0, decision.RoutineIndex);
        Assert.Equal(first, decision.Routine);
        Assert.Equal(first.Command, decision.Command);
    }

    [Fact]
    public void Decide_PropagatesInvalidConditionArgument_WithParamNameCondition()
    {
        ScriptRoutine invalid = CreateRoutine(
            "invalid",
            MyHpBelow("abc"));

        ScriptProgram program = CreateProgram(invalid);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptDecisionPipeline.Decide(
                program,
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void Decide_DoesNotMutateProgram()
    {
        ScriptRoutine first = CreateRoutine("first", Always());
        ScriptRoutine second = CreateRoutine("second", EnemyVisible());

        ScriptProgram program = CreateProgram(first, second);

        _ = ScriptDecisionPipeline.Decide(program, CreateContext());

        Assert.Equal(2, program.Count);
        Assert.Equal(first, program.GetRoutineAtIndex(0));
        Assert.Equal(second, program.GetRoutineAtIndex(1));
    }
}
