using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationBatchPipelineTests
{
    [Fact]
    public void Evaluate_rejects_null_requests_with_ParamName_requests()
    {
        IReadOnlyList<ScriptRuntimeEvaluationRequest>? requests = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                requests!));

        Assert.Equal("requests", ex.ParamName);
    }

    [Fact]
    public void Evaluate_allows_empty_requests_and_returns_empty_trace()
    {
        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                Array.Empty<ScriptRuntimeEvaluationRequest>());

        Assert.Empty(trace.Records);
        Assert.Equal(0, trace.Count);
    }

    [Fact]
    public void Evaluate_rejects_null_request_element_with_ParamName_requests()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new ScriptRuntimeEvaluationRequest?[] { first, null }!));

        Assert.Equal("requests", ex.ParamName);
    }

    [Fact]
    public void Evaluate_returns_one_record_for_one_request()
    {
        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[] { CreateRequest(0) });

        Assert.Equal(1, trace.Count);
    }

    [Fact]
    public void Evaluate_returns_one_record_per_request_for_multiple_requests()
    {
        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[]
                {
                    CreateRequest(0),
                    CreateRequest(1),
                });

        Assert.Equal(2, trace.Count);
    }

    [Fact]
    public void Evaluate_preserves_request_order()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(2);

        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[] { first, second });

        Assert.Equal(2, trace.Count);
        Assert.Equal(0, trace.GetRecordAtIndex(0).TankIndex);
        Assert.Equal(2, trace.GetRecordAtIndex(1).TankIndex);
    }

    [Fact]
    public void Evaluate_applies_shared_tick_to_all_records()
    {
        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[]
                {
                    CreateRequest(0),
                    CreateRequest(1),
                });

        Assert.Equal(new SimTick(42), trace.GetRecordAtIndex(0).Tick);
        Assert.Equal(new SimTick(42), trace.GetRecordAtIndex(1).Tick);
    }

    [Fact]
    public void Evaluate_preserves_tank_indexes_per_request()
    {
        ScriptRuntimeEvaluationRequest[] requests =
        {
            CreateRequest(3),
            CreateRequest(5),
            CreateRequest(7),
        };

        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(10),
                requests);

        Assert.Equal(requests[0].TankIndex, trace.GetRecordAtIndex(0).TankIndex);
        Assert.Equal(requests[1].TankIndex, trace.GetRecordAtIndex(1).TankIndex);
        Assert.Equal(requests[2].TankIndex, trace.GetRecordAtIndex(2).TankIndex);
    }

    [Fact]
    public void Evaluate_preserves_program_references_per_request()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                Command(ScriptCommandType.Fire)));

        ScriptRuntimeEvaluationRequest request =
            CreateRequest(1, program);

        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[] { request });

        ScriptRuntimeEvaluationRecord record = trace.GetRecordAtIndex(0);

        Assert.Same(program, record.Program);
    }

    [Fact]
    public void Evaluate_preserves_context_values_per_request()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                Command(ScriptCommandType.Fire)));

        ScriptEvaluationContext context = CreateContext(
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12));

        ScriptRuntimeEvaluationRequest request =
            CreateRequest(1, program, context);

        ScriptRuntimeEvaluationTrace trace =
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[] { request });

        ScriptRuntimeEvaluationRecord record = trace.GetRecordAtIndex(0);

        Assert.Equal(context, record.Context);
    }

    [Fact]
    public void Evaluate_propagates_invalid_condition_argument_with_ParamName_condition()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "invalid",
                MyHpBelow("abc"),
                Command(ScriptCommandType.Fire)));

        ScriptRuntimeEvaluationRequest request =
            CreateRequest(0, program);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptRuntimeEvaluationBatchPipeline.Evaluate(
                new SimTick(42),
                new[] { request }));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void Evaluate_does_not_mutate_source_program()
    {
        ScriptRoutine first = CreateRoutine(
            "first",
            Always(),
            Command(ScriptCommandType.Fire));

        ScriptRoutine second = CreateRoutine(
            "second",
            EnemyVisible(),
            Command(ScriptCommandType.ScanEnemy));

        ScriptProgram program = CreateProgram(first, second);

        _ = ScriptRuntimeEvaluationBatchPipeline.Evaluate(
            new SimTick(42),
            new[] { CreateRequest(0, program) });

        Assert.Equal(2, program.Count);
        Assert.Equal(first, program.GetRoutineAtIndex(0));
        Assert.Equal(second, program.GetRoutineAtIndex(1));
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

    private static ScriptCondition EnemyVisible()
    {
        return new ScriptCondition(
            ScriptConditionType.EnemyVisible,
            string.Empty);
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
}
