using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeFixedTickRecorderTests
{
    [Fact]
    public void RecordForTicks_rejects_zero_tickCount_with_ParamName_tickCount()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(CreateRequest(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 0,
                runtime));

        Assert.Equal("tickCount", ex.ParamName);
    }

    [Fact]
    public void RecordForTicks_rejects_negative_tickCount_with_ParamName_tickCount()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(CreateRequest(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: -1,
                runtime));

        Assert.Equal("tickCount", ex.ParamName);
    }

    [Fact]
    public void RecordForTicks_rejects_null_runtime_with_ParamName_runtime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 1,
                null!));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void RecordForTicks_records_exactly_one_frame_for_tickCount_one()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(CreateRequest(0));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 1,
                runtime);

        Assert.Equal(1, result.Recording.Count);
        Assert.Equal(0, result.Recording.GetFrameAtIndex(0).FrameIndex);
        Assert.Equal(new SimTick(10), result.Recording.GetFrameAtIndex(0).Tick);
    }

    [Fact]
    public void RecordForTicks_recording_count_equals_tickCount()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(CreateRequest(0));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(1),
                tickCount: 5,
                runtime);

        Assert.Equal(5, result.Recording.Count);
    }

    [Fact]
    public void RecordForTicks_generates_sequential_frame_indexes_and_ticks_from_initial()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(1));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 3,
                runtime);

        Assert.Equal(0, result.Recording.GetFrameAtIndex(0).FrameIndex);
        Assert.Equal(1, result.Recording.GetFrameAtIndex(1).FrameIndex);
        Assert.Equal(2, result.Recording.GetFrameAtIndex(2).FrameIndex);

        Assert.Equal(new SimTick(10), result.Recording.GetFrameAtIndex(0).Tick);
        Assert.Equal(new SimTick(11), result.Recording.GetFrameAtIndex(1).Tick);
        Assert.Equal(new SimTick(12), result.Recording.GetFrameAtIndex(2).Tick);
    }

    [Fact]
    public void RecordForTicks_each_frame_trace_record_count_matches_runtime_request_count()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(2));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 2,
                runtime);

        Assert.Equal(2, result.Recording.GetFrameAtIndex(0).Trace.Count);
        Assert.Equal(2, result.Recording.GetFrameAtIndex(1).Trace.Count);

        Assert.Equal(0, result.Recording.GetFrameAtIndex(0).Trace.GetRecordAtIndex(0).TankIndex);
        Assert.Equal(2, result.Recording.GetFrameAtIndex(0).Trace.GetRecordAtIndex(1).TankIndex);
    }

    [Fact]
    public void RecordForTicks_summary_frame_count_matches_recording()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(1));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 3,
                runtime);

        Assert.Equal(3, result.Summary.FrameCount);
    }

    [Fact]
    public void RecordForTicks_summary_initial_and_final_ticks_match_first_and_last_frames()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(1));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 3,
                runtime);

        Assert.Equal(new SimTick(10), result.Summary.InitialTick);
        Assert.Equal(new SimTick(12), result.Summary.FinalTick);
    }

    [Fact]
    public void RecordForTicks_summary_total_evaluation_records_sums_across_frames()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(1));

        MatchScriptRuntimeEvaluationRecordedRunResult result =
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 3,
                runtime);

        Assert.Equal(6, result.Summary.TotalEvaluationRecords);
    }

    [Fact]
    public void RecordForTicks_invalid_condition_argument_propagates_with_ParamName_condition()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "invalid",
                MyHpBelow("abc"),
                Command(ScriptCommandType.Fire)));

        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0, program));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
                initialTick: new SimTick(10),
                tickCount: 2,
                runtime));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void RecordForTicks_does_not_mutate_runtime_state()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);

        MatchScriptRuntimeState runtime = CreateRuntime(first, second);

        _ = MatchScriptRuntimeFixedTickRecorder.RecordForTicks(
            initialTick: new SimTick(10),
            tickCount: 2,
            runtime);

        Assert.Equal(2, runtime.Count);
        Assert.Same(first, runtime.GetRequestAtIndex(0));
        Assert.Same(second, runtime.GetRequestAtIndex(1));
    }

    private static ScriptEvaluationContext CreateContext(
        bool enemyVisible = true,
        bool weaponReady = true,
        int myHitPoints = 50,
        Fixed? enemyDistance = null,
        bool sensorReady = true)
    {
        return new ScriptEvaluationContext(
            enemyVisible,
            weaponReady,
            myHitPoints,
            enemyDistance ?? Fixed.FromInt(10),
            sensorReady,
            ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptCommand Command(
        ScriptCommandType type,
        string argument = "")
    {
        return new ScriptCommand(type, argument);
    }

    private static ScriptCondition Always()
    {
        return ScriptCondition.Always();
    }

    private static ScriptCondition MyHpBelow(string threshold)
    {
        return new ScriptCondition(
            ScriptConditionType.MyHpBelow,
            threshold);
    }

    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptCondition condition,
        ScriptCommand command)
    {
        return new ScriptRoutine(
            name,
            condition,
            command);
    }

    private static ScriptProgram CreateProgram(
        params ScriptRoutine[] routines)
    {
        return new ScriptProgram(routines);
    }

    private static ScriptRuntimeEvaluationRequest CreateRequest(
        int tankIndex,
        ScriptProgram? program = null,
        ScriptEvaluationContext? context = null)
    {
        return new ScriptRuntimeEvaluationRequest(
            tankIndex,
            program ?? CreateProgram(
                CreateRoutine(
                    "fire",
                    Always(),
                    Command(ScriptCommandType.Fire))),
            context ?? CreateContext());
    }

    private static MatchScriptRuntimeState CreateRuntime(
        params ScriptRuntimeEvaluationRequest[] requests)
    {
        return new MatchScriptRuntimeState(requests);
    }
}
