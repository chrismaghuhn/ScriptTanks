using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeEvaluationTraceTextFormatterTests
{
    [Fact]
    public void FormatLines_rejects_null_trace_with_ParamName_trace()
    {
        ScriptRuntimeEvaluationTrace? trace = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeEvaluationTraceTextFormatter.FormatLines(trace!));

        Assert.Equal("trace", ex.ParamName);
    }

    [Fact]
    public void Format_rejects_null_trace_with_ParamName_trace()
    {
        ScriptRuntimeEvaluationTrace? trace = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeEvaluationTraceTextFormatter.Format(trace!));

        Assert.Equal("trace", ex.ParamName);
    }

    [Fact]
    public void FormatLines_returns_empty_read_only_list_for_empty_trace()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        IReadOnlyList<string> lines =
            ScriptRuntimeEvaluationTraceTextFormatter.FormatLines(trace);

        Assert.Empty(lines);
    }

    [Fact]
    public void Format_returns_empty_string_for_empty_trace()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace();

        string formatted =
            ScriptRuntimeEvaluationTraceTextFormatter.Format(trace);

        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatLines_preserves_record_order()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace(
            CreateRecord(1, tankIndex: 0),
            CreateRecord(2, tankIndex: 1));

        IReadOnlyList<string> lines =
            ScriptRuntimeEvaluationTraceTextFormatter.FormatLines(trace);

        Assert.Contains("Tick = 1", lines[0], StringComparison.Ordinal);
        Assert.Contains("TankIndex = 0", lines[0], StringComparison.Ordinal);
        Assert.Contains("Tick = 2", lines[1], StringComparison.Ordinal);
        Assert.Contains("TankIndex = 1", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_delegates_record_formatting_and_includes_record_text()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace(
            CreateRecord(1));

        IReadOnlyList<string> lines =
            ScriptRuntimeEvaluationTraceTextFormatter.FormatLines(trace);

        string expectedLine =
            ScriptRuntimeEvaluationRecordTextFormatter.Format(
                trace.GetRecordAtIndex(0));

        Assert.Single(lines);
        Assert.Equal(expectedLine, lines[0]);
        Assert.Contains("ScriptRuntimeEvaluationRecord", lines[0], StringComparison.Ordinal);
        Assert.Contains("ScriptRuntimeDecisionResult", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_returned_collection_is_read_only()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace(
            CreateRecord(1));

        IReadOnlyList<string> lines =
            ScriptRuntimeEvaluationTraceTextFormatter.FormatLines(trace);

        IList<string> list =
            Assert.IsAssignableFrom<IList<string>>(lines);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add("extra"));
    }

    [Fact]
    public void Format_joins_lines_with_Environment_NewLine()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace(
            CreateRecord(1),
            CreateRecord(2));

        string formatted =
            ScriptRuntimeEvaluationTraceTextFormatter.Format(trace);

        Assert.Contains(Environment.NewLine, formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_does_not_add_trailing_newline()
    {
        ScriptRuntimeEvaluationTrace trace = CreateTrace(
            CreateRecord(1),
            CreateRecord(2));

        string formatted =
            ScriptRuntimeEvaluationTraceTextFormatter.Format(trace);

        Assert.False(
            formatted.EndsWith(Environment.NewLine, StringComparison.Ordinal));
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
        params ScriptRuntimeEvaluationRecord[] records)
    {
        return new ScriptRuntimeEvaluationTrace(records);
    }
}
