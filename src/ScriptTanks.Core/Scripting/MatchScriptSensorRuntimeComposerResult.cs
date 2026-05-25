using System;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Bundles the outputs of script intent integration, domain mapping, and mapped sensor application
/// for one end-to-end pure sensor-runtime composition pass.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not execute gameplay, mutate match state beyond what downstream results
/// describe, log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchScriptSensorRuntimeComposerResult
{
    public MatchScriptIntentIntegrationResult IntegrationResult { get; }

    public MatchScriptDomainRequestMappingResult MappingResult { get; }

    public ScriptMappedSensorRequestApplicationResult SensorApplicationResult { get; }

    /// <summary>
    /// Final sensor runtime after applying eligible mapped sensor scans; same reference as
    /// <see cref="ScriptMappedSensorRequestApplicationResult.FinalRuntime"/>.
    /// </summary>
    public MatchSensorRuntimeState FinalRuntime => SensorApplicationResult.FinalRuntime;

    public MatchScriptSensorRuntimeComposerResult(
        MatchScriptIntentIntegrationResult integrationResult,
        MatchScriptDomainRequestMappingResult mappingResult,
        ScriptMappedSensorRequestApplicationResult sensorApplicationResult)
    {
        ArgumentNullException.ThrowIfNull(integrationResult);
        ArgumentNullException.ThrowIfNull(mappingResult);
        ArgumentNullException.ThrowIfNull(sensorApplicationResult);

        if (!ReferenceEquals(mappingResult.IntegrationResult, integrationResult))
        {
            throw new ArgumentException(
                "Mapping result must reference the same integration result instance.",
                nameof(mappingResult));
        }

        if (!ReferenceEquals(sensorApplicationResult.MappingResult, mappingResult))
        {
            throw new ArgumentException(
                "Sensor application result must reference the same mapping result instance.",
                nameof(sensorApplicationResult));
        }

        IntegrationResult = integrationResult;
        MappingResult = mappingResult;
        SensorApplicationResult = sensorApplicationResult;
    }
}
