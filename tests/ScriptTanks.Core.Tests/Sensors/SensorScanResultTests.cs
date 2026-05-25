using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class SensorScanResultTests
{
    private static SensorDefinition CreateSensor()
    {
        return SensorCatalog.BasicRadar;
    }

    private static DetectedTankSnapshot CreateDetectedTank(
        int tankId = 1,
        int ownerSlot = 1,
        Fixed? distance = null)
    {
        return new DetectedTankSnapshot(
            new TankId(tankId),
            new PlayerSlot(ownerSlot),
            FixedVec2.FromInts(10 + tankId, 20),
            distance ?? Fixed.FromInt(15));
    }

    [Fact]
    public void Constructor_PreservesValues()
    {
        DetectedTankSnapshot detected = CreateDetectedTank();

        SensorScanResult result = new SensorScanResult(
            SensorScanStatus.Detected,
            new SimTick(7),
            CreateSensor(),
            new TankId(0),
            new[] { detected });

        Assert.Equal(SensorScanStatus.Detected, result.Status);
        Assert.Equal(new SimTick(7), result.Tick);
        Assert.Equal(CreateSensor(), result.Sensor);
        Assert.Equal(new TankId(0), result.ScannerTankId);
        DetectedTankSnapshot actual = Assert.Single(result.DetectedTanks);
        Assert.Equal(detected, actual);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesDetections()
    {
        DetectedTankSnapshot first = CreateDetectedTank(tankId: 1);
        DetectedTankSnapshot second = CreateDetectedTank(tankId: 2);

        DetectedTankSnapshot[] input = { first, second };

        SensorScanResult result = SensorScanResult.Detected(
            new SimTick(5),
            CreateSensor(),
            new TankId(0),
            input);

        input[1] = CreateDetectedTank(tankId: 99);

        Assert.Equal(second, result.DetectedTanks[1]);
    }

    [Fact]
    public void Constructor_DetectedTanksCollectionIsReadOnly()
    {
        SensorScanResult result = SensorScanResult.Detected(
            new SimTick(5),
            CreateSensor(),
            new TankId(0),
            new[] { CreateDetectedTank() });

        IList<DetectedTankSnapshot> list =
            (IList<DetectedTankSnapshot>)result.DetectedTanks;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => list.Add(CreateDetectedTank(tankId: 99)));
    }

    [Fact]
    public void Constructor_RejectsNullDetectedTanks()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SensorScanResult(
                SensorScanStatus.NoDetection,
                new SimTick(5),
                CreateSensor(),
                new TankId(0),
                null!));
    }

    [Fact]
    public void Constructor_RejectsStatusNone()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new SensorScanResult(
                SensorScanStatus.None,
                new SimTick(5),
                CreateSensor(),
                new TankId(0),
                Array.Empty<DetectedTankSnapshot>()));
        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsDetectedStatusWithoutDetections()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new SensorScanResult(
                SensorScanStatus.Detected,
                new SimTick(5),
                CreateSensor(),
                new TankId(0),
                Array.Empty<DetectedTankSnapshot>()));
        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNoDetectionWithDetections()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new SensorScanResult(
                SensorScanStatus.NoDetection,
                new SimTick(5),
                CreateSensor(),
                new TankId(0),
                new[] { CreateDetectedTank() }));
        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsOnCooldownWithDetections()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new SensorScanResult(
                SensorScanStatus.SensorOnCooldown,
                new SimTick(5),
                CreateSensor(),
                new TankId(0),
                new[] { CreateDetectedTank() }));
        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void NoDetectionFactory_CreatesExpectedResult()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(5),
            CreateSensor(),
            new TankId(0));

        Assert.Equal(SensorScanStatus.NoDetection, result.Status);
        Assert.Empty(result.DetectedTanks);
        Assert.False(result.HasDetections);
    }

    [Fact]
    public void OnCooldownFactory_CreatesExpectedResult()
    {
        SensorScanResult result = SensorScanResult.OnCooldown(
            new SimTick(5),
            CreateSensor(),
            new TankId(0));

        Assert.Equal(SensorScanStatus.SensorOnCooldown, result.Status);
        Assert.Empty(result.DetectedTanks);
        Assert.False(result.HasDetections);
    }

    [Fact]
    public void DetectedFactory_CreatesExpectedResult()
    {
        DetectedTankSnapshot detected = CreateDetectedTank();

        SensorScanResult result = SensorScanResult.Detected(
            new SimTick(5),
            CreateSensor(),
            new TankId(0),
            new[] { detected });

        Assert.Equal(SensorScanStatus.Detected, result.Status);
        DetectedTankSnapshot actual = Assert.Single(result.DetectedTanks);
        Assert.Equal(detected, actual);
    }

    [Fact]
    public void HasDetections_ReturnsTrue_WhenDetectionsExist()
    {
        SensorScanResult result = SensorScanResult.Detected(
            new SimTick(5),
            CreateSensor(),
            new TankId(0),
            new[] { CreateDetectedTank() });

        Assert.True(result.HasDetections);
    }

    [Fact]
    public void HasDetections_ReturnsFalse_WhenNoDetectionsExist()
    {
        SensorScanResult result = SensorScanResult.NoDetection(
            new SimTick(5),
            CreateSensor(),
            new TankId(0));

        Assert.False(result.HasDetections);
    }

    [Fact]
    public void DetectedFactory_RejectsNullDetections()
    {
        Assert.Throws<ArgumentNullException>(
            () => SensorScanResult.Detected(
                new SimTick(5),
                CreateSensor(),
                new TankId(0),
                null!));
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        SensorScanResult a = SensorScanResult.NoDetection(
            new SimTick(5),
            CreateSensor(),
            new TankId(0));

        SensorScanResult b = SensorScanResult.NoDetection(
            new SimTick(5),
            CreateSensor(),
            new TankId(0));

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Constructor_PreservesDetectionOrder()
    {
        DetectedTankSnapshot first = CreateDetectedTank(tankId: 1);
        DetectedTankSnapshot second = CreateDetectedTank(tankId: 2);

        SensorScanResult result = SensorScanResult.Detected(
            new SimTick(5),
            CreateSensor(),
            new TankId(0),
            new[] { first, second });

        Assert.Equal(first, result.DetectedTanks[0]);
        Assert.Equal(second, result.DetectedTanks[1]);
    }
}
