using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable outcome for applying one <see cref="ScriptDomainRequestMappingRecord"/> in a future
/// turret application pipeline implementation.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not resolve aim targets, compute turret rotation, mutate
/// <see cref="Match.MatchState"/>, construct or apply fire requests, run
/// <see cref="Match.MatchTickPipeline"/>, log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedTurretRequestApplicationRecord
{
    public int RecordIndex { get; }

    public ScriptDomainRequestMappingRecord MappingRecord { get; }

    public ScriptMappedTurretRequestApplicationStatus Status { get; }

    public Fixed? AppliedTurretRotation { get; }

    public bool DidApply =>
        Status == ScriptMappedTurretRequestApplicationStatus.Applied
        && AppliedTurretRotation.HasValue;

    public ScriptMappedTurretRequestApplicationRecord(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        ScriptMappedTurretRequestApplicationStatus status,
        Fixed? appliedTurretRotation)
    {
        if (recordIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(mappingRecord);

        if (!Enum.IsDefined(typeof(ScriptMappedTurretRequestApplicationStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Turret request application status must be a defined enum value.");
        }

        ValidateInvariants(status, mappingRecord, appliedTurretRotation);

        RecordIndex = recordIndex;
        MappingRecord = mappingRecord;
        Status = status;
        AppliedTurretRotation = appliedTurretRotation;
    }

    public static ScriptMappedTurretRequestApplicationRecord SkippedNotTurret(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret,
            appliedTurretRotation: null);
    }

    public static ScriptMappedTurretRequestApplicationRecord Applied(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        Fixed appliedTurretRotation)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.Applied,
            appliedTurretRotation);
    }

    public static ScriptMappedTurretRequestApplicationRecord RejectedInvalidTurretRequest(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest,
            appliedTurretRotation: null);
    }

    public static ScriptMappedTurretRequestApplicationRecord TankIndexOutOfRange(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange,
            appliedTurretRotation: null);
    }

    public static ScriptMappedTurretRequestApplicationRecord TankDestroyed(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.TankDestroyed,
            appliedTurretRotation: null);
    }

    public static ScriptMappedTurretRequestApplicationRecord NoTarget(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.NoTarget,
            appliedTurretRotation: null);
    }

    public static ScriptMappedTurretRequestApplicationRecord MissingAimSolution(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedTurretRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedTurretRequestApplicationStatus.MissingAimSolution,
            appliedTurretRotation: null);
    }

    private static void ValidateInvariants(
        ScriptMappedTurretRequestApplicationStatus status,
        ScriptDomainRequestMappingRecord mappingRecord,
        Fixed? appliedTurretRotation)
    {
        switch (status)
        {
            case ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret:
                if (appliedTurretRotation is not null)
                {
                    throw new ArgumentException(
                        "Skipped application must not carry an applied turret rotation.",
                        nameof(appliedTurretRotation));
                }

                break;

            case ScriptMappedTurretRequestApplicationStatus.Applied:
                if (appliedTurretRotation is null)
                {
                    throw new ArgumentException(
                        "Applied turret outcome requires a non-null applied turret rotation.",
                        nameof(appliedTurretRotation));
                }

                RequireTurretMapping(mappingRecord);
                break;

            case ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest:
            case ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange:
            case ScriptMappedTurretRequestApplicationStatus.TankDestroyed:
            case ScriptMappedTurretRequestApplicationStatus.NoTarget:
            case ScriptMappedTurretRequestApplicationStatus.MissingAimSolution:
                if (appliedTurretRotation is not null)
                {
                    throw new ArgumentException(
                        "Non-applied turret outcomes must not carry an applied turret rotation.",
                        nameof(appliedTurretRotation));
                }

                RequireTurretMapping(mappingRecord);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Turret request application status must be a defined enum value.");
        }
    }

    private static void RequireTurretMapping(ScriptDomainRequestMappingRecord mappingRecord)
    {
        if (mappingRecord.Category != ScriptDomainRequestCategory.Turret)
        {
            throw new ArgumentException(
                "Non-skipped turret application records require a turret mapping record.",
                nameof(mappingRecord));
        }
    }
}
