using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationTraceTests
{
    [Fact]
    public void Constructor_rejects_null_records_with_ParamName_records()
    {
        IEnumerable<ScriptRuntimeEvaluationRecord>? records = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeEvaluationTrace(records!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_allows_empty_records()
    {
        var trace = new ScriptRuntimeEvaluationTrace(
            Array.Empty<ScriptRuntimeEvaluationRecord>());

        Assert.Empty(trace.Records);
        Assert.Equal(0, trace.Count);
    }

    [Fact]
    public void Constructor_rejects_null_record_element_with_ParamName_records()
    {
        ScriptRuntimeEvaluationRecord first = CreateRecord(1);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptRuntimeEvaluationTrace(
                new ScriptRuntimeEvaluationRecord?[] { first, null }!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_records_in_order()
    {
        ScriptRuntimeEvaluationRecord a = CreateRecord(1);
        ScriptRuntimeEvaluationRecord b = CreateRecord(2);
        ScriptRuntimeEvaluationRecord c = CreateRecord(3);

        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { a, b, c });

        Assert.Same(a, trace.Records[0]);
        Assert.Same(b, trace.Records[1]);
        Assert.Same(c, trace.Records[2]);
    }

    [Fact]
    public void Constructor_defensively_copies_input_array()
    {
        ScriptRuntimeEvaluationRecord first = CreateRecord(1);
        ScriptRuntimeEvaluationRecord second = CreateRecord(2);
        ScriptRuntimeEvaluationRecord replacement = CreateRecord(3);

        ScriptRuntimeEvaluationRecord[] records = { first, second };

        var trace = new ScriptRuntimeEvaluationTrace(records);

        records[1] = replacement;

        Assert.Same(second, trace.Records[1]);
    }

    [Fact]
    public void Records_collection_is_read_only()
    {
        ScriptRuntimeEvaluationRecord first = CreateRecord(1);

        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { first });

        IList<ScriptRuntimeEvaluationRecord> list =
            Assert.IsAssignableFrom<IList<ScriptRuntimeEvaluationRecord>>(
                trace.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateRecord(2)));
    }

    [Fact]
    public void Count_returns_record_count()
    {
        ScriptRuntimeEvaluationRecord first = CreateRecord(1);
        ScriptRuntimeEvaluationRecord second = CreateRecord(2);

        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { first, second });

        Assert.Equal(2, trace.Count);
    }

    [Fact]
    public void GetRecordAtIndex_returns_record_at_index_zero()
    {
        ScriptRuntimeEvaluationRecord first = CreateRecord(1);
        ScriptRuntimeEvaluationRecord second = CreateRecord(2);

        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { first, second });

        Assert.Same(first, trace.GetRecordAtIndex(0));
    }

    [Fact]
    public void GetRecordAtIndex_returns_record_at_later_index()
    {
        ScriptRuntimeEvaluationRecord first = CreateRecord(1);
        ScriptRuntimeEvaluationRecord second = CreateRecord(2);

        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { first, second });

        Assert.Same(second, trace.GetRecordAtIndex(1));
    }

    [Fact]
    public void GetRecordAtIndex_rejects_negative_index_with_ParamName_recordIndex()
    {
        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { CreateRecord(1) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            trace.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_rejects_past_end_index_with_ParamName_recordIndex()
    {
        var trace = new ScriptRuntimeEvaluationTrace(
            new[] { CreateRecord(1) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            trace.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Two_traces_with_same_records_are_not_reference_equal()
    {
        ScriptRuntimeEvaluationRecord record = CreateRecord(1);

        var first = new ScriptRuntimeEvaluationTrace(new[] { record });
        var second = new ScriptRuntimeEvaluationTrace(new[] { record });

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_trace_reference_equals_itself()
    {
        ScriptRuntimeEvaluationRecord record = CreateRecord(1);

        var first = new ScriptRuntimeEvaluationTrace(new[] { record });

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
}
