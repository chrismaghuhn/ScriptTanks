using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRoutineDecisionTests
{
    private static ScriptRoutine CreateRoutine(
        string name = "engage",
        ScriptCommandType commandType = ScriptCommandType.Fire)
    {
        return new ScriptRoutine(
            name,
            new ScriptCondition(
                ScriptConditionType.EnemyVisible,
                string.Empty),
            new ScriptCommand(
                commandType,
                string.Empty));
    }

    [Fact]
    public void None_ReturnsHasCommandFalse()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();

        Assert.False(decision.HasCommand);
    }

    [Fact]
    public void None_ReturnsRoutineIndexNegativeOne()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();

        Assert.Equal(-1, decision.RoutineIndex);
    }

    [Fact]
    public void None_ReturnsNullRoutine()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();

        Assert.Null(decision.Routine);
    }

    [Fact]
    public void None_ReturnsNullCommand()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();

        Assert.Null(decision.Command);
    }

    [Fact]
    public void FromSelection_RejectsNullSelection_WithParamNameSelection()
    {
        ScriptRoutineSelectionResult? selection = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => ScriptRoutineDecision.FromSelection(selection!));

        Assert.Equal("selection", ex.ParamName);
    }

    [Fact]
    public void FromSelection_ReturnsNoneSemantics_WhenSelectionHasNoRoutine()
    {
        ScriptRoutineSelectionResult selection = ScriptRoutineSelectionResult.None();

        ScriptRoutineDecision decision = ScriptRoutineDecision.FromSelection(selection);

        Assert.False(decision.HasCommand);
        Assert.Equal(-1, decision.RoutineIndex);
        Assert.Null(decision.Routine);
        Assert.Null(decision.Command);
    }

    [Fact]
    public void FromSelection_ReturnsHasCommandTrue_ForSelectedRoutine()
    {
        ScriptRoutine routine = CreateRoutine();
        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 2,
                routine);

        ScriptRoutineDecision decision = ScriptRoutineDecision.FromSelection(selection);

        Assert.True(decision.HasCommand);
    }

    [Fact]
    public void FromSelection_PreservesRoutineIndex()
    {
        ScriptRoutine routine = CreateRoutine();
        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(2, routine);

        ScriptRoutineDecision decision = ScriptRoutineDecision.FromSelection(selection);

        Assert.Equal(2, decision.RoutineIndex);
    }

    [Fact]
    public void FromSelection_PreservesRoutineValue()
    {
        ScriptRoutine routine = CreateRoutine();
        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(0, routine);

        ScriptRoutineDecision decision = ScriptRoutineDecision.FromSelection(selection);

        Assert.Equal(routine, decision.Routine);
    }

    [Fact]
    public void FromSelection_ExposesSelectedRoutineCommand()
    {
        ScriptRoutine routine = CreateRoutine();
        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(0, routine);

        ScriptRoutineDecision decision = ScriptRoutineDecision.FromSelection(selection);

        Assert.Equal(routine.Command, decision.Command);
    }

    [Fact]
    public void TwoDecisionsWithSameValues_UseReferenceEqualityNotValueEquality()
    {
        ScriptRoutine routine = CreateRoutine();
        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(1, routine);

        ScriptRoutineDecision first = ScriptRoutineDecision.FromSelection(selection);
        ScriptRoutineDecision second = ScriptRoutineDecision.FromSelection(selection);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void SameDecisionReference_EqualsItself()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.FromSelection(
            ScriptRoutineSelectionResult.Selected(0, CreateRoutine()));

        Assert.True(decision.Equals(decision));
    }
}
