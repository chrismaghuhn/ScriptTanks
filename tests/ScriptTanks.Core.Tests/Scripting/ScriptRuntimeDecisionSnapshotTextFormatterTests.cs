using System;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionSnapshotTextFormatterTests
{
    [Fact]
    public void Format_rejects_null_snapshot_with_ParamName_snapshot()
    {
        ScriptRuntimeDecisionSnapshot? snapshot = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeDecisionSnapshotTextFormatter.Format(snapshot!));

        Assert.Equal("snapshot", ex.ParamName);
    }

    [Fact]
    public void Formatted_text_includes_ScriptRuntimeDecisionSnapshot()
    {
        string text = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateSnapshot());

        Assert.Contains("ScriptRuntimeDecisionSnapshot", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Tick_42()
    {
        string text = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateSnapshot());

        Assert.Contains("Tick = 42", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_TankIndex_1()
    {
        string text = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateSnapshot());

        Assert.Contains("TankIndex = 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_ScriptRuntimeDecisionResult()
    {
        string text = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateSnapshot());

        Assert.Contains("ScriptRuntimeDecisionResult", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_translation_status()
    {
        string text = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateSnapshot());

        Assert.Contains("Translation.Status = Translated", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_command_type_or_None()
    {
        string translated = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateSnapshot());

        Assert.Contains("Translation.Command = Fire", translated, StringComparison.Ordinal);

        string noCommand = ScriptRuntimeDecisionSnapshotTextFormatter.Format(
            CreateNoCommandSnapshot());

        Assert.Contains("Translation.Command = None", noCommand, StringComparison.Ordinal);
    }

    private static ScriptRuntimeDecisionResult CreateResult()
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

    private static ScriptRuntimeDecisionSnapshot CreateSnapshot()
    {
        return new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            tankIndex: 1,
            CreateResult());
    }

    private static ScriptRuntimeDecisionSnapshot CreateNoCommandSnapshot()
    {
        ScriptRuntimeDecisionResult result = CreateNoCommandResult();

        return new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            tankIndex: 1,
            result);
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
}
