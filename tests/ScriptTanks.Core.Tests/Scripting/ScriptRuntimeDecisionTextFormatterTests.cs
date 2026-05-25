using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionTextFormatterTests
{
    [Fact]
    public void Format_rejects_null_result_with_ParamName_result()
    {
        ScriptRuntimeDecisionResult? result = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeDecisionTextFormatter.Format(result!));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Format_no_command_result_includes_Decision_HasCommand_False()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateNoCommandResult());

        Assert.Contains("ScriptRuntimeDecisionResult", text, StringComparison.Ordinal);
        Assert.Contains("Decision.HasCommand = False", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_no_command_result_includes_Intent_HasIntent_False()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateNoCommandResult());

        Assert.Contains("Intent.HasIntent = False", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_no_command_result_includes_Translation_Status_NoIntent()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateNoCommandResult());

        Assert.Contains("Translation.Status = NoIntent", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_no_command_result_includes_Translation_Command_None()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateNoCommandResult());

        Assert.Contains("Translation.Command = None", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_translated_result_includes_Decision_HasCommand_True()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateTranslatedResult());

        Assert.Contains("Decision.HasCommand = True", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_translated_result_includes_command_type_routine_index_and_Translated_status()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateTranslatedResult());

        Assert.Contains("Decision.RoutineIndex = 2", text, StringComparison.Ordinal);
        Assert.Contains("Intent.RoutineIndex = 2", text, StringComparison.Ordinal);
        Assert.Contains("Translation.RoutineIndex = 2", text, StringComparison.Ordinal);
        Assert.Contains("Translation.Status = Translated", text, StringComparison.Ordinal);
        Assert.Contains("Translation.Command = Fire", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_translated_result_includes_translation_message()
    {
        string text = ScriptRuntimeDecisionTextFormatter.Format(
            CreateTranslatedResult());

        Assert.Contains("Fire command recognized.", text, StringComparison.Ordinal);
    }

    private static ScriptRuntimeDecisionResult CreateNoCommandResult()
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

    private static ScriptRuntimeDecisionResult CreateTranslatedResult()
    {
        ScriptCommand command = new ScriptCommand(
            ScriptCommandType.Fire,
            "primary");

        ScriptRoutine routine = new ScriptRoutine(
            "fire",
            ScriptCondition.Always(),
            command);

        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelectionResult.Selected(
                routineIndex: 2,
                routine);

        ScriptRoutineDecision decision =
            ScriptRoutineDecision.FromSelection(selection);

        ScriptCommandIntent intent =
            ScriptCommandIntent.FromDecision(decision);

        ScriptCommandTranslationResult translation =
            ScriptCommandTranslator.Translate(intent);

        return new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);
    }
}
