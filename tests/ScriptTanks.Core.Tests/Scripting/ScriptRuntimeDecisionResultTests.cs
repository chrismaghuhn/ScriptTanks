using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionResultTests
{
    [Fact]
    public void Constructor_rejects_null_decision_with_ParamName_decision()
    {
        ScriptCommandIntent intent = CreateIntent();
        ScriptCommandTranslationResult translation = CreateTranslation(intent);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeDecisionResult(
                null!,
                intent,
                translation));

        Assert.Equal("decision", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_intent_with_ParamName_intent()
    {
        ScriptRoutineDecision decision = CreateDecision();
        ScriptCommandTranslationResult translation = CreateTranslation();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeDecisionResult(
                decision,
                null!,
                translation));

        Assert.Equal("intent", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_translation_with_ParamName_translation()
    {
        ScriptRoutineDecision decision = CreateDecision();
        ScriptCommandIntent intent = CreateIntent(decision);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeDecisionResult(
                decision,
                intent,
                null!));

        Assert.Equal("translation", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_decision_reference()
    {
        ScriptRoutineDecision decision = CreateDecision();
        ScriptCommandIntent intent = CreateIntent(decision);
        ScriptCommandTranslationResult translation = CreateTranslation(intent);

        var result = new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);

        Assert.Same(decision, result.Decision);
    }

    [Fact]
    public void Constructor_preserves_intent_reference()
    {
        ScriptRoutineDecision decision = CreateDecision();
        ScriptCommandIntent intent = CreateIntent(decision);
        ScriptCommandTranslationResult translation = CreateTranslation(intent);

        var result = new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);

        Assert.Same(intent, result.Intent);
    }

    [Fact]
    public void Constructor_preserves_translation_reference()
    {
        ScriptRoutineDecision decision = CreateDecision();
        ScriptCommandIntent intent = CreateIntent(decision);
        ScriptCommandTranslationResult translation = CreateTranslation(intent);

        var result = new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);

        Assert.Same(translation, result.Translation);
    }

    [Fact]
    public void Two_results_with_same_references_are_not_reference_equal()
    {
        ScriptRoutineDecision decision = CreateDecision();
        ScriptCommandIntent intent = CreateIntent(decision);
        ScriptCommandTranslationResult translation = CreateTranslation(intent);

        var first = new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);

        var second = new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }

    private static ScriptRoutine CreateRoutine()
    {
        return new ScriptRoutine(
            "engage",
            ScriptCondition.Always(),
            new ScriptCommand(
                ScriptCommandType.Fire,
                string.Empty));
    }

    private static ScriptRoutineDecision CreateDecision()
    {
        ScriptRoutine routine = CreateRoutine();

        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 2,
                routine);

        return ScriptRoutineDecision.FromSelection(selection);
    }

    private static ScriptCommandIntent CreateIntent(
        ScriptRoutineDecision? decision = null)
    {
        return ScriptCommandIntent.FromDecision(
            decision ?? CreateDecision());
    }

    private static ScriptCommandTranslationResult CreateTranslation(
        ScriptCommandIntent? intent = null)
    {
        return ScriptCommandTranslator.Translate(
            intent ?? CreateIntent());
    }
}
