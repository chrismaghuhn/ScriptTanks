using System;
using System.Collections.Generic;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionTraceTests
{
    [Fact]
    public void Constructor_rejects_null_snapshots_with_ParamName_snapshots()
    {
        IEnumerable<ScriptRuntimeDecisionSnapshot>? snapshots = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeDecisionTrace(snapshots!));

        Assert.Equal("snapshots", ex.ParamName);
    }

    [Fact]
    public void Constructor_allows_empty_snapshots()
    {
        var trace = new ScriptRuntimeDecisionTrace(
            Array.Empty<ScriptRuntimeDecisionSnapshot>());

        Assert.Empty(trace.Snapshots);
        Assert.Equal(0, trace.Count);
    }

    [Fact]
    public void Constructor_rejects_null_snapshot_element_with_ParamName_snapshots()
    {
        ScriptRuntimeDecisionSnapshot first = CreateSnapshot(1);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptRuntimeDecisionTrace(
                new ScriptRuntimeDecisionSnapshot?[] { first, null }!));

        Assert.Equal("snapshots", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_snapshots_in_order()
    {
        ScriptRuntimeDecisionSnapshot a = CreateSnapshot(1);
        ScriptRuntimeDecisionSnapshot b = CreateSnapshot(2);
        ScriptRuntimeDecisionSnapshot c = CreateSnapshot(3);

        var trace = new ScriptRuntimeDecisionTrace(
            new[] { a, b, c });

        Assert.Same(a, trace.Snapshots[0]);
        Assert.Same(b, trace.Snapshots[1]);
        Assert.Same(c, trace.Snapshots[2]);
    }

    [Fact]
    public void Constructor_defensively_copies_input_array()
    {
        ScriptRuntimeDecisionSnapshot first = CreateSnapshot(1);
        ScriptRuntimeDecisionSnapshot second = CreateSnapshot(2);
        ScriptRuntimeDecisionSnapshot replacement = CreateSnapshot(3);

        ScriptRuntimeDecisionSnapshot[] snapshots = { first, second };

        var trace = new ScriptRuntimeDecisionTrace(snapshots);

        snapshots[1] = replacement;

        Assert.Same(second, trace.Snapshots[1]);
    }

    [Fact]
    public void Snapshots_collection_is_read_only()
    {
        ScriptRuntimeDecisionSnapshot first = CreateSnapshot(1);

        var trace = new ScriptRuntimeDecisionTrace(
            new[] { first });

        IList<ScriptRuntimeDecisionSnapshot> list =
            Assert.IsAssignableFrom<IList<ScriptRuntimeDecisionSnapshot>>(
                trace.Snapshots);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateSnapshot(2)));
    }

    [Fact]
    public void Count_returns_snapshot_count()
    {
        ScriptRuntimeDecisionSnapshot first = CreateSnapshot(1);
        ScriptRuntimeDecisionSnapshot second = CreateSnapshot(2);

        var trace = new ScriptRuntimeDecisionTrace(
            new[] { first, second });

        Assert.Equal(2, trace.Count);
    }

    [Fact]
    public void GetSnapshotAtIndex_returns_snapshot_at_index_zero()
    {
        ScriptRuntimeDecisionSnapshot first = CreateSnapshot(1);
        ScriptRuntimeDecisionSnapshot second = CreateSnapshot(2);

        var trace = new ScriptRuntimeDecisionTrace(
            new[] { first, second });

        Assert.Same(first, trace.GetSnapshotAtIndex(0));
    }

    [Fact]
    public void GetSnapshotAtIndex_returns_snapshot_at_later_index()
    {
        ScriptRuntimeDecisionSnapshot first = CreateSnapshot(1);
        ScriptRuntimeDecisionSnapshot second = CreateSnapshot(2);

        var trace = new ScriptRuntimeDecisionTrace(
            new[] { first, second });

        Assert.Same(second, trace.GetSnapshotAtIndex(1));
    }

    [Fact]
    public void GetSnapshotAtIndex_rejects_negative_index_with_ParamName_snapshotIndex()
    {
        var trace = new ScriptRuntimeDecisionTrace(
            new[] { CreateSnapshot(1) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            trace.GetSnapshotAtIndex(-1));

        Assert.Equal("snapshotIndex", ex.ParamName);
    }

    [Fact]
    public void GetSnapshotAtIndex_rejects_past_end_index_with_ParamName_snapshotIndex()
    {
        var trace = new ScriptRuntimeDecisionTrace(
            new[] { CreateSnapshot(1) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            trace.GetSnapshotAtIndex(1));

        Assert.Equal("snapshotIndex", ex.ParamName);
    }

    [Fact]
    public void Two_traces_with_same_snapshots_are_not_reference_equal()
    {
        ScriptRuntimeDecisionSnapshot snapshot = CreateSnapshot(1);

        var first = new ScriptRuntimeDecisionTrace(new[] { snapshot });
        var second = new ScriptRuntimeDecisionTrace(new[] { snapshot });

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_trace_reference_equals_itself()
    {
        ScriptRuntimeDecisionSnapshot snapshot = CreateSnapshot(1);

        var first = new ScriptRuntimeDecisionTrace(new[] { snapshot });

        Assert.True(first.Equals(first));
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
}
