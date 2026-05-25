using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationFrameTests
{
    [Fact]
    public void Constructor_rejects_negative_frameIndex_with_ParamName_frameIndex()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptRuntimeEvaluationFrame(
                frameIndex: -1,
                tick: new SimTick(42),
                trace));

        Assert.Equal("frameIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_trace_with_ParamName_trace()
    {
        ScriptRuntimeEvaluationTrace? trace = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptRuntimeEvaluationFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                trace!));

        Assert.Equal("trace", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_frameIndex()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        var frame = new MatchScriptRuntimeEvaluationFrame(
            frameIndex: 3,
            tick: new SimTick(42),
            trace);

        Assert.Equal(3, frame.FrameIndex);
    }

    [Fact]
    public void Constructor_preserves_tick()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        var frame = new MatchScriptRuntimeEvaluationFrame(
            frameIndex: 3,
            tick: new SimTick(42),
            trace);

        Assert.Equal(new SimTick(42), frame.Tick);
    }

    [Fact]
    public void Constructor_preserves_trace_reference()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        var frame = new MatchScriptRuntimeEvaluationFrame(
            frameIndex: 3,
            tick: new SimTick(42),
            trace);

        Assert.Same(trace, frame.Trace);
    }

    [Fact]
    public void Constructor_allows_frameIndex_zero()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        var frame = new MatchScriptRuntimeEvaluationFrame(
            frameIndex: 0,
            tick: new SimTick(0),
            trace);

        Assert.Equal(0, frame.FrameIndex);
    }

    [Fact]
    public void Two_frames_with_same_values_are_not_reference_equal()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        var first = new MatchScriptRuntimeEvaluationFrame(
            3,
            new SimTick(42),
            trace);

        var second = new MatchScriptRuntimeEvaluationFrame(
            3,
            new SimTick(42),
            trace);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_frame_reference_equals_itself()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        var first = new MatchScriptRuntimeEvaluationFrame(
            3,
            new SimTick(42),
            trace);

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

    private static ScriptRuntimeEvaluationTrace CreateTrace()
    {
        return new ScriptRuntimeEvaluationTrace(new[]
        {
            CreateRecord(42),
        });
    }
}
