using System;
using System.Collections.Generic;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionTraceTextFormatterTests
{
    [Fact]
    public void FormatLines_rejects_null_trace_with_ParamName_trace()
    {
        ScriptRuntimeDecisionTrace? trace = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeDecisionTraceTextFormatter.FormatLines(trace!));

        Assert.Equal("trace", ex.ParamName);
    }

    [Fact]
    public void Format_rejects_null_trace_with_ParamName_trace()
    {
        ScriptRuntimeDecisionTrace? trace = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeDecisionTraceTextFormatter.Format(trace!));

        Assert.Equal("trace", ex.ParamName);
    }

    [Fact]
    public void FormatLines_returns_empty_read_only_list_for_empty_trace()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace();

        IReadOnlyList<string> lines =
            ScriptRuntimeDecisionTraceTextFormatter.FormatLines(trace);

        Assert.Empty(lines);
    }

    [Fact]
    public void Format_returns_empty_string_for_empty_trace()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace();

        string formatted =
            ScriptRuntimeDecisionTraceTextFormatter.Format(trace);

        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatLines_preserves_snapshot_order()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace(
            CreateSnapshot(1, tankIndex: 0),
            CreateSnapshot(2, tankIndex: 1));

        IReadOnlyList<string> lines =
            ScriptRuntimeDecisionTraceTextFormatter.FormatLines(trace);

        Assert.Contains("Tick = 1", lines[0], StringComparison.Ordinal);
        Assert.Contains("TankIndex = 0", lines[0], StringComparison.Ordinal);
        Assert.Contains("Tick = 2", lines[1], StringComparison.Ordinal);
        Assert.Contains("TankIndex = 1", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_delegates_snapshot_formatting_and_includes_snapshot_text()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace(CreateSnapshot(42, tankIndex: 3));

        IReadOnlyList<string> lines =
            ScriptRuntimeDecisionTraceTextFormatter.FormatLines(trace);

        string expectedLine =
            ScriptRuntimeDecisionSnapshotTextFormatter.Format(trace.GetSnapshotAtIndex(0));

        Assert.Single(lines);
        Assert.Equal(expectedLine, lines[0]);
        Assert.Contains("ScriptRuntimeDecisionSnapshot", lines[0], StringComparison.Ordinal);
        Assert.Contains("ScriptRuntimeDecisionResult", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLines_returned_collection_is_read_only()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace(
            CreateSnapshot(1));

        IReadOnlyList<string> lines =
            ScriptRuntimeDecisionTraceTextFormatter.FormatLines(trace);

        IList<string> list =
            Assert.IsAssignableFrom<IList<string>>(lines);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add("extra"));
    }

    [Fact]
    public void Format_joins_lines_with_Environment_NewLine()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace(
            CreateSnapshot(1),
            CreateSnapshot(2));

        string formatted =
            ScriptRuntimeDecisionTraceTextFormatter.Format(trace);

        Assert.Contains(Environment.NewLine, formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_does_not_add_trailing_newline()
    {
        ScriptRuntimeDecisionTrace trace = CreateTrace(
            CreateSnapshot(1),
            CreateSnapshot(2));

        string formatted =
            ScriptRuntimeDecisionTraceTextFormatter.Format(trace);

        Assert.False(
            formatted.EndsWith(Environment.NewLine, StringComparison.Ordinal));
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

    private static ScriptRuntimeDecisionSnapshot CreateSnapshot(
        int tick,
        int tankIndex = 0)
    {
        return new ScriptRuntimeDecisionSnapshot(
            new SimTick(tick),
            tankIndex,
            CreateResult());
    }

    private static ScriptRuntimeDecisionTrace CreateTrace(
        params ScriptRuntimeDecisionSnapshot[] snapshots)
    {
        return new ScriptRuntimeDecisionTrace(snapshots);
    }
}
