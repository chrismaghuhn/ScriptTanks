using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationPipelineTests
{
    [Fact]
    public void Evaluate_rejects_null_runtime_with_ParamName_runtime()
    {
        MatchScriptRuntimeState? runtime = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime!));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Evaluate_with_empty_runtime_returns_empty_trace()
    {
        MatchScriptRuntimeState runtime = CreateRuntime();

        ScriptRuntimeEvaluationTrace trace =
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime);

        Assert.Empty(trace.Records);
        Assert.Equal(0, trace.Count);
    }

    [Fact]
    public void Evaluate_returns_one_record_for_one_request()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0));

        ScriptRuntimeEvaluationTrace trace =
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime);

        Assert.Equal(1, trace.Count);
    }

    [Fact]
    public void Evaluate_returns_records_for_multiple_requests()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(2));

        ScriptRuntimeEvaluationTrace trace =
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime);

        Assert.Equal(2, trace.Count);
    }

    [Fact]
    public void Evaluate_applies_supplied_tick_to_all_records()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(1));

        ScriptRuntimeEvaluationTrace trace =
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime);

        Assert.Equal(new SimTick(42), trace.GetRecordAtIndex(0).Tick);
        Assert.Equal(new SimTick(42), trace.GetRecordAtIndex(1).Tick);
    }

    [Fact]
    public void Evaluate_preserves_request_order_and_tank_indexes()
    {
        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0),
            CreateRequest(2));

        ScriptRuntimeEvaluationTrace trace =
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime);

        Assert.Equal(0, trace.GetRecordAtIndex(0).TankIndex);
        Assert.Equal(2, trace.GetRecordAtIndex(1).TankIndex);
    }

    [Fact]
    public void Evaluate_propagates_invalid_condition_argument_with_ParamName_condition()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "invalid",
                MyHpBelow("abc"),
                Command(ScriptCommandType.Fire)));

        MatchScriptRuntimeState runtime = CreateRuntime(
            CreateRequest(0, program));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(42),
                runtime));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void Evaluate_does_not_mutate_runtime_state()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);

        MatchScriptRuntimeState runtime = CreateRuntime(first, second);

        _ = MatchScriptRuntimeEvaluationPipeline.Evaluate(
            new SimTick(42),
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
