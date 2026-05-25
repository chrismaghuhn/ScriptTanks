using System;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeEvaluationRecordingSummaryTests
{
    [Fact]
    public void Constructor_rejects_zero_frameCount_with_ParamName_frameCount()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptRuntimeEvaluationRecordingSummary(
                frameCount: 0,
                initialTick: new SimTick(10),
                finalTick: new SimTick(12),
                totalEvaluationRecords: 5));

        Assert.Equal("frameCount", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_negative_frameCount_with_ParamName_frameCount()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptRuntimeEvaluationRecordingSummary(
                frameCount: -1,
                initialTick: new SimTick(10),
                finalTick: new SimTick(12),
                totalEvaluationRecords: 5));

        Assert.Equal("frameCount", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_finalTick_before_initialTick_with_ParamName_finalTick()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptRuntimeEvaluationRecordingSummary(
                frameCount: 3,
                initialTick: new SimTick(12),
                finalTick: new SimTick(10),
                totalEvaluationRecords: 5));

        Assert.Equal("finalTick", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_negative_totalEvaluationRecords_with_ParamName_totalEvaluationRecords()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptRuntimeEvaluationRecordingSummary(
                frameCount: 3,
                initialTick: new SimTick(10),
                finalTick: new SimTick(12),
                totalEvaluationRecords: -1));

        Assert.Equal("totalEvaluationRecords", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_frameCount()
    {
        var summary = new MatchScriptRuntimeEvaluationRecordingSummary(
            frameCount: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(12),
            totalEvaluationRecords: 5);

        Assert.Equal(3, summary.FrameCount);
    }

    [Fact]
    public void Constructor_preserves_initialTick()
    {
        var summary = new MatchScriptRuntimeEvaluationRecordingSummary(
            frameCount: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(12),
            totalEvaluationRecords: 5);

        Assert.Equal(new SimTick(10), summary.InitialTick);
    }

    [Fact]
    public void Constructor_preserves_finalTick()
    {
        var summary = new MatchScriptRuntimeEvaluationRecordingSummary(
            frameCount: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(12),
            totalEvaluationRecords: 5);

        Assert.Equal(new SimTick(12), summary.FinalTick);
    }

    [Fact]
    public void Constructor_preserves_totalEvaluationRecords()
    {
        var summary = new MatchScriptRuntimeEvaluationRecordingSummary(
            frameCount: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(12),
            totalEvaluationRecords: 5);

        Assert.Equal(5, summary.TotalEvaluationRecords);
    }

    [Fact]
    public void Constructor_allows_zero_totalEvaluationRecords()
    {
        var summary = new MatchScriptRuntimeEvaluationRecordingSummary(
            frameCount: 1,
            initialTick: new SimTick(10),
            finalTick: new SimTick(10),
            totalEvaluationRecords: 0);

        Assert.Equal(0, summary.TotalEvaluationRecords);
    }

    [Fact]
    public void Two_summaries_with_same_values_are_not_reference_equal()
    {
        var first = new MatchScriptRuntimeEvaluationRecordingSummary(
            3,
            new SimTick(10),
            new SimTick(12),
            5);

        var second = new MatchScriptRuntimeEvaluationRecordingSummary(
            3,
            new SimTick(10),
            new SimTick(12),
            5);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_summary_reference_equals_itself()
    {
        var first = new MatchScriptRuntimeEvaluationRecordingSummary(
            3,
            new SimTick(10),
            new SimTick(12),
            5);

        Assert.True(first.Equals(first));
    }
}
