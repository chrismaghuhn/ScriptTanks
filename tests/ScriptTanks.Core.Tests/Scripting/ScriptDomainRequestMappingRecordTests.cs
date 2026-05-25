using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptDomainRequestMappingRecordTests
{
    private static ScriptCommandTranslationOutput SampleOutput()
    {
        return new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.NoIntent(),
            ScriptTranslatedCommandRequest.None());
    }

    private static MatchSensorScanRequest SampleSensorScan()
        => new MatchSensorScanRequest(0, SensorSlot.Zero);

    private static MatchFireRequest SampleFireRequest()
        => new MatchFireRequest(
            new TankId(1),
            new WeaponSlot(0),
            new ProjectileId(10),
            FixedVec2.FromInts(1, 2),
            FixedVec2.Zero);

    [Fact]
    public void Constructor_preserves_values_when_valid()
    {
        ScriptCommandTranslationOutput output = SampleOutput();
        TankId tankId = new TankId(3);

        var record = new ScriptDomainRequestMappingRecord(
            tankIndex: 2,
            tankId,
            output,
            ScriptDomainRequestCategory.Sensor,
            SampleSensorScan(),
            fireRequest: null);

        Assert.Equal(2, record.TankIndex);
        Assert.Equal(tankId, record.TankId);
        Assert.Same(output, record.TranslationOutput);
        Assert.Equal(ScriptDomainRequestCategory.Sensor, record.Category);
        Assert.Equal(SampleSensorScan(), record.SensorRequest);
        Assert.Null(record.FireRequest);
        Assert.True(record.HasConcreteRequest);
    }

    [Fact]
    public void Constructor_rejects_negative_tankIndex_ParamName_tankIndex()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptDomainRequestMappingRecord(
                -1,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.None,
                sensorRequest: null,
                fireRequest: null));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_translationOutput_ParamName_translationOutput()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                translationOutput: null!,
                ScriptDomainRequestCategory.None,
                sensorRequest: null,
                fireRequest: null));

        Assert.Equal("translationOutput", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_undefined_category_ParamName_category()
    {
        var bad = (ScriptDomainRequestCategory)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                bad,
                sensorRequest: null,
                fireRequest: null));

        Assert.Equal("category", ex.ParamName);
    }

    [Fact]
    public void Category_None_rejects_sensor_request_ParamName_sensorRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.None,
                SampleSensorScan(),
                fireRequest: null));

        Assert.Equal("sensorRequest", ex.ParamName);
    }

    [Fact]
    public void Category_None_rejects_fire_request_ParamName_fireRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.None,
                sensorRequest: null,
                SampleFireRequest()));

        Assert.Equal("fireRequest", ex.ParamName);
    }

    [Fact]
    public void Category_Unsupported_rejects_concrete_requests()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.Unsupported,
                SampleSensorScan(),
                fireRequest: null));
    }

    [Fact]
    public void Category_Turret_rejects_sensor_request()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.Turret,
                SampleSensorScan(),
                fireRequest: null));
    }

    [Fact]
    public void Category_Movement_rejects_fire_request_ParamName_fireRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.Movement,
                sensorRequest: null,
                SampleFireRequest()));

        Assert.Equal("fireRequest", ex.ParamName);
    }

    [Fact]
    public void Category_Sensor_allows_null_sensor_request()
    {
        var record = new ScriptDomainRequestMappingRecord(
            0,
            new TankId(0),
            SampleOutput(),
            ScriptDomainRequestCategory.Sensor,
            sensorRequest: null,
            fireRequest: null);

        Assert.False(record.HasConcreteRequest);
        Assert.Null(record.SensorRequest);
    }

    [Fact]
    public void Category_Sensor_allows_sensor_request()
    {
        MatchSensorScanRequest scan = SampleSensorScan();

        var record = new ScriptDomainRequestMappingRecord(
            0,
            new TankId(0),
            SampleOutput(),
            ScriptDomainRequestCategory.Sensor,
            scan,
            fireRequest: null);

        Assert.Equal(scan, record.SensorRequest);
        Assert.True(record.HasConcreteRequest);
    }

    [Fact]
    public void Category_Sensor_rejects_fire_request_ParamName_fireRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.Sensor,
                sensorRequest: null,
                SampleFireRequest()));

        Assert.Equal("fireRequest", ex.ParamName);
    }

    [Fact]
    public void Category_Weapon_allows_null_fire_request()
    {
        var record = new ScriptDomainRequestMappingRecord(
            0,
            new TankId(0),
            SampleOutput(),
            ScriptDomainRequestCategory.Weapon,
            sensorRequest: null,
            fireRequest: null);

        Assert.False(record.HasConcreteRequest);
    }

    [Fact]
    public void Category_Weapon_allows_fire_request()
    {
        MatchFireRequest fire = SampleFireRequest();

        var record = new ScriptDomainRequestMappingRecord(
            0,
            new TankId(0),
            SampleOutput(),
            ScriptDomainRequestCategory.Weapon,
            sensorRequest: null,
            fire);

        Assert.Equal(fire, record.FireRequest);
        Assert.True(record.HasConcreteRequest);
    }

    [Fact]
    public void Category_Weapon_rejects_sensor_request_ParamName_sensorRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.Weapon,
                SampleSensorScan(),
                fireRequest: null));

        Assert.Equal("sensorRequest", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_both_concrete_requests_non_null_ParamName_fireRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptDomainRequestMappingRecord(
                0,
                new TankId(0),
                SampleOutput(),
                ScriptDomainRequestCategory.Sensor,
                SampleSensorScan(),
                SampleFireRequest()));

        Assert.Equal("fireRequest", ex.ParamName);
    }

    [Fact]
    public void HasConcreteRequest_false_when_both_optional_null()
    {
        var record = ScriptDomainRequestMappingRecord.Sensor(
            0,
            new TankId(0),
            SampleOutput(),
            sensorRequest: null);

        Assert.False(record.HasConcreteRequest);
    }

    [Fact]
    public void HasConcreteRequest_true_when_sensor_set()
    {
        var record = ScriptDomainRequestMappingRecord.Sensor(
            0,
            new TankId(0),
            SampleOutput(),
            SampleSensorScan());

        Assert.True(record.HasConcreteRequest);
    }

    [Fact]
    public void HasConcreteRequest_true_when_fire_set()
    {
        var record = ScriptDomainRequestMappingRecord.Weapon(
            0,
            new TankId(0),
            SampleOutput(),
            SampleFireRequest());

        Assert.True(record.HasConcreteRequest);
    }

    [Fact]
    public void Factory_None_matches_constructor_None()
    {
        ScriptCommandTranslationOutput output = SampleOutput();

        var factory = ScriptDomainRequestMappingRecord.None(4, new TankId(9), output);
        var direct = new ScriptDomainRequestMappingRecord(
            4,
            new TankId(9),
            output,
            ScriptDomainRequestCategory.None,
            null,
            null);

        Assert.Equal(direct.Category, factory.Category);
        Assert.False(factory.HasConcreteRequest);
    }

    [Fact]
    public void Factory_Unsupported_matches_constructor()
    {
        ScriptCommandTranslationOutput output = SampleOutput();

        var factory = ScriptDomainRequestMappingRecord.Unsupported(0, new TankId(1), output);

        Assert.Equal(ScriptDomainRequestCategory.Unsupported, factory.Category);
        Assert.False(factory.HasConcreteRequest);
    }

    [Fact]
    public void Factory_TurretPlaceholder_sets_Turret_without_concrete_requests()
    {
        var factory = ScriptDomainRequestMappingRecord.TurretPlaceholder(
            0,
            new TankId(0),
            SampleOutput());

        Assert.Equal(ScriptDomainRequestCategory.Turret, factory.Category);
        Assert.False(factory.HasConcreteRequest);
    }

    [Fact]
    public void Factory_MovementPlaceholder_sets_Movement_without_concrete_requests()
    {
        var factory = ScriptDomainRequestMappingRecord.MovementPlaceholder(
            0,
            new TankId(0),
            SampleOutput());

        Assert.Equal(ScriptDomainRequestCategory.Movement, factory.Category);
        Assert.False(factory.HasConcreteRequest);
    }
}
