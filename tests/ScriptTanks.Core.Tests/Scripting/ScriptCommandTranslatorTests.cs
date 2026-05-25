using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTranslatorTests
{
    [Fact]
    public void Translate_rejects_null_intent_with_ParamName_intent()
    {
        ScriptCommandIntent? intent = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptCommandTranslator.Translate(intent!));

        Assert.Equal("intent", ex.ParamName);
    }

    [Fact]
    public void Translate_no_intent_maps_to_NoIntent_status()
    {
        ScriptCommandTranslationResult result =
            ScriptCommandTranslator.Translate(ScriptCommandIntent.None());

        Assert.Equal(ScriptCommandTranslationStatus.NoIntent, result.Status);
        Assert.Equal(-1, result.RoutineIndex);
        Assert.Null(result.Command);
        Assert.Empty(result.Message);
    }

    [Fact]
    public void Translate_no_intent_HasTranslatedRequest_false()
    {
        ScriptCommandTranslationResult result =
            ScriptCommandTranslator.Translate(ScriptCommandIntent.None());

        Assert.False(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_NoOp_maps_to_Translated()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.NoOp);

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_ScanEnemy_maps_to_Translated()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.ScanEnemy);

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_AimAtEnemy_maps_to_Translated()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.AimAtEnemy);

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_Fire_maps_to_Translated()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.Fire);

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_MoveToPatrolPoint_maps_to_Translated()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.MoveToPatrolPoint);

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_Retreat_maps_to_Translated()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.Retreat);

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_translated_result_preserves_routine_index_and_command()
    {
        ScriptCommandIntent intent = CreateIntent(
            ScriptCommandType.Fire,
            routineIndex: 3,
            argument: "primary");

        ScriptCommandTranslationResult result = ScriptCommandTranslator.Translate(intent);

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Status);
        Assert.True(result.HasTranslatedRequest);
        Assert.Equal(3, result.RoutineIndex);
        Assert.Equal(intent.Command, result.Command);
    }

    private static ScriptCommandIntent CreateIntent(
        ScriptCommandType commandType,
        int routineIndex = 2,
        string argument = "")
    {
        ScriptRoutine routine = new ScriptRoutine(
            "routine",
            ScriptCondition.Always(),
            new ScriptCommand(
                commandType,
                argument));

        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(
                routineIndex,
                routine);

        ScriptRoutineDecision decision =
            ScriptRoutineDecision.FromSelection(selection);

        return ScriptCommandIntent.FromDecision(decision);
    }
}
