using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class TankSensorLoadoutTests
{
    private static SensorDefinition CreateDefinition(
        string id = "sensor",
        int cooldownTicks = 5)
    {
        return new SensorDefinition(
            id,
            id,
            Fixed.FromInt(10),
            Fixed.FromInt(90),
            cooldownTicks,
            cpuCost: 1);
    }

    private static SensorState CreateSensorState(
        string id = "sensor",
        int cooldownTicks = 5,
        int lastScanTick = 0)
    {
        return new SensorState(
            CreateDefinition(id, cooldownTicks),
            new SimTick(lastScanTick));
    }

    private static TankSensorLoadout CreateLoadout()
    {
        return new TankSensorLoadout(new[]
        {
            CreateSensorState("basic"),
            CreateSensorState("wide"),
        });
    }

    [Fact]
    public void Constructor_PreservesSensorsInOrder()
    {
        SensorState first = CreateSensorState("first");
        SensorState second = CreateSensorState("second");

        TankSensorLoadout loadout = new TankSensorLoadout(new[] { first, second });

        Assert.Equal(2, loadout.Sensors.Count);
        Assert.Equal(first, loadout.Sensors[0]);
        Assert.Equal(second, loadout.Sensors[1]);
    }

    [Fact]
    public void Constructor_RejectsNullSensors()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankSensorLoadout(null!));
    }

    [Fact]
    public void Constructor_RejectsEmptySensors()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new TankSensorLoadout(Array.Empty<SensorState>()));

        Assert.Equal("sensors", ex.ParamName);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesInputArray()
    {
        SensorState first = CreateSensorState("first");
        SensorState second = CreateSensorState("second");
        SensorState[] input = { first, second };

        TankSensorLoadout loadout = new TankSensorLoadout(input);

        input[1] = CreateSensorState("mutated");

        Assert.Equal(second, loadout.Sensors[1]);
    }

    [Fact]
    public void SensorsCollection_IsReadOnly()
    {
        TankSensorLoadout loadout = CreateLoadout();

        IList<SensorState> list = (IList<SensorState>)loadout.Sensors;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => list.Add(CreateSensorState("extra")));
    }

    [Fact]
    public void Count_ReturnsSensorCount()
    {
        TankSensorLoadout loadout = CreateLoadout();

        Assert.Equal(2, loadout.Count);
    }

    [Fact]
    public void GetSensor_ReturnsSensorAtSlotZero()
    {
        TankSensorLoadout loadout = CreateLoadout();

        Assert.Equal(CreateSensorState("basic"), loadout.GetSensor(SensorSlot.Zero));
    }

    [Fact]
    public void GetSensor_ReturnsSensorAtLaterSlot()
    {
        TankSensorLoadout loadout = CreateLoadout();

        Assert.Equal(CreateSensorState("wide"), loadout.GetSensor(new SensorSlot(1)));
    }

    [Fact]
    public void GetSensor_RejectsSlotPastEnd()
    {
        TankSensorLoadout loadout = CreateLoadout();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => loadout.GetSensor(new SensorSlot(2)));

        Assert.Equal("slot", ex.ParamName);
    }

    [Fact]
    public void WithSensor_ReplacesSensorAtSlot()
    {
        TankSensorLoadout loadout = CreateLoadout();
        SensorState replacement = CreateSensorState("replacement");

        TankSensorLoadout updated = loadout.WithSensor(new SensorSlot(1), replacement);

        Assert.Equal(replacement, updated.Sensors[1]);
    }

    [Fact]
    public void WithSensor_PreservesOtherSlots()
    {
        TankSensorLoadout loadout = CreateLoadout();
        SensorState replacement = CreateSensorState("replacement");

        TankSensorLoadout updated = loadout.WithSensor(new SensorSlot(1), replacement);

        Assert.Equal(CreateSensorState("basic"), updated.Sensors[0]);
    }

    [Fact]
    public void WithSensor_PreservesSensorOrder()
    {
        SensorState first = CreateSensorState("first");
        SensorState second = CreateSensorState("second");
        SensorState third = CreateSensorState("third");
        TankSensorLoadout loadout = new TankSensorLoadout(new[] { first, second, third });
        SensorState replacement = CreateSensorState("replacement");

        TankSensorLoadout updated = loadout.WithSensor(new SensorSlot(1), replacement);

        Assert.Equal(first, updated.Sensors[0]);
        Assert.Equal(replacement, updated.Sensors[1]);
        Assert.Equal(third, updated.Sensors[2]);
    }

    [Fact]
    public void WithSensor_DoesNotMutateOriginalLoadout()
    {
        TankSensorLoadout original = CreateLoadout();
        SensorState replacement = CreateSensorState("replacement");

        TankSensorLoadout updated = original.WithSensor(new SensorSlot(1), replacement);

        Assert.Equal(CreateSensorState("wide"), original.Sensors[1]);
        Assert.Equal(replacement, updated.Sensors[1]);
    }

    [Fact]
    public void WithSensor_RejectsSlotPastEnd()
    {
        TankSensorLoadout loadout = CreateLoadout();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => loadout.WithSensor(
                new SensorSlot(2),
                CreateSensorState("replacement")));

        Assert.Equal("slot", ex.ParamName);
    }

    [Fact]
    public void TwoLoadouts_WithSameSensorStates_AreNotValueEqual()
    {
        TankSensorLoadout a = CreateLoadout();
        TankSensorLoadout b = CreateLoadout();

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void SameReference_EqualsItself()
    {
        TankSensorLoadout loadout = CreateLoadout();

        Assert.True(loadout.Equals(loadout));
    }

    [Fact]
    public void UpdatedLoadout_SensorsCollection_IsReadOnly()
    {
        TankSensorLoadout original = CreateLoadout();
        TankSensorLoadout updated = original.WithSensor(
            new SensorSlot(0),
            CreateSensorState("replacement"));

        IList<SensorState> list = (IList<SensorState>)updated.Sensors;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => list.Add(CreateSensorState("extra")));
    }
}
