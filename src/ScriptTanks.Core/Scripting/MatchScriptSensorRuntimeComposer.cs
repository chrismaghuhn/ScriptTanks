using System;
using System.Collections.Generic;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// End-to-end pure orchestration: script integration, domain mapping, and mapped sensor scan
/// application against a sensor runtime snapshot.
/// </summary>
/// <remarks>
/// Delegates to existing pure components only. Does not run weapons, turret, movement, MatchRunner,
/// schedulers, combat logs, replay, or Godot.
/// </remarks>
public static class MatchScriptSensorRuntimeComposer
{
    /// <summary>
    /// Runs integration, mapping, and sensor application in order and returns all intermediate
    /// results plus the final runtime.
    /// </summary>
    public static MatchScriptSensorRuntimeComposerResult Run(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<ScriptProgram> programs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(programs);

        MatchScriptIntentIntegrationResult integrationResult =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        MatchScriptDomainRequestMappingResult mappingResult =
            ScriptTranslatedCommandDomainMapper.MapAll(integrationResult);

        ScriptMappedSensorRequestApplicationResult sensorApplicationResult =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mappingResult);

        return new MatchScriptSensorRuntimeComposerResult(
            integrationResult,
            mappingResult,
            sensorApplicationResult);
    }
}
