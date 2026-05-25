using System;
using System.Collections.Generic;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class MatchSensorLoadoutStateTests
{
    private static TankSensorLoadout CreateLoadout(SensorDefinition sensor)
    {
        return new TankSensorLoadout(new[] { SensorState.Ready(sensor) });
    }

    private static MatchSensorLoadoutState CreateState()
    {
        return new MatchSensorLoadoutState(new[]
        {
            CreateLoadout(SensorCatalog.BasicRadar),
            CreateLoadout(SensorCatalog.WideScanner),
        });
    }

    [Fact]
    public void Constructor_PreservesLoadoutsInOrder()
    {
        TankSensorLoadout first = CreateLoadout(SensorCatalog.BasicRadar);
        TankSensorLoadout second = CreateLoadout(SensorCatalog.WideScanner);

        MatchSensorLoadoutState state = new MatchSensorLoadoutState(
            new[] { first, second });

        Assert.Equal(2, state.SensorLoadouts.Count);
        Assert.Same(first, state.SensorLoadouts[0]);
        Assert.Same(second, state.SensorLoadouts[1]);
    }

    [Fact]
    public void Constructor_RejectsNullSensorLoadouts()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new MatchSensorLoadoutState(null!));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptySensorLoadouts()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorLoadoutState(Array.Empty<TankSensorLoadout>()));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullLoadoutElement()
    {
        TankSensorLoadout[] input =
        {
            CreateLoadout(SensorCatalog.BasicRadar),
            null!,
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchSensorLoadoutState(input));

        Assert.Equal("sensorLoadouts", ex.ParamName);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesInputArray()
    {
        TankSensorLoadout first = CreateLoadout(SensorCatalog.BasicRadar);
        TankSensorLoadout second = CreateLoadout(SensorCatalog.WideScanner);
        TankSensorLoadout[] input = { first, second };

        MatchSensorLoadoutState state = new MatchSensorLoadoutState(input);
        input[1] = CreateLoadout(SensorCatalog.BasicRadar);

        Assert.Same(second, state.SensorLoadouts[1]);
    }

    [Fact]
    public void SensorLoadoutsCollection_IsReadOnly()
    {
        MatchSensorLoadoutState state = CreateState();

        IList<TankSensorLoadout> list = (IList<TankSensorLoadout>)state.SensorLoadouts;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => list.Add(CreateLoadout(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void Count_ReturnsLoadoutCount()
    {
        MatchSensorLoadoutState state = CreateState();

        Assert.Equal(2, state.Count);
    }

    [Fact]
    public void GetLoadoutAtIndex_ReturnsLoadoutAtIndexZero()
    {
        TankSensorLoadout first = CreateLoadout(SensorCatalog.BasicRadar);
        TankSensorLoadout second = CreateLoadout(SensorCatalog.WideScanner);
        MatchSensorLoadoutState state = new MatchSensorLoadoutState(
            new[] { first, second });

        Assert.Same(first, state.GetLoadoutAtIndex(0));
    }

    [Fact]
    public void GetLoadoutAtIndex_ReturnsLoadoutAtLaterIndex()
    {
        TankSensorLoadout first = CreateLoadout(SensorCatalog.BasicRadar);
        TankSensorLoadout second = CreateLoadout(SensorCatalog.WideScanner);
        MatchSensorLoadoutState state = new MatchSensorLoadoutState(
            new[] { first, second });

        Assert.Same(second, state.GetLoadoutAtIndex(1));
    }

    [Fact]
    public void GetLoadoutAtIndex_RejectsNegativeIndex()
    {
        MatchSensorLoadoutState state = CreateState();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => state.GetLoadoutAtIndex(-1));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void GetLoadoutAtIndex_RejectsPastEndIndex()
    {
        MatchSensorLoadoutState state = CreateState();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => state.GetLoadoutAtIndex(2));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void WithLoadoutAtIndex_RejectsNullReplacement()
    {
        MatchSensorLoadoutState state = CreateState();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => state.WithLoadoutAtIndex(0, null!));

        Assert.Equal("sensorLoadout", ex.ParamName);
    }

    [Fact]
    public void WithLoadoutAtIndex_RejectsInvalidIndex()
    {
        MatchSensorLoadoutState state = CreateState();
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => state.WithLoadoutAtIndex(2, replacement));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void WithLoadoutAtIndex_ReplacesSelectedLoadout()
    {
        MatchSensorLoadoutState state = CreateState();
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        MatchSensorLoadoutState updated = state.WithLoadoutAtIndex(1, replacement);

        Assert.Same(replacement, updated.GetLoadoutAtIndex(1));
    }

    [Fact]
    public void WithLoadoutAtIndex_PreservesOtherLoadoutsAndOrder()
    {
        TankSensorLoadout first = CreateLoadout(SensorCatalog.BasicRadar);
        TankSensorLoadout second = CreateLoadout(SensorCatalog.WideScanner);
        MatchSensorLoadoutState state = new MatchSensorLoadoutState(
            new[] { first, second });
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        MatchSensorLoadoutState updated = state.WithLoadoutAtIndex(1, replacement);

        Assert.Same(first, updated.GetLoadoutAtIndex(0));
        Assert.Equal(2, updated.Count);
    }

    [Fact]
    public void WithLoadoutAtIndex_DoesNotMutateOriginalState()
    {
        TankSensorLoadout first = CreateLoadout(SensorCatalog.BasicRadar);
        TankSensorLoadout second = CreateLoadout(SensorCatalog.WideScanner);
        MatchSensorLoadoutState original = new MatchSensorLoadoutState(
            new[] { first, second });
        TankSensorLoadout replacement = CreateLoadout(SensorCatalog.BasicRadar);

        _ = original.WithLoadoutAtIndex(1, replacement);

        Assert.Same(second, original.GetLoadoutAtIndex(1));
    }
}
