using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationFrameTextFormatterTests
{
    [Fact]
    public void Format_rejects_null_frame_with_ParamName_frame()
    {
        MatchScriptRuntimeEvaluationFrame? frame = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(frame!));

        Assert.Equal("frame", ex.ParamName);
    }

    [Fact]
    public void Formatted_text_includes_MatchScriptRuntimeEvaluationFrame()
    {
        string text =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                CreateFrame());

        Assert.Contains("MatchScriptRuntimeEvaluationFrame", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_FrameIndex_3()
    {
        string text =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                CreateFrame());

        Assert.Contains("FrameIndex = 3", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Tick_42()
    {
        string text =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                CreateFrame());

        Assert.Contains("Tick = 42", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Trace_Count_1()
    {
        string text =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                CreateFrame());

        Assert.Contains("Trace.Count = 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_ScriptRuntimeEvaluationRecord()
    {
        string text =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                CreateFrame());

        Assert.Contains("ScriptRuntimeEvaluationRecord", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_ScriptRuntimeDecisionResult()
    {
        string text =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                CreateFrame());

        Assert.Contains("ScriptRuntimeDecisionResult", text, StringComparison.Ordinal);
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

    private static ScriptRuntimeDecisionResult CreateResult()
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
            CreateResult());
    }

    private static ScriptRuntimeEvaluationTrace CreateTrace()
    {
        return new ScriptRuntimeEvaluationTrace(new[]
        {
            CreateRecord(42),
        });
    }

    private static MatchScriptRuntimeEvaluationFrame CreateFrame()
    {
        return new MatchScriptRuntimeEvaluationFrame(
            frameIndex: 3,
            tick: new SimTick(42),
            trace: CreateTrace());
    }
}
