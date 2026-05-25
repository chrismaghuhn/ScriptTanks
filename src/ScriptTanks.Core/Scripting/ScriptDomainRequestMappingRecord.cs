using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable carrier for one tank's script-domain mapping: translation output, category,
/// and optional concrete kernel requests that already exist in the core.
/// </summary>
/// <remarks>
/// This type does not run mapping logic, execute commands, dispatch requests, mutate match state,
/// schedule work, log, record replays, or integrate Godot.
/// </remarks>
public sealed class ScriptDomainRequestMappingRecord
{
    public int TankIndex { get; }

    public TankId TankId { get; }

    public ScriptCommandTranslationOutput TranslationOutput { get; }

    public ScriptDomainRequestCategory Category { get; }

    public MatchSensorScanRequest? SensorRequest { get; }

    public MatchFireRequest? FireRequest { get; }

    public bool HasConcreteRequest =>
        SensorRequest.HasValue || FireRequest.HasValue;

    public ScriptDomainRequestMappingRecord(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput,
        ScriptDomainRequestCategory category,
        MatchSensorScanRequest? sensorRequest,
        MatchFireRequest? fireRequest)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(translationOutput);

        if (!Enum.IsDefined(typeof(ScriptDomainRequestCategory), category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Domain request category must be a defined enum value.");
        }

        ValidateConcreteRequests(category, sensorRequest, fireRequest);

        TankIndex = tankIndex;
        TankId = tankId;
        TranslationOutput = translationOutput;
        Category = category;
        SensorRequest = sensorRequest;
        FireRequest = fireRequest;
    }

    public static ScriptDomainRequestMappingRecord None(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput)
    {
        return new ScriptDomainRequestMappingRecord(
            tankIndex,
            tankId,
            translationOutput,
            ScriptDomainRequestCategory.None,
            sensorRequest: null,
            fireRequest: null);
    }

    public static ScriptDomainRequestMappingRecord Unsupported(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput)
    {
        return new ScriptDomainRequestMappingRecord(
            tankIndex,
            tankId,
            translationOutput,
            ScriptDomainRequestCategory.Unsupported,
            sensorRequest: null,
            fireRequest: null);
    }

    public static ScriptDomainRequestMappingRecord Sensor(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput,
        MatchSensorScanRequest? sensorRequest)
    {
        return new ScriptDomainRequestMappingRecord(
            tankIndex,
            tankId,
            translationOutput,
            ScriptDomainRequestCategory.Sensor,
            sensorRequest,
            fireRequest: null);
    }

    public static ScriptDomainRequestMappingRecord Weapon(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput,
        MatchFireRequest? fireRequest)
    {
        return new ScriptDomainRequestMappingRecord(
            tankIndex,
            tankId,
            translationOutput,
            ScriptDomainRequestCategory.Weapon,
            sensorRequest: null,
            fireRequest);
    }

    public static ScriptDomainRequestMappingRecord TurretPlaceholder(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput)
    {
        return new ScriptDomainRequestMappingRecord(
            tankIndex,
            tankId,
            translationOutput,
            ScriptDomainRequestCategory.Turret,
            sensorRequest: null,
            fireRequest: null);
    }

    public static ScriptDomainRequestMappingRecord MovementPlaceholder(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput)
    {
        return new ScriptDomainRequestMappingRecord(
            tankIndex,
            tankId,
            translationOutput,
            ScriptDomainRequestCategory.Movement,
            sensorRequest: null,
            fireRequest: null);
    }

    private static void ValidateConcreteRequests(
        ScriptDomainRequestCategory category,
        MatchSensorScanRequest? sensorRequest,
        MatchFireRequest? fireRequest)
    {
        bool hasSensor = sensorRequest.HasValue;
        bool hasFire = fireRequest.HasValue;

        if (hasSensor && hasFire)
        {
            throw new ArgumentException(
                "Sensor and weapon concrete requests cannot both be set.",
                nameof(fireRequest));
        }

        switch (category)
        {
            case ScriptDomainRequestCategory.None:
            case ScriptDomainRequestCategory.Unsupported:
                if (hasSensor || hasFire)
                {
                    throw new ArgumentException(
                        "None and Unsupported categories cannot carry concrete sensor or weapon requests.",
                        hasSensor ? nameof(sensorRequest) : nameof(fireRequest));
                }

                break;

            case ScriptDomainRequestCategory.Turret:
            case ScriptDomainRequestCategory.Movement:
                if (hasSensor || hasFire)
                {
                    throw new ArgumentException(
                        "Turret and Movement categories do not carry sensor or weapon concrete requests in this model version.",
                        hasSensor ? nameof(sensorRequest) : nameof(fireRequest));
                }

                break;

            case ScriptDomainRequestCategory.Sensor:
                if (hasFire)
                {
                    throw new ArgumentException(
                        "Sensor category cannot carry a weapon fire request.",
                        nameof(fireRequest));
                }

                break;

            case ScriptDomainRequestCategory.Weapon:
                if (hasSensor)
                {
                    throw new ArgumentException(
                        "Weapon category cannot carry a sensor scan request.",
                        nameof(sensorRequest));
                }

                break;
        }
    }
}
