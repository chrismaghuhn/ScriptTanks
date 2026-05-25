using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRoutineSelectorTests
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

    private static ScriptCondition EnemyDistanceBelow(string threshold)
    {
        return new ScriptCondition(
            ScriptConditionType.EnemyDistanceBelow,
            threshold);
    }

    private static ScriptProgram CreateProgram(params ScriptRoutine[] routines)
    {
        return new ScriptProgram(routines);
    }

    [Fact]
    public void SelectFirstValid_RejectsNullProgram_WithParamNameProgram()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => ScriptRoutineSelector.SelectFirstValid(
                null!,
                CreateContext()));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void SelectFirstValid_ReturnsNone_WhenNoRoutineConditionMatches()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine("enemy", EnemyVisible()),
            CreateRoutine("weapon", WeaponReady()));

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(program, context);

        Assert.False(result.HasSelection);
        Assert.Equal(-1, result.RoutineIndex);
        Assert.Null(result.Routine);
    }

    [Fact]
    public void SelectFirstValid_ReturnsSelected_WhenFirstRoutineMatches()
    {
        ScriptRoutine first = CreateRoutine("first", Always());

        ScriptProgram program = CreateProgram(
            first,
            CreateRoutine("second", EnemyVisible()));

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(program, CreateContext(enemyVisible: false));

        Assert.True(result.HasSelection);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(first, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_ReturnsSelectedRoutineIndex_ForLaterMatchingRoutine()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine("a", EnemyVisible()),
            CreateRoutine("b", WeaponReady()),
            CreateRoutine("c", Always()));

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false,
            sensorReady: true);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(program, context);

        Assert.True(result.HasSelection);
        Assert.Equal(2, result.RoutineIndex);
    }

    [Fact]
    public void SelectFirstValid_FirstMatchingRoutineWins_WhenMultipleRoutinesMatch()
    {
        ScriptRoutine first = CreateRoutine("first", Always());
        ScriptRoutine second = CreateRoutine("second", Always());

        ScriptProgram program = CreateProgram(first, second);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(program, CreateContext());

        Assert.True(result.HasSelection);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(first, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_DoesNotSelectLaterRoutine_WhenEarlierRoutineMatches()
    {
        ScriptRoutine first = CreateRoutine("first", Always());
        ScriptRoutine second = CreateRoutine("second", EnemyVisible());

        ScriptProgram program = CreateProgram(first, second);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(
                program,
                CreateContext(enemyVisible: true));

        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(first, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_EvaluatesRoutinesInOrder_SecondSelectedWhenFirstFails()
    {
        ScriptRoutine second = CreateRoutine("second", WeaponReady());

        ScriptProgram program = CreateProgram(
            CreateRoutine("first", EnemyVisible()),
            second);

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: true);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(program, context);

        Assert.True(result.HasSelection);
        Assert.Equal(1, result.RoutineIndex);
        Assert.Equal(second, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_StopsEvaluatingAfterFirstMatch_InvalidLaterConditionNotEvaluated()
    {
        ScriptRoutine first = CreateRoutine("first", Always());
        ScriptRoutine invalidLater = CreateRoutine(
            "invalid",
            MyHpBelow("abc"));

        ScriptProgram program = CreateProgram(first, invalidLater);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(program, CreateContext());

        Assert.True(result.HasSelection);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(first, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_PropagatesInvalidConditionArgument_WithParamNameCondition()
    {
        ScriptRoutine invalidFirst = CreateRoutine(
            "invalid",
            MyHpBelow("abc"));

        ScriptProgram program = CreateProgram(invalidFirst);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptRoutineSelector.SelectFirstValid(
                program,
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void SelectFirstValid_SupportsMyHpBelowThreshold()
    {
        ScriptRoutine r = CreateRoutine("low", MyHpBelow("60"));

        ScriptProgram program = CreateProgram(r);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(
                program,
                CreateContext(myHitPoints: 50));

        Assert.True(result.HasSelection);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(r, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_SupportsEnemyDistanceBelowThreshold()
    {
        ScriptRoutine r = CreateRoutine("close", EnemyDistanceBelow("20"));

        ScriptProgram program = CreateProgram(r);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(
                program,
                CreateContext(enemyDistance: Fixed.FromInt(5)));

        Assert.True(result.HasSelection);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(r, result.Routine);
    }

    [Fact]
    public void SelectFirstValid_DoesNotMutateProgram()
    {
        ScriptRoutine first = CreateRoutine("first", Always());
        ScriptRoutine second = CreateRoutine("second", EnemyVisible());

        ScriptProgram program = CreateProgram(first, second);

        _ = ScriptRoutineSelector.SelectFirstValid(program, CreateContext());

        Assert.Equal(2, program.Count);
        Assert.Equal(first, program.GetRoutineAtIndex(0));
        Assert.Equal(second, program.GetRoutineAtIndex(1));
    }

    [Fact]
    public void SelectFirstValid_ReturnsSelectedRoutineValue_WhenFirstMatches()
    {
        ScriptRoutine expected = CreateRoutine("engage", EnemyVisible());

        ScriptProgram program = CreateProgram(expected);

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelector.SelectFirstValid(
                program,
                CreateContext(enemyVisible: true));

        Assert.Equal(expected, result.Routine);
    }
}
