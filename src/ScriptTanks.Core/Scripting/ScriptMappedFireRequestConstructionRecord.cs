using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable outcome for constructing a <see cref="MatchFireRequest"/> from one
/// <see cref="ScriptDomainRequestMappingRecord"/> in a future factory implementation.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not construct requests, execute fire, mutate state, log, replay,
/// or integrate Godot.
/// </remarks>
public sealed class ScriptMappedFireRequestConstructionRecord
{
    public int RecordIndex { get; }

    public ScriptDomainRequestMappingRecord MappingRecord { get; }

    public ScriptMappedFireRequestConstructionStatus Status { get; }

    public MatchFireRequest? FireRequest { get; }

    public bool DidConstruct =>
        Status == ScriptMappedFireRequestConstructionStatus.Constructed
        && FireRequest.HasValue;

    public ScriptMappedFireRequestConstructionRecord(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        ScriptMappedFireRequestConstructionStatus status,
        MatchFireRequest? fireRequest)
    {
        if (recordIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(mappingRecord);

        if (!Enum.IsDefined(typeof(ScriptMappedFireRequestConstructionStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Fire request construction status must be a defined enum value.");
        }

        ValidateFireRequestInvariant(status, fireRequest);

        RecordIndex = recordIndex;
        MappingRecord = mappingRecord;
        Status = status;
        FireRequest = fireRequest;
    }

    public static ScriptMappedFireRequestConstructionRecord NotWeapon(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedFireRequestConstructionRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            fireRequest: null);
    }

    public static ScriptMappedFireRequestConstructionRecord NoFireCommand(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedFireRequestConstructionRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedFireRequestConstructionStatus.NoFireCommand,
            fireRequest: null);
    }

    public static ScriptMappedFireRequestConstructionRecord Unsupported(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord)
    {
        return new ScriptMappedFireRequestConstructionRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedFireRequestConstructionStatus.Unsupported,
            fireRequest: null);
    }

    public static ScriptMappedFireRequestConstructionRecord Constructed(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        MatchFireRequest fireRequest)
    {
        return new ScriptMappedFireRequestConstructionRecord(
            recordIndex,
            mappingRecord,
            ScriptMappedFireRequestConstructionStatus.Constructed,
            fireRequest);
    }

    private static void ValidateFireRequestInvariant(
        ScriptMappedFireRequestConstructionStatus status,
        MatchFireRequest? fireRequest)
    {
        if (status == ScriptMappedFireRequestConstructionStatus.Constructed)
        {
            if (!fireRequest.HasValue)
            {
                throw new ArgumentException(
                    "Constructed status requires a non-null fire request.",
                    nameof(fireRequest));
            }
        }
        else if (fireRequest.HasValue)
        {
            throw new ArgumentException(
                "Non-constructed statuses must not carry a fire request.",
                nameof(fireRequest));
        }
    }
}
