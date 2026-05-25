using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecorderTests
{
    [Fact]
    public void RecordFrame_rejects_negative_frameIndex_with_ParamName_frameIndex()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: -1,
                tick: new SimTick(42),
                runtime));

        Assert.Equal("frameIndex", ex.ParamName);
    }

    [Fact]
    public void RecordFrame_rejects_null_runtime_with_ParamName_runtime()
    {
        MatchScriptRuntimeState? runtime = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime!));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void RecordFrame_preserves_frameIndex()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0));

        MatchScriptRuntimeEvaluationFrame frame =
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime);

        Assert.Equal(3, frame.FrameIndex);
    }

    [Fact]
    public void RecordFrame_preserves_tick()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0));

        MatchScriptRuntimeEvaluationFrame frame =
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime);

        Assert.Equal(new SimTick(42), frame.Tick);
    }

    [Fact]
    public void RecordFrame_returns_empty_trace_for_empty_runtime()
    {
        MatchScriptRuntimeState runtime = CreateRuntime();

        MatchScriptRuntimeEvaluationFrame frame =
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime);

        Assert.Equal(3, frame.FrameIndex);
        Assert.Equal(new SimTick(42), frame.Tick);
        Assert.Empty(frame.Trace.Records);
        Assert.Equal(0, frame.Trace.Count);
    }

    [Fact]
    public void RecordFrame_records_one_trace_record_for_one_request()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0));

        MatchScriptRuntimeEvaluationFrame frame =
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime);

        Assert.Equal(1, frame.Trace.Count);
        Assert.Equal(0, frame.Trace.GetRecordAtIndex(0).TankIndex);
        Assert.Equal(new SimTick(42), frame.Trace.GetRecordAtIndex(0).Tick);
    }

    [Fact]
    public void RecordFrame_records_multiple_trace_records_for_multiple_requests()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(2));

        MatchScriptRuntimeEvaluationFrame frame =
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime);

        Assert.Equal(2, frame.Trace.Count);
        Assert.Equal(0, frame.Trace.GetRecordAtIndex(0).TankIndex);
        Assert.Equal(2, frame.Trace.GetRecordAtIndex(1).TankIndex);
    }

    [Fact]
    public void RecordFrame_propagates_invalid_condition_argument_with_ParamName_condition()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "invalid",
                MyHpBelow("abc"),
                Command(ScriptCommandType.Fire)));

        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0, program));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                frameIndex: 3,
                tick: new SimTick(42),
                runtime));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void RecordFrame_does_not_mutate_runtime_state()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);

        MatchScriptRuntimeState runtime = CreateRuntime(first, second);

        _ = MatchScriptRuntimeEvaluationRecorder.RecordFrame(
            frameIndex: 3,
            tick: new SimTick(42),
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
