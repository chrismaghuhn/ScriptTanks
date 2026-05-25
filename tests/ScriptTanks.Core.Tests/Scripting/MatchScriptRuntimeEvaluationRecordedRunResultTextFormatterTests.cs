using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordedRunResultTextFormatterTests
{
    [Fact]
    public void Format_rejects_null_result_with_ParamName_result()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(null!));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Formatted_text_includes_MatchScriptRuntimeEvaluationRecordedRunResult()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains(
            "MatchScriptRuntimeEvaluationRecordedRunResult",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Summary_FrameCount_equals_1()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains("Summary.FrameCount = 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Summary_InitialTick_equals_42()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains("Summary.InitialTick = 42", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Summary_FinalTick_equals_42()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains("Summary.FinalTick = 42", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Summary_TotalEvaluationRecords_equals_1()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains(
            "Summary.TotalEvaluationRecords = 1",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_MatchScriptRuntimeEvaluationFrame()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains("MatchScriptRuntimeEvaluationFrame", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_ScriptRuntimeEvaluationRecord()
    {
        string text = MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter.Format(
            CreateResult());

        Assert.Contains("ScriptRuntimeEvaluationRecord", text, StringComparison.Ordinal);
    }

    private static ScriptProgram CreateProgram()
    {
        ScriptRoutine routine = new ScriptRoutine(
            "fire",
            ScriptCondition.Always(),
            new ScriptCommand(
                ScriptCommandType.Fire,
                string.Empty));

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

    private static ScriptRuntimeDecisionResult CreateDecisionResult()
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

    private static ScriptRuntimeEvaluationRecord CreateRecord(
        int tick,
        int tankIndex = 0)
    {
        return new ScriptRuntimeEvaluationRecord(
            new SimTick(tick),
            tankIndex,
            CreateProgram(),
            CreateContext(),
            CreateDecisionResult());
    }

    private static ScriptRuntimeEvaluationTrace CreateTrace(
        int tick = 42)
    {
        return new ScriptRuntimeEvaluationTrace(new[]
        {
            CreateRecord(tick),
        });
    }

    private static MatchScriptRuntimeEvaluationFrame CreateFrame(
        int frameIndex,
        int tick = 42)
    {
        return new MatchScriptRuntimeEvaluationFrame(
            frameIndex,
            new SimTick(tick),
            CreateTrace(tick));
    }

    private static MatchScriptRuntimeEvaluationRecording CreateRecording()
    {
        return new MatchScriptRuntimeEvaluationRecording(new[]
        {
            CreateFrame(0, tick: 42),
        });
    }

    private static MatchScriptRuntimeEvaluationRecordedRunResult CreateResult()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();
        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        return new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);
    }
}
