using System;
using System.Collections.Generic;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchScheduledSensorScanRequestSelectorTests
{
    private static MatchScheduledSensorScanRequest CreateScheduled(
        int tick,
        int tankIndex = 0,
        int sensorSlot = 0)
    {
        return new MatchScheduledSensorScanRequest(
            new SimTick(tick),
            new MatchSensorScanRequest(
                tankIndex,
                new SensorSlot(sensorSlot)));
    }

    [Fact]
    public void TrySelectForTick_RejectsNullSchedule_WithParamNameScheduledScanRequests()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchScheduledSensorScanRequestSelector.TrySelectForTick(
                null!,
                new SimTick(10),
                0,
                out _));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void TrySelectForTick_RejectsNegativeNextIndex_WithParamNameNextIndex()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchScheduledSensorScanRequestSelector.TrySelectForTick(
                Array.Empty<MatchScheduledSensorScanRequest>(),
                new SimTick(10),
                -1,
                out _));

        Assert.Equal("nextIndex", ex.ParamName);
    }

    [Fact]
    public void TrySelectForTick_EmptySchedule_ReturnsFalse()
    {
        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            Array.Empty<MatchScheduledSensorScanRequest>(),
            new SimTick(10),
            nextIndex: 0,
            out MatchSensorScanRequest request);

        Assert.False(selected);
        Assert.Equal(default, request);
    }

    [Fact]
    public void TrySelectForTick_NextIndexEqualToCount_ReturnsFalse()
    {
        MatchScheduledSensorScanRequest[] schedule =
        {
            CreateScheduled(10),
            CreateScheduled(11),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(10),
            nextIndex: 2,
            out MatchSensorScanRequest request);

        Assert.False(selected);
        Assert.Equal(default, request);
    }

    [Fact]
    public void TrySelectForTick_NextIndexGreaterThanCount_ReturnsFalse()
    {
        MatchScheduledSensorScanRequest[] schedule =
        {
            CreateScheduled(10),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(10),
            nextIndex: 3,
            out MatchSensorScanRequest request);

        Assert.False(selected);
        Assert.Equal(default, request);
    }

    [Fact]
    public void TrySelectForTick_MatchingTick_ReturnsTrue()
    {
        var schedule = new[]
        {
            CreateScheduled(10),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(10),
            nextIndex: 0,
            out MatchSensorScanRequest request);

        Assert.True(selected);
    }

    [Fact]
    public void TrySelectForTick_MatchingTick_ReturnsScheduledRequest()
    {
        var expectedRequest = new MatchSensorScanRequest(
            1,
            new SensorSlot(2));

        var schedule = new[]
        {
            new MatchScheduledSensorScanRequest(new SimTick(10), expectedRequest),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(10),
            nextIndex: 0,
            out MatchSensorScanRequest request);

        Assert.True(selected);
        Assert.Equal(expectedRequest, request);
    }

    [Fact]
    public void TrySelectForTick_EarlierScheduledTick_ReturnsFalse()
    {
        var schedule = new[]
        {
            CreateScheduled(20),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(10),
            nextIndex: 0,
            out MatchSensorScanRequest request);

        Assert.False(selected);
        Assert.Equal(default, request);
    }

    [Fact]
    public void TrySelectForTick_LaterScheduledTick_ReturnsFalse()
    {
        var schedule = new[]
        {
            CreateScheduled(10),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(20),
            nextIndex: 0,
            out MatchSensorScanRequest request);

        Assert.False(selected);
        Assert.Equal(default, request);
    }

    [Fact]
    public void TrySelectForTick_NonzeroNextIndex_SelectsRequestAtThatIndex()
    {
        MatchScheduledSensorScanRequest[] schedule =
        {
            CreateScheduled(10),
            CreateScheduled(11),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(11),
            nextIndex: 1,
            out MatchSensorScanRequest request);

        Assert.True(selected);
        Assert.Equal(CreateScheduled(11).Request, request);
    }

    [Fact]
    public void TrySelectForTick_DoesNotMutateSchedule()
    {
        MatchScheduledSensorScanRequest first = CreateScheduled(10);
        MatchScheduledSensorScanRequest second = CreateScheduled(11);
        var schedule = new List<MatchScheduledSensorScanRequest> { first, second };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(11),
            nextIndex: 1,
            out MatchSensorScanRequest request);

        Assert.True(selected);
        Assert.Equal(2, schedule.Count);
        Assert.Equal(first, schedule[0]);
        Assert.Equal(second, schedule[1]);
        Assert.Equal(second.Request, request);
    }

    [Fact]
    public void TrySelectForTick_DoesNotRejectUnsortedSchedule_OnlyChecksIndexedEntry()
    {
        MatchScheduledSensorScanRequest[] schedule =
        {
            CreateScheduled(20),
            CreateScheduled(10, tankIndex: 1),
        };

        bool selected = MatchScheduledSensorScanRequestSelector.TrySelectForTick(
            schedule,
            new SimTick(10),
            nextIndex: 1,
            out MatchSensorScanRequest request);

        Assert.True(selected);
        Assert.Equal(new MatchSensorScanRequest(1, SensorSlot.Zero), request);
    }
}
