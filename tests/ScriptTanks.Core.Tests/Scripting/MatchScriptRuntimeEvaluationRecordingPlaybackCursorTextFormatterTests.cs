using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatterTests
{
    [Fact]
    public void Format_rejects_null_cursor_with_ParamName_cursor()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(null!));

        Assert.Equal("cursor", ex.ParamName);
    }

    [Fact]
    public void Formatted_text_includes_MatchScriptRuntimeEvaluationRecordingPlaybackCursor()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains(
            "MatchScriptRuntimeEvaluationRecordingPlaybackCursor",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Position_equals_1()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("Position = 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_Recording_Count_equals_3()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("Recording.Count = 3", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_IsFirst_equals_False()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("IsFirst = False", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_IsLast_equals_False()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("IsLast = False", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_CurrentFrame_FrameIndex_equals_20()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("CurrentFrame.FrameIndex = 20", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_CurrentFrame_Tick_equals_101()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("CurrentFrame.Tick = 101", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatted_text_includes_inner_MatchScriptRuntimeEvaluationFrame()
    {
        string text = MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter.Format(
            CreateCursor());

        Assert.Contains("MatchScriptRuntimeEvaluationFrame", text, StringComparison.Ordinal);
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
        int tick)
    {
        return new ScriptRuntimeEvaluationTrace(new[]
        {
            CreateRecord(tick),
        });
    }

    private static MatchScriptRuntimeEvaluationFrame CreateFrame(
        int frameIndex,
        int tick)
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
            CreateFrame(10, tick: 100),
            CreateFrame(20, tick: 101),
            CreateFrame(30, tick: 102),
        });
    }

    private static MatchScriptRuntimeEvaluationRecordingPlaybackCursor CreateCursor()
    {
        return new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            CreateRecording(),
            position: 1);
    }
}
