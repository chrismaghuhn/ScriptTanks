using System;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchScheduledSensorScanRequestTests
{
    [Fact]
    public void Constructor_PreservesTick()
    {
        var tick = new SimTick(7);
        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);
        var scheduled = new MatchScheduledSensorScanRequest(tick, request);

        Assert.Equal(tick, scheduled.Tick);
    }

    [Fact]
    public void Constructor_PreservesRequest()
    {
        var request = new MatchSensorScanRequest(1, new SensorSlot(2));
        var scheduled = new MatchScheduledSensorScanRequest(new SimTick(42), request);

        Assert.Equal(request, scheduled.Request);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameTickAndRequest()
    {
        var request = new MatchSensorScanRequest(1, new SensorSlot(2));
        var a = new MatchScheduledSensorScanRequest(new SimTick(10), request);
        var b = new MatchScheduledSensorScanRequest(new SimTick(10), request);

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentTick()
    {
        var request = new MatchSensorScanRequest(0, SensorSlot.Zero);
        var a = new MatchScheduledSensorScanRequest(new SimTick(5), request);
        var b = new MatchScheduledSensorScanRequest(new SimTick(6), request);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentRequest()
    {
        var tick = new SimTick(3);
        var a = new MatchScheduledSensorScanRequest(
            tick,
            new MatchSensorScanRequest(0, SensorSlot.Zero));
        var b = new MatchScheduledSensorScanRequest(
            tick,
            new MatchSensorScanRequest(1, SensorSlot.Zero));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EqualityOperators_MatchEqualsSemantics()
    {
        var request = new MatchSensorScanRequest(2, new SensorSlot(1));
        var left = new MatchScheduledSensorScanRequest(new SimTick(4), request);
        var same = new MatchScheduledSensorScanRequest(new SimTick(4), request);
        var different = new MatchScheduledSensorScanRequest(
            new SimTick(9),
            request);

        Assert.True(left == same);
        Assert.False(left != same);
        Assert.False(left == different);
        Assert.True(left != different);
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        var request = new MatchSensorScanRequest(1, new SensorSlot(0));
        var a = new MatchScheduledSensorScanRequest(new SimTick(8), request);
        var b = new MatchScheduledSensorScanRequest(new SimTick(8), request);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsTypeNameAndFields_UsingInvariantCulture()
    {
        var request = new MatchSensorScanRequest(1, new SensorSlot(2));
        var scheduled = new MatchScheduledSensorScanRequest(new SimTick(42), request);

        string value = scheduled.ToString();

        Assert.Contains("MatchScheduledSensorScanRequest", value, StringComparison.Ordinal);
        Assert.Contains("Tick = 42", value, StringComparison.Ordinal);
        Assert.Contains("Request =", value, StringComparison.Ordinal);
        Assert.Contains("TankIndex = 1", value, StringComparison.Ordinal);
        Assert.Contains("SensorSlot = 2", value, StringComparison.Ordinal);
    }
}
