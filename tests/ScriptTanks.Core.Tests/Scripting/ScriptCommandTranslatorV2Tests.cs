using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTranslatorV2Tests
{
    [Fact]
    public void Translate_rejects_null_intent_with_ParamName_intent()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptCommandTranslatorV2.Translate(null!));

        Assert.Equal("intent", ex.ParamName);
    }

    [Fact]
    public void Translate_no_intent_returns_NoIntent_result()
    {
        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(
                ScriptCommandIntent.None());

        Assert.Equal(
            ScriptCommandTranslationStatus.NoIntent,
            output.Result.Status);

        Assert.False(output.Result.HasTranslatedRequest);
    }

    [Fact]
    public void Translate_no_intent_returns_None_request()
    {
        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(
                ScriptCommandIntent.None());

        Assert.False(output.Request.HasRequest);
        Assert.Equal(
            ScriptTranslatedCommandRequestKind.None,
            output.Request.Kind);

        Assert.Null(output.Request.Command);
        Assert.Empty(output.Request.Payload);
    }

    [Fact]
    public void NoOp_maps_to_Translated_and_kind_NoOp_with_empty_payload()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.NoOp);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            output.Result.Status);

        Assert.Equal(
            ScriptTranslatedCommandRequestKind.NoOp,
            output.Request.Kind);

        Assert.Empty(output.Request.Payload);
    }

    [Fact]
    public void ScanEnemy_maps_to_kind_ScanEnemy_and_payload_default()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.ScanEnemy);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(
            ScriptTranslatedCommandRequestKind.ScanEnemy,
            output.Request.Kind);

        Assert.Equal("default", output.Request.Payload);
    }

    [Fact]
    public void AimAtEnemy_maps_to_kind_AimAtEnemy_and_payload_nearest_visible()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.AimAtEnemy);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(
            ScriptTranslatedCommandRequestKind.AimAtEnemy,
            output.Request.Kind);

        Assert.Equal("nearest_visible", output.Request.Payload);
    }

    [Fact]
    public void Fire_maps_to_kind_Fire_and_payload_default_with_routine_and_command()
    {
        ScriptCommandIntent intent = CreateIntent(
            ScriptCommandType.Fire,
            routineIndex: 3,
            argument: "primary");

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            output.Result.Status);

        Assert.Equal(
            ScriptTranslatedCommandRequestKind.Fire,
            output.Request.Kind);

        Assert.Equal(3, output.Result.RoutineIndex);
        Assert.Equal(3, output.Request.RoutineIndex);

        Assert.Equal(intent.Command, output.Result.Command);
        Assert.Equal(intent.Command, output.Request.Command);

        Assert.Equal("default", output.Request.Payload);

        Assert.True(output.Result.HasTranslatedRequest);
        Assert.True(output.Request.HasRequest);
    }

    [Fact]
    public void MoveToPatrolPoint_with_empty_argument_maps_payload_next()
    {
        ScriptCommandIntent intent = CreateIntent(
            ScriptCommandType.MoveToPatrolPoint,
            argument: string.Empty);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal("next", output.Request.Payload);
    }

    [Fact]
    public void MoveToPatrolPoint_with_explicit_argument_preserves_payload_verbatim()
    {
        ScriptCommandIntent intent = CreateIntent(
            ScriptCommandType.MoveToPatrolPoint,
            argument: "alpha");

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal("alpha", output.Request.Payload);
    }

    [Fact]
    public void MoveToPatrolPoint_with_whitespace_argument_preserves_whitespace_verbatim()
    {
        ScriptCommandIntent intent = CreateIntent(
            ScriptCommandType.MoveToPatrolPoint,
            argument: "   ");

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal("   ", output.Request.Payload);
    }

    [Fact]
    public void Retreat_maps_to_kind_Retreat_and_payload_away_from_nearest_visible()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.Retreat);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(
            ScriptTranslatedCommandRequestKind.Retreat,
            output.Request.Kind);

        Assert.Equal("away_from_nearest_visible", output.Request.Payload);
    }

    [Fact]
    public void Translated_output_preserves_routine_index_in_result_and_request()
    {
        ScriptCommandIntent intent = CreateIntent(
            ScriptCommandType.ScanEnemy,
            routineIndex: 7);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(7, output.Result.RoutineIndex);
        Assert.Equal(7, output.Request.RoutineIndex);
    }

    [Fact]
    public void Translated_output_preserves_command_in_result_and_request()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.Fire);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.Equal(intent.Command, output.Result.Command);
        Assert.Equal(intent.Command, output.Request.Command);
    }

    [Fact]
    public void Translated_output_HasTranslatedRequest_and_HasRequest_true()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.NoOp);

        ScriptCommandTranslationOutput output =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.True(output.Result.HasTranslatedRequest);
        Assert.True(output.Request.HasRequest);
    }

    [Fact]
    public void Repeated_Translate_calls_produce_equivalent_values_but_different_instances()
    {
        ScriptCommandIntent intent = CreateIntent(ScriptCommandType.Fire);

        ScriptCommandTranslationOutput first =
            ScriptCommandTranslatorV2.Translate(intent);

        ScriptCommandTranslationOutput second =
            ScriptCommandTranslatorV2.Translate(intent);

        Assert.False(ReferenceEquals(first, second));
        Assert.Equal(first.Result.Status, second.Result.Status);
        Assert.Equal(first.Request.Kind, second.Request.Kind);
        Assert.Equal(first.Request.Payload, second.Request.Payload);
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
