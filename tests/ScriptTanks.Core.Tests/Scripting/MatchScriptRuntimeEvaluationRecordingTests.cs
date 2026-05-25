using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordingTests
{
    [Fact]
    public void Constructor_rejects_null_frames_with_ParamName_frames()
    {
        IEnumerable<MatchScriptRuntimeEvaluationFrame>? frames = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptRuntimeEvaluationRecording(frames!));

        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_empty_frames_with_ParamName_frames()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptRuntimeEvaluationRecording(
                Array.Empty<MatchScriptRuntimeEvaluationFrame>()));

        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_frame_element_with_ParamName_frames()
    {
        MatchScriptRuntimeEvaluationFrame first = CreateFrame(0);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptRuntimeEvaluationRecording(
                new MatchScriptRuntimeEvaluationFrame?[] { first, null }!));

        Assert.Equal("frames", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_frames_in_order()
    {
        MatchScriptRuntimeEvaluationFrame a = CreateFrame(0);
        MatchScriptRuntimeEvaluationFrame b = CreateFrame(1);
        MatchScriptRuntimeEvaluationFrame c = CreateFrame(2);

        var recording = new MatchScriptRuntimeEvaluationRecording(
            new[] { a, b, c });

        Assert.Same(a, recording.Frames[0]);
        Assert.Same(b, recording.Frames[1]);
        Assert.Same(c, recording.Frames[2]);
    }

    [Fact]
    public void Constructor_defensively_copies_input_array()
    {
        MatchScriptRuntimeEvaluationFrame first = CreateFrame(0);
        MatchScriptRuntimeEvaluationFrame second = CreateFrame(1);
        MatchScriptRuntimeEvaluationFrame replacement = CreateFrame(2);

        MatchScriptRuntimeEvaluationFrame[] frames = { first, second };

        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(frames);

        frames[1] = replacement;

        Assert.Same(second, recording.Frames[1]);
    }

    [Fact]
    public void Frames_collection_is_read_only()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                CreateFrame(0),
            });

        IList<MatchScriptRuntimeEvaluationFrame> list =
            Assert.IsAssignableFrom<IList<MatchScriptRuntimeEvaluationFrame>>(
                recording.Frames);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateFrame(1)));
    }

    [Fact]
    public void Count_returns_frame_count()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                CreateFrame(0),
                CreateFrame(1),
            });

        Assert.Equal(2, recording.Count);
    }

    [Fact]
    public void GetFrameAtIndex_returns_frame_at_index_zero()
    {
        MatchScriptRuntimeEvaluationFrame first = CreateFrame(0);
        MatchScriptRuntimeEvaluationFrame second = CreateFrame(1);

        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                first,
                second,
            });

        Assert.Same(first, recording.GetFrameAtIndex(0));
    }

    [Fact]
    public void GetFrameAtIndex_returns_frame_at_later_index()
    {
        MatchScriptRuntimeEvaluationFrame first = CreateFrame(0);
        MatchScriptRuntimeEvaluationFrame second = CreateFrame(1);

        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                first,
                second,
            });

        Assert.Same(second, recording.GetFrameAtIndex(1));
    }

    [Fact]
    public void GetFrameAtIndex_rejects_negative_index_with_ParamName_frameIndex()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                CreateFrame(0),
            });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            recording.GetFrameAtIndex(-1));

        Assert.Equal("frameIndex", ex.ParamName);
    }

    [Fact]
    public void GetFrameAtIndex_rejects_past_end_index_with_ParamName_frameIndex()
    {
        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                CreateFrame(0),
            });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            recording.GetFrameAtIndex(1));

        Assert.Equal("frameIndex", ex.ParamName);
    }

    [Fact]
    public void Recording_allows_nonzero_first_stored_frame_index()
    {
        MatchScriptRuntimeEvaluationFrame frame = CreateFrame(5);

        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(new[]
            {
                frame,
            });

        Assert.Same(frame, recording.GetFrameAtIndex(0));
        Assert.Equal(5, recording.GetFrameAtIndex(0).FrameIndex);
    }

    [Fact]
    public void Two_recordings_with_same_frames_are_not_reference_equal()
    {
        MatchScriptRuntimeEvaluationFrame frame = CreateFrame(0);

        var first = new MatchScriptRuntimeEvaluationRecording(new[] { frame });
        var second = new MatchScriptRuntimeEvaluationRecording(new[] { frame });

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_recording_reference_equals_itself()
    {
        MatchScriptRuntimeEvaluationFrame frame = CreateFrame(0);

        var first = new MatchScriptRuntimeEvaluationRecording(new[] { frame });

        Assert.True(first.Equals(first));
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
}
