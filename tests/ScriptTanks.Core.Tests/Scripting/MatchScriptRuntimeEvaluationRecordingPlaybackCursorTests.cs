using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordingPlaybackCursorTests
{
    [Fact]
    public void Constructor_rejects_null_recording_with_ParamName_recording()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(null!, position: 0));

        Assert.Equal("recording", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_negative_position_with_ParamName_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
                recording,
                position: -1));

        Assert.Equal("position", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_past_end_position_with_ParamName_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
                recording,
                position: 3));

        Assert.Equal("position", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_recording_reference()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 0);

        Assert.Same(recording, cursor.Recording);
    }

    [Fact]
    public void Constructor_preserves_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 1);

        Assert.Equal(1, cursor.Position);
    }

    [Fact]
    public void CurrentFrame_returns_frame_at_collection_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 1);

        Assert.Same(recording.GetFrameAtIndex(1), cursor.CurrentFrame);
        Assert.Equal(20, cursor.CurrentFrame.FrameIndex);
    }

    [Fact]
    public void IsFirst_true_at_position_zero()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 0);

        Assert.True(cursor.IsFirst);
    }

    [Fact]
    public void IsFirst_false_at_later_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 1);

        Assert.False(cursor.IsFirst);
    }

    [Fact]
    public void IsLast_true_at_final_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 2);

        Assert.True(cursor.IsLast);
    }

    [Fact]
    public void IsLast_false_before_final_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 1);

        Assert.False(cursor.IsLast);
    }

    [Fact]
    public void MoveTo_returns_new_cursor_at_requested_position()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 0);

        MatchScriptRuntimeEvaluationRecordingPlaybackCursor moved =
            cursor.MoveTo(2);

        Assert.Equal(2, moved.Position);
        Assert.Same(recording, moved.Recording);
    }

    [Fact]
    public void MoveTo_does_not_mutate_original_cursor()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            0);

        MatchScriptRuntimeEvaluationRecordingPlaybackCursor moved =
            cursor.MoveTo(2);

        Assert.Equal(0, cursor.Position);
        Assert.Equal(2, moved.Position);
        Assert.Same(recording, moved.Recording);
    }

    [Fact]
    public void MoveNext_advances_when_not_last()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 0);

        MatchScriptRuntimeEvaluationRecordingPlaybackCursor moved =
            cursor.MoveNext();

        Assert.Equal(1, moved.Position);
        Assert.NotSame(cursor, moved);
    }

    [Fact]
    public void MoveNext_returns_same_instance_when_already_last()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 2);

        MatchScriptRuntimeEvaluationRecordingPlaybackCursor moved =
            cursor.MoveNext();

        Assert.Same(cursor, moved);
        Assert.True(moved.IsLast);
    }

    [Fact]
    public void MovePrevious_moves_back_when_not_first()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 1);

        MatchScriptRuntimeEvaluationRecordingPlaybackCursor moved =
            cursor.MovePrevious();

        Assert.Equal(0, moved.Position);
        Assert.NotSame(cursor, moved);
    }

    [Fact]
    public void MovePrevious_returns_same_instance_when_already_first()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        var cursor = new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            recording,
            position: 0);

        MatchScriptRuntimeEvaluationRecordingPlaybackCursor moved =
            cursor.MovePrevious();

        Assert.Same(cursor, moved);
        Assert.True(moved.IsFirst);
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
}
