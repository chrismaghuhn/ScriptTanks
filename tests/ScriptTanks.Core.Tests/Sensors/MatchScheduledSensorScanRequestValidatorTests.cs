using System;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchScheduledSensorScanRequestValidatorTests
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
    public void Validate_RejectsNullSchedule_WithParamNameScheduledScanRequests()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => MatchScheduledSensorScanRequestValidator.Validate(
                new SimTick(10),
                null!));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void Validate_AllowsEmptySchedule()
    {
        MatchScheduledSensorScanRequestValidator.Validate(
            new SimTick(10),
            Array.Empty<MatchScheduledSensorScanRequest>());
    }

    [Fact]
    public void Validate_AllowsRequestExactlyAtInitialTick()
    {
        MatchScheduledSensorScanRequestValidator.Validate(
            new SimTick(10),
            new[] { CreateScheduled(10) });
    }

    [Fact]
    public void Validate_AllowsStrictlyIncreasingFutureRequests()
    {
        MatchScheduledSensorScanRequestValidator.Validate(
            new SimTick(10),
            new[]
            {
                CreateScheduled(11),
                CreateScheduled(12),
                CreateScheduled(20),
            });
    }

    [Fact]
    public void Validate_RejectsRequestBeforeInitialTick_WithParamNameScheduledScanRequests()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchScheduledSensorScanRequestValidator.Validate(
                new SimTick(10),
                new[] { CreateScheduled(9) }));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void Validate_RejectsDuplicateTicks_WithParamNameScheduledScanRequests()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchScheduledSensorScanRequestValidator.Validate(
                new SimTick(10),
                new[]
                {
                    CreateScheduled(10),
                    CreateScheduled(10, tankIndex: 1),
                }));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void Validate_RejectsDecreasingTickOrder_WithParamNameScheduledScanRequests()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchScheduledSensorScanRequestValidator.Validate(
                new SimTick(10),
                new[]
                {
                    CreateScheduled(12),
                    CreateScheduled(11),
                }));

        Assert.Equal("scheduledScanRequests", ex.ParamName);
    }

    [Fact]
    public void Validate_AllowsSameRequestDataOnDifferentIncreasingTicks()
    {
        MatchScheduledSensorScanRequestValidator.Validate(
            new SimTick(10),
            new[]
            {
                CreateScheduled(10, tankIndex: 0, sensorSlot: 0),
                CreateScheduled(11, tankIndex: 0, sensorSlot: 0),
            });
    }

    [Fact]
    public void Validate_AllowsLargeTankIndex_RuntimeValidationOutOfScope()
    {
        MatchScheduledSensorScanRequestValidator.Validate(
            new SimTick(10),
            new[] { CreateScheduled(10, tankIndex: 999, sensorSlot: 0) });
    }

    [Fact]
    public void Validate_AllowsLaterSensorSlot_LoadoutValidationOutOfScope()
    {
        MatchScheduledSensorScanRequestValidator.Validate(
            new SimTick(10),
            new[] { CreateScheduled(10, tankIndex: 0, sensorSlot: 999) });
    }
}
