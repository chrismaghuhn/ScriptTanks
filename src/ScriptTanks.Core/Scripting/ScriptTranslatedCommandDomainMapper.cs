using System;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure domain mapper from integrated script translation output to <see cref="ScriptDomainRequestMappingRecord"/>
/// values. Does not execute sensors, weapons, or movement; does not mutate match or runtime state.
/// </summary>
public static class ScriptTranslatedCommandDomainMapper
{
    /// <summary>
    /// Maps one <see cref="MatchScriptIntentIntegrationRecord"/> to a <see cref="ScriptDomainRequestMappingRecord"/>.
    /// </summary>
    public static ScriptDomainRequestMappingRecord Map(MatchScriptIntentIntegrationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        ScriptCommandTranslationOutput translationOutput = record.TranslationOutput;

        switch (translationOutput.Result.Status)
        {
            case ScriptCommandTranslationStatus.NoIntent:
                return ScriptDomainRequestMappingRecord.None(
                    record.TankIndex,
                    record.TankId,
                    translationOutput);

            case ScriptCommandTranslationStatus.UnsupportedCommand:
            case ScriptCommandTranslationStatus.InvalidCommandArgument:
            case ScriptCommandTranslationStatus.MissingHardware:
                return ScriptDomainRequestMappingRecord.Unsupported(
                    record.TankIndex,
                    record.TankId,
                    translationOutput);

            case ScriptCommandTranslationStatus.Translated:
                return MapTranslated(record);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(translationOutput.Result.Status),
                    translationOutput.Result.Status,
                    "Unhandled script command translation status.");
        }
    }

    /// <summary>
    /// Maps every integration record in index order and wraps the result with the same
    /// <see cref="MatchScriptIntentIntegrationResult"/> reference.
    /// </summary>
    public static MatchScriptDomainRequestMappingResult MapAll(
        MatchScriptIntentIntegrationResult integrationResult)
    {
        ArgumentNullException.ThrowIfNull(integrationResult);

        int count = integrationResult.Count;
        var mapped = new ScriptDomainRequestMappingRecord[count];

        for (int i = 0; i < count; i++)
        {
            mapped[i] = Map(integrationResult.GetRecordAtIndex(i));
        }

        return new MatchScriptDomainRequestMappingResult(integrationResult, mapped);
    }

    private static ScriptDomainRequestMappingRecord MapTranslated(
        MatchScriptIntentIntegrationRecord record)
    {
        ScriptCommandTranslationOutput translationOutput = record.TranslationOutput;
        ScriptTranslatedCommandRequest request = translationOutput.Request;

        if (!request.HasRequest)
        {
            return ScriptDomainRequestMappingRecord.Unsupported(
                record.TankIndex,
                record.TankId,
                translationOutput);
        }

        switch (request.Kind)
        {
            case ScriptTranslatedCommandRequestKind.None:
                return ScriptDomainRequestMappingRecord.Unsupported(
                    record.TankIndex,
                    record.TankId,
                    translationOutput);

            case ScriptTranslatedCommandRequestKind.NoOp:
                return ScriptDomainRequestMappingRecord.None(
                    record.TankIndex,
                    record.TankId,
                    translationOutput);

            case ScriptTranslatedCommandRequestKind.ScanEnemy:
                MatchSensorScanRequest scanRequest =
                    new MatchSensorScanRequest(record.TankIndex, SensorSlot.Zero);

                return ScriptDomainRequestMappingRecord.Sensor(
                    record.TankIndex,
                    record.TankId,
                    translationOutput,
                    scanRequest);

            case ScriptTranslatedCommandRequestKind.AimAtEnemy:
                return ScriptDomainRequestMappingRecord.TurretPlaceholder(
                    record.TankIndex,
                    record.TankId,
                    translationOutput);

            case ScriptTranslatedCommandRequestKind.Fire:
                return ScriptDomainRequestMappingRecord.Weapon(
                    record.TankIndex,
                    record.TankId,
                    translationOutput,
                    fireRequest: null);

            case ScriptTranslatedCommandRequestKind.MoveToPatrolPoint:
            case ScriptTranslatedCommandRequestKind.Retreat:
                return ScriptDomainRequestMappingRecord.MovementPlaceholder(
                    record.TankIndex,
                    record.TankId,
                    translationOutput);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request.Kind),
                    request.Kind,
                    "Unhandled translated command request kind.");
        }
    }
}
