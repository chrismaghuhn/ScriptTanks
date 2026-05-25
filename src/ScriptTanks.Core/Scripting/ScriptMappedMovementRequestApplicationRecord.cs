using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable outcome for applying one <see cref="ScriptDomainRequestMappingRecord"/> in a future
/// movement application pipeline implementation.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not resolve movement commands, derive velocity, mutate
/// <see cref="Match.MatchState"/>, call <see cref="Movement.MovementIntegrator"/>,
/// run <see cref="Match.MatchTickPipeline"/>, log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedMovementRequestApplicationRecord
{
    public int RecordIndex { get; }

    public ScriptDomainRequestMappingRecord MappingRecord { get; }

    public ScriptMappedMovementRequestApplicationStatus Status { get; }

    public FixedVec2? AppliedVelocity { get; }

    public bool DidApply =>
        Status == ScriptMappedMovementRequestApplicationStatus.Applied
        && AppliedVelocity.HasValue;

    public ScriptMappedMovementRequestApplicationRecord(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        ScriptMappedMovementRequestApplicationStatus status,
        FixedVec2? appliedVelocity)
    {
        if (recordIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(mappingRecord);

        if (!Enum.IsDefined(typeof(ScriptMappedMovementRequestApplicationStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Movement request application status must be a defined enum value.");
        }

        ValidateInvariants(status, mappingRecord, appliedVelocity);

        RecordIndex = recordIndex;
        MappingRecord = mappingRecord;
        Status = status;
        AppliedVelocity = appliedVelocity;
    }

    public static ScriptMappedMovementRequestApplicationRecord SkippedNotMovement(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedMovementRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement,
            appliedVelocity: null);
    }

    public static ScriptMappedMovementRequestApplicationRecord Applied(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        FixedVec2 appliedVelocity)
    {
        return new ScriptMappedMovementRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedMovementRequestApplicationStatus.Applied,
            appliedVelocity);
    }

    public static ScriptMappedMovementRequestApplicationRecord RejectedInvalidMovement(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedMovementRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement,
            appliedVelocity: null);
    }

    public static ScriptMappedMovementRequestApplicationRecord TankIndexOutOfRange(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedMovementRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange,
            appliedVelocity: null);
    }

    public static ScriptMappedMovementRequestApplicationRecord TankDestroyed(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedMovementRequestApplicationRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedMovementRequestApplicationStatus.TankDestroyed,
            appliedVelocity: null);
    }

    private static void ValidateInvariants(
        ScriptMappedMovementRequestApplicationStatus status,
        ScriptDomainRequestMappingRecord mappingRecord,
        FixedVec2? appliedVelocity)
    {
        switch (status)
        {
            case ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement:
                if (appliedVelocity is not null)
                {
                    throw new ArgumentException(
                        "Skipped application must not carry an applied velocity.",
                        nameof(appliedVelocity));
                }

                break;

            case ScriptMappedMovementRequestApplicationStatus.Applied:
                if (appliedVelocity is null)
                {
                    throw new ArgumentException(
                        "Applied movement requires a non-null applied velocity.",
                        nameof(appliedVelocity));
                }

                RequireMovementMapping(mappingRecord);
                break;

            case ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement:
            case ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange:
            case ScriptMappedMovementRequestApplicationStatus.TankDestroyed:
                if (appliedVelocity is not null)
                {
                    throw new ArgumentException(
                        "Non-applied movement outcomes must not carry an applied velocity.",
                        nameof(appliedVelocity));
                }

                RequireMovementMapping(mappingRecord);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Movement request application status must be a defined enum value.");
        }
    }

    private static void RequireMovementMapping(ScriptDomainRequestMappingRecord mappingRecord)
    {
        if (mappingRecord.Category != ScriptDomainRequestCategory.Movement)
        {
            throw new ArgumentException(
                "Non-skipped movement application records require a movement mapping record.",
                nameof(mappingRecord));
        }
    }
}
