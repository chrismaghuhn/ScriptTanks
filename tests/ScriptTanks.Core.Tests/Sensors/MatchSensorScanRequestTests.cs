using System;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchSensorScanRequestTests
{
    [Fact]
    public void Constructor_PreservesTankIndex()
    {
        var request = new MatchSensorScanRequest(5, SensorSlot.Zero);

        Assert.Equal(5, request.TankIndex);
    }

    [Fact]
    public void Constructor_PreservesSensorSlot()
    {
        var slot = new SensorSlot(2);
        var request = new MatchSensorScanRequest(0, slot);

        Assert.Equal(slot, request.SensorSlot);
    }

    [Fact]
    public void Constructor_RejectsNegativeTankIndex_WithParamNameTankIndex()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchSensorScanRequest(-1, SensorSlot.Zero));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsZeroTankIndex()
    {
        var request = new MatchSensorScanRequest(0, new SensorSlot(1));

        Assert.Equal(0, request.TankIndex);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameTankIndexAndSensorSlot()
    {
        var a = new MatchSensorScanRequest(4, new SensorSlot(1));
        var b = new MatchSensorScanRequest(4, new SensorSlot(1));

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentTankIndex()
    {
        var a = new MatchSensorScanRequest(0, SensorSlot.Zero);
        var b = new MatchSensorScanRequest(1, SensorSlot.Zero);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentSensorSlot()
    {
        var a = new MatchSensorScanRequest(2, SensorSlot.Zero);
        var b = new MatchSensorScanRequest(2, new SensorSlot(1));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EqualityOperators_MatchEqualsSemantics()
    {
        var left = new MatchSensorScanRequest(3, new SensorSlot(2));
        var same = new MatchSensorScanRequest(3, new SensorSlot(2));
        var different = new MatchSensorScanRequest(3, SensorSlot.Zero);

        Assert.True(left == same);
        Assert.False(left != same);
        Assert.False(left == different);
        Assert.True(left != different);
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        var a = new MatchSensorScanRequest(7, new SensorSlot(1));
        var b = new MatchSensorScanRequest(7, new SensorSlot(1));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsTypeNameAndFields_InvariantCulture()
    {
        var request = new MatchSensorScanRequest(3, new SensorSlot(2));

        string value = request.ToString();

        Assert.Contains("MatchSensorScanRequest", value, StringComparison.Ordinal);
        Assert.Contains("TankIndex = 3", value, StringComparison.Ordinal);
        Assert.Contains("SensorSlot = 2", value, StringComparison.Ordinal);
    }
}
