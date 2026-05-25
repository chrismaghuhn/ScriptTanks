using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchSensorRuntimeReplaySummaryTests
{
    [Fact]
    public void Constructor_RejectsFrameCountZero_WithParamNameFrameCount()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeReplaySummary(
                frameCount: 0,
                ticksExecuted: 0,
                initialTick: new SimTick(10),
                finalTick: new SimTick(10),
                didEnd: false,
                endReason: MatchEndReason.None));

        Assert.Equal("frameCount", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeFrameCount_WithParamNameFrameCount()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeReplaySummary(
                frameCount: -1,
                ticksExecuted: 0,
                initialTick: new SimTick(10),
                finalTick: new SimTick(10),
                didEnd: false,
                endReason: MatchEndReason.None));

        Assert.Equal("frameCount", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeTicksExecuted_WithParamNameTicksExecuted()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorRuntimeReplaySummary(
                frameCount: 1,
                ticksExecuted: -1,
                initialTick: new SimTick(10),
                finalTick: new SimTick(10),
                didEnd: false,
                endReason: MatchEndReason.None));

        Assert.Equal("ticksExecuted", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsFinalTickBeforeInitialTick_WithParamNameFinalTick()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorRuntimeReplaySummary(
                frameCount: 2,
                ticksExecuted: 1,
                initialTick: new SimTick(10),
                finalTick: new SimTick(9),
                didEnd: true,
                endReason: MatchEndReason.TimeoutDraw));

        Assert.Equal("finalTick", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNotEndedSummaryWithNonNoneEndReason_WithParamNameEndReason()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorRuntimeReplaySummary(
                frameCount: 1,
                ticksExecuted: 0,
                initialTick: new SimTick(10),
                finalTick: new SimTick(10),
                didEnd: false,
                endReason: MatchEndReason.TimeoutDraw));

        Assert.Equal("endReason", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEndedSummaryWithNoneEndReason_WithParamNameEndReason()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorRuntimeReplaySummary(
                frameCount: 4,
                ticksExecuted: 3,
                initialTick: new SimTick(10),
                finalTick: new SimTick(13),
                didEnd: true,
                endReason: MatchEndReason.None));

        Assert.Equal("endReason", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsNotEndedSummaryWithEndReasonNone()
    {
        var summary = new MatchSensorRuntimeReplaySummary(
            frameCount: 1,
            ticksExecuted: 0,
            initialTick: new SimTick(10),
            finalTick: new SimTick(10),
            didEnd: false,
            endReason: MatchEndReason.None);

        Assert.False(summary.DidEnd);
        Assert.Equal(MatchEndReason.None, summary.EndReason);
    }

    [Fact]
    public void Constructor_AllowsEndedSummaryWithNonNoneEndReason()
    {
        var summary = new MatchSensorRuntimeReplaySummary(
            frameCount: 4,
            ticksExecuted: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(13),
            didEnd: true,
            endReason: MatchEndReason.TimeoutDraw);

        Assert.True(summary.DidEnd);
        Assert.Equal(MatchEndReason.TimeoutDraw, summary.EndReason);
    }

    [Fact]
    public void Constructor_PreservesFrameCountAndTicksExecuted()
    {
        var summary = new MatchSensorRuntimeReplaySummary(
            frameCount: 4,
            ticksExecuted: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(13),
            didEnd: true,
            endReason: MatchEndReason.TimeoutDraw);

        Assert.Equal(4, summary.FrameCount);
        Assert.Equal(3, summary.TicksExecuted);
    }

    [Fact]
    public void Constructor_PreservesInitialTickAndFinalTick()
    {
        var summary = new MatchSensorRuntimeReplaySummary(
            frameCount: 4,
            ticksExecuted: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(13),
            didEnd: true,
            endReason: MatchEndReason.TimeoutDraw);

        Assert.Equal(new SimTick(10), summary.InitialTick);
        Assert.Equal(new SimTick(13), summary.FinalTick);
    }

    [Fact]
    public void Constructor_PreservesDidEndAndEndReason()
    {
        var summary = new MatchSensorRuntimeReplaySummary(
            frameCount: 4,
            ticksExecuted: 3,
            initialTick: new SimTick(10),
            finalTick: new SimTick(13),
            didEnd: true,
            endReason: MatchEndReason.TimeoutDraw);

        Assert.True(summary.DidEnd);
        Assert.Equal(MatchEndReason.TimeoutDraw, summary.EndReason);
    }

    [Fact]
    public void Instances_AreDistinctAndUseDefaultReferenceEquality()
    {
        var first = new MatchSensorRuntimeReplaySummary(
            4,
            3,
            new SimTick(10),
            new SimTick(13),
            didEnd: true,
            MatchEndReason.TimeoutDraw);

        var second = new MatchSensorRuntimeReplaySummary(
            4,
            3,
            new SimTick(10),
            new SimTick(13),
            didEnd: true,
            MatchEndReason.TimeoutDraw);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }
}
