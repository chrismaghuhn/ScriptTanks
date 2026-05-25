using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedMovementRequestApplicationRecordTests
{
    private static FixedVec2 SampleVelocity() => FixedVec2.FromInts(1, 0);

    private static ScriptCommandTranslationOutput MovementTranslationOutput()
    {
        ScriptCommand command = new ScriptCommand(
            ScriptCommandType.MoveToPatrolPoint,
            argument: "next");

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "Move to patrol point command translated.");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.MoveToPatrolPoint,
                routineIndex: 0,
                command,
                payload: "next");

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

    private static ScriptDomainRequestMappingRecord CreateMovementMappingRecord()
    {
        return ScriptDomainRequestMappingRecord.MovementPlaceholder(
            tankIndex: 0,
            tankId: new TankId(0),
            MovementTranslationOutput());
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
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();
        FixedVec2 velocity = SampleVelocity();

        var record = new ScriptMappedMovementRequestApplicationRecord(
            2,
            mapping,
            ScriptMappedMovementRequestApplicationStatus.Applied,
            velocity);

        Assert.Equal(2, record.RecordIndex);
        Assert.Same(mapping, record.MappingRecord);
        Assert.Equal(ScriptMappedMovementRequestApplicationStatus.Applied, record.Status);
        Assert.Equal(velocity, record.AppliedVelocity);
        Assert.True(record.DidApply);
    }

    [Fact]
    public void Constructor_rejects_negative_recordIndex_ParamName_recordIndex()
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedMovementRequestApplicationRecord(
                -1,
                mapping,
                ScriptMappedMovementRequestApplicationStatus.Applied,
                SampleVelocity()));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_mappingRecord_ParamName_mappingRecord()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedMovementRequestApplicationRecord(
                0,
                null!,
                ScriptMappedMovementRequestApplicationStatus.Applied,
                SampleVelocity()));

        Assert.Equal("mappingRecord", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedMovementRequestApplicationRecord(
                0,
                mapping,
                (ScriptMappedMovementRequestApplicationStatus)99,
                SampleVelocity()));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_applied_requires_velocity_ParamName_appliedVelocity()
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedMovementRequestApplicationRecord(
                0,
                mapping,
                ScriptMappedMovementRequestApplicationStatus.Applied,
                appliedVelocity: null));

        Assert.Equal("appliedVelocity", ex.ParamName);
    }

    [Theory]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.TankDestroyed)]
    public void Constructor_non_applied_statuses_reject_velocity_ParamName_appliedVelocity(
        ScriptMappedMovementRequestApplicationStatus status)
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedMovementRequestApplicationRecord(
                0,
                mapping,
                status,
                SampleVelocity()));

        Assert.Equal("appliedVelocity", ex.ParamName);
    }

    [Theory]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.Applied)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.TankDestroyed)]
    public void Constructor_non_skip_statuses_require_movement_mapping_ParamName_mappingRecord(
        ScriptMappedMovementRequestApplicationStatus status)
    {
        ScriptDomainRequestMappingRecord mapping = CreateSensorMappingRecord();
        FixedVec2? velocity = status == ScriptMappedMovementRequestApplicationStatus.Applied
            ? SampleVelocity()
            : null;

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedMovementRequestApplicationRecord(
                0,
                mapping,
                status,
                velocity));

        Assert.Equal("mappingRecord", ex.ParamName);
    }

    [Fact]
    public void Constructor_skipped_not_movement_allows_non_movement_mapping()
    {
        ScriptDomainRequestMappingRecord mapping = CreateSensorMappingRecord();

        ScriptMappedMovementRequestApplicationRecord record =
            ScriptMappedMovementRequestApplicationRecord.SkippedNotMovement(0, mapping);

        Assert.Equal(ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement, record.Status);
        Assert.Null(record.AppliedVelocity);
        Assert.False(record.DidApply);
    }

    [Fact]
    public void Applied_factory_sets_DidApply_true()
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ScriptMappedMovementRequestApplicationRecord record =
            ScriptMappedMovementRequestApplicationRecord.Applied(0, mapping, SampleVelocity());

        Assert.True(record.DidApply);
        Assert.Equal(SampleVelocity(), record.AppliedVelocity);
    }

    [Theory]
    [InlineData(nameof(ScriptMappedMovementRequestApplicationRecord.RejectedInvalidMovement))]
    [InlineData(nameof(ScriptMappedMovementRequestApplicationRecord.TankIndexOutOfRange))]
    [InlineData(nameof(ScriptMappedMovementRequestApplicationRecord.TankDestroyed))]
    public void Failure_factories_set_DidApply_false(string factoryName)
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ScriptMappedMovementRequestApplicationRecord record = factoryName switch
        {
            nameof(ScriptMappedMovementRequestApplicationRecord.RejectedInvalidMovement) =>
                ScriptMappedMovementRequestApplicationRecord.RejectedInvalidMovement(0, mapping),
            nameof(ScriptMappedMovementRequestApplicationRecord.TankIndexOutOfRange) =>
                ScriptMappedMovementRequestApplicationRecord.TankIndexOutOfRange(0, mapping),
            nameof(ScriptMappedMovementRequestApplicationRecord.TankDestroyed) =>
                ScriptMappedMovementRequestApplicationRecord.TankDestroyed(0, mapping),
            _ => throw new InvalidOperationException("Unexpected factory name."),
        };

        Assert.False(record.DidApply);
        Assert.Null(record.AppliedVelocity);
    }

    [Fact]
    public void Record_does_not_override_equality()
    {
        ScriptDomainRequestMappingRecord mapping = CreateMovementMappingRecord();

        ScriptMappedMovementRequestApplicationRecord a =
            ScriptMappedMovementRequestApplicationRecord.Applied(0, mapping, SampleVelocity());

        ScriptMappedMovementRequestApplicationRecord b =
            ScriptMappedMovementRequestApplicationRecord.Applied(0, mapping, SampleVelocity());

        Assert.False(ReferenceEquals(a, b));
        Assert.NotSame(a, b);
    }
}
