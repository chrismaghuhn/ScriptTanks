using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordedRunResultTests
{
    [Fact]
    public void Constructor_rejects_null_recording_with_ParamName_recording()
    {
        MatchScriptRuntimeEvaluationRecordingSummary summary = CreateSummary();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptRuntimeEvaluationRecordedRunResult(null!, summary));

        Assert.Equal("recording", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_summary_with_ParamName_summary()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptRuntimeEvaluationRecordedRunResult(recording, null!));

        Assert.Equal("summary", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_recording_reference()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();
        MatchScriptRuntimeEvaluationRecordingSummary summary = CreateSummary(recording);

        var result = new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);

        Assert.Same(recording, result.Recording);
    }

    [Fact]
    public void Constructor_preserves_summary_reference()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();
        MatchScriptRuntimeEvaluationRecordingSummary summary = CreateSummary(recording);

        var result = new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);

        Assert.Same(summary, result.Summary);
    }

    [Fact]
    public void Two_results_with_same_references_are_not_reference_equal()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();
        MatchScriptRuntimeEvaluationRecordingSummary summary = CreateSummary(recording);

        var first = new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);

        var second = new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_result_reference_equals_itself()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording();
        MatchScriptRuntimeEvaluationRecordingSummary summary = CreateSummary(recording);

        var first = new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);

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

    private static MatchScriptRuntimeEvaluationRecordingSummary CreateSummary(
        MatchScriptRuntimeEvaluationRecording? recording = null)
    {
        return MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(
            recording ?? CreateRecording());
    }
}
