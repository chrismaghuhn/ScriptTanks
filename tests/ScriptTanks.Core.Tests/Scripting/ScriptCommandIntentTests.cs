using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandIntentTests
{
    [Fact]
    public void None_returns_HasIntent_false()
    {
        ScriptCommandIntent intent = ScriptCommandIntent.None();

        Assert.False(intent.HasIntent);
    }

    [Fact]
    public void None_returns_RoutineIndex_minus_one()
    {
        ScriptCommandIntent intent = ScriptCommandIntent.None();

        Assert.Equal(-1, intent.RoutineIndex);
    }

    [Fact]
    public void None_returns_null_Command()
    {
        ScriptCommandIntent intent = ScriptCommandIntent.None();

        Assert.Null(intent.Command);
    }

    [Fact]
    public void FromDecision_rejects_null_decision_with_ParamName_decision()
    {
        ScriptRoutineDecision? decision = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptCommandIntent.FromDecision(decision!));

        Assert.Equal("decision", ex.ParamName);
    }

    [Fact]
    public void FromDecision_returns_None_semantics_when_decision_has_no_command()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();

        ScriptCommandIntent intent = ScriptCommandIntent.FromDecision(decision);

        Assert.False(intent.HasIntent);
        Assert.Equal(-1, intent.RoutineIndex);
        Assert.Null(intent.Command);
    }

    [Fact]
    public void FromDecision_returns_HasIntent_true_for_command_decision()
    {
        ScriptRoutineDecision decision = CreateDecision();

        ScriptCommandIntent intent = ScriptCommandIntent.FromDecision(decision);

        Assert.True(intent.HasIntent);
    }

    [Fact]
    public void FromDecision_preserves_routine_index()
    {
        ScriptRoutineDecision decision = CreateDecision(
            routineIndex: 2,
            commandType: ScriptCommandType.Fire);

        ScriptCommandIntent intent = ScriptCommandIntent.FromDecision(decision);

        Assert.Equal(2, intent.RoutineIndex);
    }

    [Fact]
    public void FromDecision_exposes_selected_command()
    {
        ScriptRoutineDecision decision = CreateDecision(
            routineIndex: 2,
            commandType: ScriptCommandType.Fire);

        ScriptCommandIntent intent = ScriptCommandIntent.FromDecision(decision);

        Assert.Equal(decision.Command, intent.Command);
    }

    [Fact]
    public void Two_intents_from_same_decision_are_not_reference_equal()
    {
        ScriptRoutineDecision decision = CreateDecision();

        ScriptCommandIntent first = ScriptCommandIntent.FromDecision(decision);
        ScriptCommandIntent second = ScriptCommandIntent.FromDecision(decision);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_intent_reference_equals_itself()
    {
        ScriptRoutineDecision decision = CreateDecision();

        ScriptCommandIntent first = ScriptCommandIntent.FromDecision(decision);

        Assert.True(first.Equals(first));
    }

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

    private static ScriptRoutineDecision CreateDecision(
        int routineIndex = 2,
        ScriptCommandType commandType = ScriptCommandType.Fire)
    {
        ScriptRoutine routine = CreateRoutine(
            commandType: commandType);

        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(
                routineIndex,
                routine);

        return ScriptRoutineDecision.FromSelection(selection);
    }
}
