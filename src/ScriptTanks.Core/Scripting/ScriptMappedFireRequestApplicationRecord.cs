using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable outcome for applying one <see cref="ScriptMappedFireRequestConstructionRecord"/>
/// in a future application pipeline implementation.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not call <see cref="MatchStateFireSystem"/>, execute fire,
/// mutate state, log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedFireRequestApplicationRecord
{
    public int RecordIndex { get; }

    public ScriptMappedFireRequestConstructionRecord ConstructionRecord { get; }

    public ScriptMappedFireRequestApplicationStatus Status { get; }

    public MatchStateFireOutcome? FireOutcome { get; }

    public bool DidApply => Status == ScriptMappedFireRequestApplicationStatus.Applied;

    public ScriptMappedFireRequestApplicationRecord(
        int recordIndex,
        ScriptMappedFireRequestConstructionRecord constructionRecord,
        ScriptMappedFireRequestApplicationStatus status,
        MatchStateFireOutcome? fireOutcome)
    {
        if (recordIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(constructionRecord);

        if (!Enum.IsDefined(typeof(ScriptMappedFireRequestApplicationStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Fire request application status must be a defined enum value.");
        }

        ValidateInvariants(status, constructionRecord, fireOutcome);

        RecordIndex = recordIndex;
        ConstructionRecord = constructionRecord;
        Status = status;
        FireOutcome = fireOutcome;
    }

    public static ScriptMappedFireRequestApplicationRecord SkippedNotConstructed(
        int recordIndex,
        ScriptMappedFireRequestConstructionRecord constructionRecord)
    {
        return new ScriptMappedFireRequestApplicationRecord(
            recordIndex,
            constructionRecord,
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            fireOutcome: null);
    }

    public static ScriptMappedFireRequestApplicationRecord Applied(
        int recordIndex,
        ScriptMappedFireRequestConstructionRecord constructionRecord,
        MatchStateFireOutcome fireOutcome)
    {
        ArgumentNullException.ThrowIfNull(fireOutcome);

        return new ScriptMappedFireRequestApplicationRecord(
            recordIndex,
            constructionRecord,
            ScriptMappedFireRequestApplicationStatus.Applied,
            fireOutcome);
    }

    public static ScriptMappedFireRequestApplicationRecord FireRejected(
        int recordIndex,
        ScriptMappedFireRequestConstructionRecord constructionRecord,
        MatchStateFireOutcome fireOutcome)
    {
        ArgumentNullException.ThrowIfNull(fireOutcome);

        return new ScriptMappedFireRequestApplicationRecord(
            recordIndex,
            constructionRecord,
            ScriptMappedFireRequestApplicationStatus.FireRejected,
            fireOutcome);
    }

    private static void ValidateInvariants(
        ScriptMappedFireRequestApplicationStatus status,
        ScriptMappedFireRequestConstructionRecord constructionRecord,
        MatchStateFireOutcome? fireOutcome)
    {
        switch (status)
        {
            case ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed:
                if (fireOutcome is not null)
                {
                    throw new ArgumentException(
                        "Skipped application must not carry a fire outcome.",
                        nameof(fireOutcome));
                }

                if (constructionRecord.DidConstruct)
                {
                    throw new ArgumentException(
                        "Skipped application requires a non-constructed construction record.",
                        nameof(constructionRecord));
                }

                break;

            case ScriptMappedFireRequestApplicationStatus.Applied:
                if (fireOutcome is null)
                {
                    throw new ArgumentException(
                        "Applied fire requires a non-null fire outcome.",
                        nameof(fireOutcome));
                }

                if (!fireOutcome.DidFire)
                {
                    throw new ArgumentException(
                        "Applied fire requires a fire outcome with DidFire true.",
                        nameof(fireOutcome));
                }

                if (!constructionRecord.DidConstruct)
                {
                    throw new ArgumentException(
                        "Applied fire requires a constructed construction record.",
                        nameof(constructionRecord));
                }

                break;

            case ScriptMappedFireRequestApplicationStatus.FireRejected:
                if (fireOutcome is null)
                {
                    throw new ArgumentException(
                        "Rejected fire requires a non-null fire outcome.",
                        nameof(fireOutcome));
                }

                if (fireOutcome.DidFire)
                {
                    throw new ArgumentException(
                        "Rejected fire requires a fire outcome with DidFire false.",
                        nameof(fireOutcome));
                }

                if (!constructionRecord.DidConstruct)
                {
                    throw new ArgumentException(
                        "Rejected fire requires a constructed construction record.",
                        nameof(constructionRecord));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Fire request application status must be a defined enum value.");
        }
    }
}
