using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordingSummaryFactoryTests
{
    [Fact]
    public void Create_rejects_null_recording_with_ParamName_recording()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(null!));

        Assert.Equal("recording", ex.ParamName);
    }

    [Fact]
    public void Create_maps_frame_count()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(frameIndex: 0, tick: 1, CreateRecord(1, tankIndex: 0)),
            CreateFrame(frameIndex: 1, tick: 2, CreateRecord(2, tankIndex: 0)),
            CreateFrame(frameIndex: 2, tick: 3, CreateRecord(3, tankIndex: 0)));

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        Assert.Equal(3, summary.FrameCount);
    }

    [Fact]
    public void Create_maps_initial_tick_from_first_frame()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(frameIndex: 0, tick: 100, CreateRecord(100, tankIndex: 0)),
            CreateFrame(frameIndex: 1, tick: 200, CreateRecord(200, tankIndex: 0)));

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        Assert.Equal(new SimTick(100), summary.InitialTick);
    }

    [Fact]
    public void Create_maps_final_tick_from_last_frame()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(frameIndex: 0, tick: 100, CreateRecord(100, tankIndex: 0)),
            CreateFrame(frameIndex: 1, tick: 200, CreateRecord(200, tankIndex: 0)));

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        Assert.Equal(new SimTick(200), summary.FinalTick);
    }

    [Fact]
    public void Create_sums_total_evaluation_records_across_frames()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(
                frameIndex: 0,
                tick: 10,
                CreateRecord(10, tankIndex: 0),
                CreateRecord(10, tankIndex: 1)),
            CreateFrame(
                frameIndex: 1,
                tick: 11,
                CreateRecord(11, tankIndex: 0)));

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        Assert.Equal(2, summary.FrameCount);
        Assert.Equal(new SimTick(10), summary.InitialTick);
        Assert.Equal(new SimTick(11), summary.FinalTick);
        Assert.Equal(3, summary.TotalEvaluationRecords);
    }

    [Fact]
    public void Create_allows_frames_with_zero_records()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(frameIndex: 0, tick: 10),
            CreateFrame(
                frameIndex: 1,
                tick: 11,
                CreateRecord(11, tankIndex: 0)));

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        Assert.Equal(2, summary.FrameCount);
        Assert.Equal(1, summary.TotalEvaluationRecords);
    }

    [Fact]
    public void Create_uses_recording_order_not_stored_frame_index_order()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(frameIndex: 5, tick: 20),
            CreateFrame(frameIndex: 2, tick: 30));

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        Assert.Equal(new SimTick(20), summary.InitialTick);
        Assert.Equal(new SimTick(30), summary.FinalTick);
    }

    [Fact]
    public void Create_propagates_summary_finalTick_validation_for_decreasing_recording_order_ticks_with_ParamName_finalTick()
    {
        MatchScriptRuntimeEvaluationRecording recording = CreateRecording(
            CreateFrame(frameIndex: 0, tick: 30),
            CreateFrame(frameIndex: 1, tick: 20));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording));

        Assert.Equal("finalTick", ex.ParamName);
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
        int tankIndex)
    {
        return new ScriptRuntimeEvaluationRecord(
            new SimTick(tick),
            tankIndex,
            CreateProgram(),
            CreateContext(),
            CreateResult());
    }

    private static ScriptRuntimeEvaluationTrace CreateTrace(
        params ScriptRuntimeEvaluationRecord[] records)
    {
        return new ScriptRuntimeEvaluationTrace(records);
    }

    private static MatchScriptRuntimeEvaluationFrame CreateFrame(
        int frameIndex,
        int tick,
        params ScriptRuntimeEvaluationRecord[] records)
    {
        return new MatchScriptRuntimeEvaluationFrame(
            frameIndex,
            new SimTick(tick),
            CreateTrace(records));
    }

    private static MatchScriptRuntimeEvaluationRecording CreateRecording(
        params MatchScriptRuntimeEvaluationFrame[] frames)
    {
        return new MatchScriptRuntimeEvaluationRecording(frames);
    }
}
