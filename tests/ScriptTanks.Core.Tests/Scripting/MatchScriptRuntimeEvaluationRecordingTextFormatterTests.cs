using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordingTextFormatterTests
{
    [Fact]
    public void FormatLines_rejects_null_recording_with_ParamName_recording()
    {
        MatchScriptRuntimeEvaluationRecording? recording = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationRecordingTextFormatter.FormatLines(recording!));

        Assert.Equal("recording", ex.ParamName);
    }

    [Fact]
    public void Format_rejects_null_recording_with_ParamName_recording()
    {
        MatchScriptRuntimeEvaluationRecording? recording = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationRecordingTextFormatter.Format(recording!));

        Assert.Equal("recording", ex.ParamName);
    }

    [Fact]
    public void FormatLines_returns_one_line_for_one_frame()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            CreateRecording(CreateFrame(0));

        IReadOnlyList<string> lines =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.FormatLines(
                recording);

        Assert.Single(lines);
        Assert.Contains(
            "MatchScriptRuntimeEvaluationFrame",
            lines[0],
            StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_preserves_frame_order()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            CreateRecording(
                CreateFrame(0, tick: 10),
                CreateFrame(1, tick: 11));

        IReadOnlyList<string> lines =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.FormatLines(
                recording);

        Assert.Contains("FrameIndex = 0", lines[0], StringComparison.Ordinal);
        Assert.Contains("Tick = 10", lines[0], StringComparison.Ordinal);
        Assert.Contains("FrameIndex = 1", lines[1], StringComparison.Ordinal);
        Assert.Contains("Tick = 11", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_delegates_frame_formatting_and_includes_nested_text()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            CreateRecording(CreateFrame(3, tick: 42));

        IReadOnlyList<string> lines =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.FormatLines(
                recording);

        string expectedLine =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                recording.GetFrameAtIndex(0));

        Assert.Single(lines);
        Assert.Equal(expectedLine, lines[0]);
        Assert.Contains("MatchScriptRuntimeEvaluationFrame", lines[0], StringComparison.Ordinal);
        Assert.Contains("ScriptRuntimeEvaluationRecord", lines[0], StringComparison.Ordinal);
        Assert.Contains("ScriptRuntimeDecisionResult", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_returned_collection_is_read_only()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            CreateRecording(CreateFrame(0));

        IReadOnlyList<string> lines =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.FormatLines(recording);

        IList<string> list =
            Assert.IsAssignableFrom<IList<string>>(lines);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add("extra"));
    }

    [Fact]
    public void Format_joins_lines_with_Environment_NewLine()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            CreateRecording(
                CreateFrame(0, tick: 10),
                CreateFrame(1, tick: 11));

        string formatted =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.Format(recording);

        Assert.Contains(Environment.NewLine, formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_does_not_add_trailing_newline()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            CreateRecording(
                CreateFrame(0, tick: 10),
                CreateFrame(1, tick: 11));

        string formatted =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.Format(recording);

        Assert.False(
            formatted.EndsWith(Environment.NewLine, StringComparison.Ordinal));
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

    private static MatchScriptRuntimeEvaluationRecording CreateRecording(
        params MatchScriptRuntimeEvaluationFrame[] frames)
    {
        return new MatchScriptRuntimeEvaluationRecording(frames);
    }
}
