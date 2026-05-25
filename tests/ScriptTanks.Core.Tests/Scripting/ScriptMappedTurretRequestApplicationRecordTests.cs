using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedTurretRequestApplicationRecordTests
{
    private static Fixed SampleRotation() => Fixed.FromRatio(1, 4);

    private static ScriptCommandTranslationOutput TurretTranslationOutput()
    {
        ScriptCommand command = new ScriptCommand(
            ScriptCommandType.AimAtEnemy,
            argument: "nearest_visible");

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "Aim at enemy command translated.");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.AimAtEnemy,
                routineIndex: 0,
                command,
                payload: "nearest_visible");

        return new ScriptCommandTranslationOutput(result, request);
    }

    private static ScriptCommandTranslationOutput SensorTranslationOutput()
    {
        ScriptCommand command = new ScriptCommand(
            ScriptCommandType.ScanEnemy,
            argument: "default");

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "Scan enemy command translated.");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.ScanEnemy,
                routineIndex: 0,
                command,
                payload: "default");

        return new ScriptCommandTranslationOutput(result, request);
    }

    private static ScriptDomainRequestMappingRecord CreateTurretMappingRecord()
    {
        return ScriptDomainRequestMappingRecord.TurretPlaceholder(
            tankIndex: 0,
            tankId: new TankId(0),
            TurretTranslationOutput());
    }

    private static ScriptDomainRequestMappingRecord CreateSensorMappingRecord()
    {
        return ScriptDomainRequestMappingRecord.Sensor(
            tankIndex: 0,
            tankId: new TankId(0),
            SensorTranslationOutput(),
            sensorRequest: new MatchSensorScanRequest(0, SensorSlot.Zero));
    }

    [Fact]
    public void Constructor_preserves_values_when_applied()
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();
        Fixed rotation = SampleRotation();

        var record = new ScriptMappedTurretRequestApplicationRecord(
            2,
            mapping,
            ScriptMappedTurretRequestApplicationStatus.Applied,
            rotation);

        Assert.Equal(2, record.RecordIndex);
        Assert.Same(mapping, record.MappingRecord);
        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.Applied, record.Status);
        Assert.Equal(rotation, record.AppliedTurretRotation);
        Assert.True(record.DidApply);
    }

    [Fact]
    public void Constructor_rejects_negative_recordIndex_ParamName_recordIndex()
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedTurretRequestApplicationRecord(
                -1,
                mapping,
                ScriptMappedTurretRequestApplicationStatus.Applied,
                SampleRotation()));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_mappingRecord_ParamName_mappingRecord()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedTurretRequestApplicationRecord(
                0,
                null!,
                ScriptMappedTurretRequestApplicationStatus.Applied,
                SampleRotation()));

        Assert.Equal("mappingRecord", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedTurretRequestApplicationRecord(
                0,
                mapping,
                (ScriptMappedTurretRequestApplicationStatus)99,
                SampleRotation()));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_applied_requires_rotation_ParamName_appliedTurretRotation()
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedTurretRequestApplicationRecord(
                0,
                mapping,
                ScriptMappedTurretRequestApplicationStatus.Applied,
                appliedTurretRotation: null));

        Assert.Equal("appliedTurretRotation", ex.ParamName);
    }

    [Theory]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.TankDestroyed)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.NoTarget)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.MissingAimSolution)]
    public void Constructor_non_applied_statuses_reject_rotation_ParamName_appliedTurretRotation(
        ScriptMappedTurretRequestApplicationStatus status)
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedTurretRequestApplicationRecord(
                0,
                mapping,
                status,
                SampleRotation()));

        Assert.Equal("appliedTurretRotation", ex.ParamName);
    }

    [Theory]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.Applied)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.TankDestroyed)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.NoTarget)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.MissingAimSolution)]
    public void Constructor_non_skip_statuses_require_turret_mapping_ParamName_mappingRecord(
        ScriptMappedTurretRequestApplicationStatus status)
    {
        ScriptDomainRequestMappingRecord mapping = CreateSensorMappingRecord();
        Fixed? rotation = status == ScriptMappedTurretRequestApplicationStatus.Applied
            ? SampleRotation()
            : null;

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedTurretRequestApplicationRecord(
                0,
                mapping,
                status,
                rotation));

        Assert.Equal("mappingRecord", ex.ParamName);
    }

    [Fact]
    public void Constructor_skipped_not_turret_allows_non_turret_mapping()
    {
        ScriptDomainRequestMappingRecord mapping = CreateSensorMappingRecord();

        ScriptMappedTurretRequestApplicationRecord record =
            ScriptMappedTurretRequestApplicationRecord.SkippedNotTurret(0, mapping);

        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret, record.Status);
        Assert.Null(record.AppliedTurretRotation);
        Assert.False(record.DidApply);
    }

    [Fact]
    public void Applied_factory_sets_DidApply_true()
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ScriptMappedTurretRequestApplicationRecord record =
            ScriptMappedTurretRequestApplicationRecord.Applied(0, mapping, SampleRotation());

        Assert.True(record.DidApply);
        Assert.Equal(SampleRotation(), record.AppliedTurretRotation);
    }

    [Theory]
    [InlineData(nameof(ScriptMappedTurretRequestApplicationRecord.RejectedInvalidTurretRequest))]
    [InlineData(nameof(ScriptMappedTurretRequestApplicationRecord.TankIndexOutOfRange))]
    [InlineData(nameof(ScriptMappedTurretRequestApplicationRecord.TankDestroyed))]
    [InlineData(nameof(ScriptMappedTurretRequestApplicationRecord.NoTarget))]
    [InlineData(nameof(ScriptMappedTurretRequestApplicationRecord.MissingAimSolution))]
    public void Failure_factories_set_DidApply_false(string factoryName)
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ScriptMappedTurretRequestApplicationRecord record = factoryName switch
        {
            nameof(ScriptMappedTurretRequestApplicationRecord.RejectedInvalidTurretRequest) =>
                ScriptMappedTurretRequestApplicationRecord.RejectedInvalidTurretRequest(0, mapping),
            nameof(ScriptMappedTurretRequestApplicationRecord.TankIndexOutOfRange) =>
                ScriptMappedTurretRequestApplicationRecord.TankIndexOutOfRange(0, mapping),
            nameof(ScriptMappedTurretRequestApplicationRecord.TankDestroyed) =>
                ScriptMappedTurretRequestApplicationRecord.TankDestroyed(0, mapping),
            nameof(ScriptMappedTurretRequestApplicationRecord.NoTarget) =>
                ScriptMappedTurretRequestApplicationRecord.NoTarget(0, mapping),
            nameof(ScriptMappedTurretRequestApplicationRecord.MissingAimSolution) =>
                ScriptMappedTurretRequestApplicationRecord.MissingAimSolution(0, mapping),
            _ => throw new InvalidOperationException("Unexpected factory name."),
        };

        Assert.False(record.DidApply);
        Assert.Null(record.AppliedTurretRotation);
    }

    [Fact]
    public void Record_does_not_override_equality()
    {
        ScriptDomainRequestMappingRecord mapping = CreateTurretMappingRecord();

        ScriptMappedTurretRequestApplicationRecord a =
            ScriptMappedTurretRequestApplicationRecord.Applied(0, mapping, SampleRotation());

        ScriptMappedTurretRequestApplicationRecord b =
            ScriptMappedTurretRequestApplicationRecord.Applied(0, mapping, SampleRotation());

        Assert.False(ReferenceEquals(a, b));
        Assert.NotSame(a, b);
    }
}
