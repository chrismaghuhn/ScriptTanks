using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationRecordTextFormatterTests
{
    [Fact]
    public void Format_rejects_null_record_with_ParamName_record()
    {
        ScriptRuntimeEvaluationRecord? record = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeEvaluationRecordTextFormatter.Format(record!));

        Assert.Equal("record", ex.ParamName);
    }

    [Fact]
    public void Formatted_text_includes_ScriptRuntimeEvaluationRecord()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("ScriptRuntimeEvaluationRecord", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Tick_42()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("Tick = 42", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_TankIndex_1()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("TankIndex = 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Program_Count_1()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("Program.Count = 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Context_prefix()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("Context = ScriptEvaluationContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_ScriptRuntimeDecisionResult()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("ScriptRuntimeDecisionResult", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_translation_status_and_command_type()
    {
        string text =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                CreateRecord());

        Assert.Contains("Translation.Status = Translated", text, StringComparison.Ordinal);
        Assert.Contains("Translation.Command = Fire", text, StringComparison.Ordinal);
    }

    private static ScriptProgram CreateProgram()
    {
        ScriptRoutine routine = new ScriptRoutine(
            "fire",
            ScriptCondition.Always(),
            new ScriptCommand(
                ScriptCommandType.Fire,
                "primary"));

        return new ScriptProgram(new[] { routine });
    }

    private static ScriptEvaluationContext CreateContext()
    {
        return new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);
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
                routineIndex: 0,
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

    private static ScriptRuntimeEvaluationRecord CreateRecord()
    {
        return new ScriptRuntimeEvaluationRecord(
            new SimTick(42),
            tankIndex: 1,
            CreateProgram(),
            CreateContext(),
            CreateResult());
    }
}
