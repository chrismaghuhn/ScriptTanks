using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// End-to-end pure orchestration: script integration, domain mapping, sensor application,
/// fire request construction, fire request application, turret application, movement application,
/// and Option A merged final runtime.
/// </summary>
/// <remarks>
/// Sensor and turret apply use the initial runtime snapshot (<c>runtime₀</c>). Fire construct/apply
/// use <c>turretApplicationResult.FinalRuntime</c> so muzzle and velocity resolvers see updated
/// <see cref="Tanks.TankState.TurretRotation"/>. Movement apply uses
/// <c>fireApplicationResult.FinalRuntime</c> so fire-applied match state is preserved.
/// Final <see cref="MatchSensorRuntimeState.State"/> comes from movement; final
/// <see cref="MatchSensorRuntimeState.SensorLoadouts"/> from sensor. Movement sets
/// <see cref="Movement.MovementState.VelocityPerTick"/> only; position is not integrated. Does not
/// call <see cref="Movement.MovementIntegrator"/>, run <see cref="Match.MatchTickPipeline"/> tank
/// movement, MatchRunner, schedulers, combat logs, or Godot.
/// </remarks>
public static class CombinedScriptRuntimeComposer
{
    /// <summary>
    /// Runs integration, mapping, sensor, fire, and movement pipelines on <paramref name="runtime"/>,
    /// merges movement state with sensor loadouts, and returns all intermediate results.
    /// </summary>
    public static CombinedScriptRuntimeComposerResult Run(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<ScriptProgram> programs,
        ProjectileIdSequence projectileIdSequence)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        MatchScriptIntentIntegrationResult integrationResult =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        MatchScriptDomainRequestMappingResult mappingResult =
            ScriptTranslatedCommandDomainMapper.MapAll(integrationResult);

        ScriptMappedSensorRequestApplicationResult sensorApplicationResult =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mappingResult);

        ScriptMappedTurretRequestApplicationResult turretApplicationResult =
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, mappingResult);

        ScriptMappedFireRequestConstructionPipelineResult fireConstructionPipelineResult =
            ScriptMappedFireRequestConstructionPipeline.Construct(
                turretApplicationResult.FinalRuntime,
                mappingResult,
                projectileIdSequence);

        ScriptMappedFireRequestApplicationResult fireApplicationResult =
            ScriptMappedFireRequestApplicationPipeline.Apply(
                turretApplicationResult.FinalRuntime,
                fireConstructionPipelineResult);

        ScriptMappedMovementRequestApplicationResult movementApplicationResult =
            ScriptMappedMovementRequestApplicationPipeline.Apply(
                fireApplicationResult.FinalRuntime,
                mappingResult);

        MatchSensorRuntimeState finalRuntime =
            movementApplicationResult.FinalRuntime.WithSensorLoadouts(
                sensorApplicationResult.FinalRuntime.SensorLoadouts);

        return new CombinedScriptRuntimeComposerResult(
            runtime,
            integrationResult,
            mappingResult,
            sensorApplicationResult,
            turretApplicationResult,
            fireConstructionPipelineResult,
            fireApplicationResult,
            movementApplicationResult,
            finalRuntime,
            fireConstructionPipelineResult.FinalProjectileIdSequence);
    }
}
