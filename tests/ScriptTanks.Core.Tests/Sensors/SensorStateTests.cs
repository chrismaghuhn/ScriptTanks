using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class SensorStateTests
{
    private static SensorDefinition CreateDefinition(int cooldownTicks = 5)
    {
        return new SensorDefinition(
            "sensor",
            "Sensor",
            Fixed.FromInt(10),
            Fixed.FromInt(90),
            cooldownTicks,
            cpuCost: 3);
    }

    [Fact]
    public void Constructor_PreservesValues()
    {
        SensorDefinition definition = CreateDefinition();
        SimTick tick = new SimTick(7);

        SensorState state = new SensorState(definition, tick);

        Assert.Equal(definition, state.Definition);
        Assert.Equal(tick, state.LastScanTick);
    }

    [Fact]
    public void Ready_UsesSimTickZero()
    {
        SensorDefinition definition = CreateDefinition();

        SensorState state = SensorState.Ready(definition);

        Assert.Equal(definition, state.Definition);
        Assert.Equal(SimTick.Zero, state.LastScanTick);
    }

    [Fact]
    public void IsReady_ReturnsTrue_WhenCooldownElapsed()
    {
        SensorState state = new SensorState(CreateDefinition(cooldownTicks: 5), new SimTick(5));

        Assert.True(state.IsReady(new SimTick(10)));
    }

    [Fact]
    public void IsReady_ReturnsFalse_WhenCooldownNotElapsed()
    {
        SensorState state = new SensorState(CreateDefinition(cooldownTicks: 5), new SimTick(5));

        Assert.False(state.IsReady(new SimTick(9)));
    }

    [Fact]
    public void IsReady_ReturnsTrue_WhenCooldownIsZero()
    {
        SensorState state = new SensorState(CreateDefinition(cooldownTicks: 0), new SimTick(5));

        Assert.True(state.IsReady(new SimTick(5)));
    }

    [Fact]
    public void MarkScanned_ReturnsUpdatedTick()
    {
        SensorState state = SensorState.Ready(CreateDefinition());

        SensorState updated = state.MarkScanned(new SimTick(12));

        Assert.Equal(new SimTick(12), updated.LastScanTick);
    }

    [Fact]
    public void MarkScanned_PreservesDefinition()
    {
        SensorDefinition definition = CreateDefinition();
        SensorState state = SensorState.Ready(definition);

        SensorState updated = state.MarkScanned(new SimTick(3));

        Assert.Equal(definition, updated.Definition);
    }

    [Fact]
    public void MarkScanned_DoesNotMutateOriginal()
    {
        SensorState original = SensorState.Ready(CreateDefinition());

        SensorState updated = original.MarkScanned(new SimTick(4));

        Assert.Equal(SimTick.Zero, original.LastScanTick);
        Assert.Equal(new SimTick(4), updated.LastScanTick);
    }

    [Fact]
    public void Equality_OperatorsHashCodeAndToString_Work()
    {
        SensorDefinition definition = CreateDefinition();
        SensorState a = new SensorState(definition, new SimTick(2));
        SensorState b = new SensorState(definition, new SimTick(2));
        SensorState different = new SensorState(definition, new SimTick(3));

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.NotEqual(a, different);
        Assert.False(a == different);
        Assert.True(a != different);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        string text = a.ToString();
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void IsReady_HandlesCurrentTickEarlierThanLastScanAsNotReady()
    {
        SensorState state = new SensorState(CreateDefinition(cooldownTicks: 5), new SimTick(10));

        Assert.False(state.IsReady(new SimTick(8)));
    }
}
