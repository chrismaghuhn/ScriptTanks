using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRoutineSelectionResultTests
{
    private static ScriptRoutine CreateRoutine(
        string name = "engage")
    {
        return new ScriptRoutine(
            name,
            new ScriptCondition(
                ScriptConditionType.EnemyVisible,
                string.Empty),
            new ScriptCommand(
                ScriptCommandType.Fire,
                string.Empty));
    }

    [Fact]
    public void None_ReturnsHasSelectionFalse()
    {
        ScriptRoutineSelectionResult result = ScriptRoutineSelectionResult.None();

        Assert.False(result.HasSelection);
    }

    [Fact]
    public void None_ReturnsRoutineIndexNegativeOne()
    {
        ScriptRoutineSelectionResult result = ScriptRoutineSelectionResult.None();

        Assert.Equal(-1, result.RoutineIndex);
    }

    [Fact]
    public void None_ReturnsNullRoutine()
    {
        ScriptRoutineSelectionResult result = ScriptRoutineSelectionResult.None();

        Assert.Null(result.Routine);
    }

    [Fact]
    public void Selected_RejectsNegativeRoutineIndex_WithParamNameRoutineIndex()
    {
        ScriptRoutine routine = CreateRoutine();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => ScriptRoutineSelectionResult.Selected(
                routineIndex: -1,
                routine));

        Assert.Equal("routineIndex", ex.ParamName);
    }

    [Fact]
    public void Selected_ReturnsHasSelectionTrue()
    {
        ScriptRoutine routine = CreateRoutine();

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 2,
                routine);

        Assert.True(result.HasSelection);
    }

    [Fact]
    public void Selected_PreservesRoutineIndex()
    {
        ScriptRoutine routine = CreateRoutine();

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 2,
                routine);

        Assert.Equal(2, result.RoutineIndex);
    }

    [Fact]
    public void Selected_PreservesRoutineValue()
    {
        ScriptRoutine routine = CreateRoutine();

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 2,
                routine);

        Assert.Equal(routine, result.Routine);
    }

    [Fact]
    public void Selected_AllowsRoutineIndexZero()
    {
        ScriptRoutine routine = CreateRoutine();

        ScriptRoutineSelectionResult result =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 0,
                routine);

        Assert.Equal(0, result.RoutineIndex);
        Assert.True(result.HasSelection);
    }

    [Fact]
    public void TwoResultsWithSameValues_UseReferenceEqualityNotValueEquality()
    {
        ScriptRoutine routine = CreateRoutine();

        ScriptRoutineSelectionResult first =
            ScriptRoutineSelectionResult.Selected(1, routine);

        ScriptRoutineSelectionResult second =
            ScriptRoutineSelectionResult.Selected(1, routine);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void SameResultReference_EqualsItself()
    {
        ScriptRoutineSelectionResult result =
            ScriptRoutineSelectionResult.Selected(1, CreateRoutine());

        Assert.True(result.Equals(result));
    }
}
