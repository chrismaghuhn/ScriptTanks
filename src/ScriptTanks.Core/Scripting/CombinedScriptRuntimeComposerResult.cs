using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Bundles the outputs of script intent integration, domain mapping, sensor application,
/// fire request construction, fire request application, movement application, and the Option A
/// merged final runtime for one combined script-runtime composition pass.
/// </summary>
/// <remarks>
/// Pure data carrier for <see cref="CombinedScriptRuntimeComposer"/>. Final
/// <see cref="MatchSensorRuntimeState.State"/> comes from movement application; final
/// <see cref="MatchSensorRuntimeState.SensorLoadouts"/> from sensor application. Movement sets
/// <see cref="Movement.MovementState.VelocityPerTick"/> only; position is not integrated. Does not
/// call <see cref="Movement.MovementIntegrator"/>, run <see cref="Match.MatchTickPipeline"/> tank
/// movement, log, replay, or integrate Godot.
/// </remarks>
public sealed class CombinedScriptRuntimeComposerResult
{
    public MatchSensorRuntimeState InitialRuntime { get; }

    public MatchScriptIntentIntegrationResult IntegrationResult { get; }

    public MatchScriptDomainRequestMappingResult MappingResult { get; }

    public ScriptMappedSensorRequestApplicationResult SensorApplicationResult { get; }

    public ScriptMappedTurretRequestApplicationResult TurretApplicationResult { get; }

    public ScriptMappedFireRequestConstructionPipelineResult FireConstructionPipelineResult { get; }

    public ScriptMappedFireRequestApplicationResult FireApplicationResult { get; }

    public ScriptMappedMovementRequestApplicationResult MovementApplicationResult { get; }

    public MatchSensorRuntimeState FinalRuntime { get; }

    public ProjectileIdSequence FinalProjectileIdSequence { get; }

    public CombinedScriptRuntimeComposerResult(
        MatchSensorRuntimeState initialRuntime,
        MatchScriptIntentIntegrationResult integrationResult,
        MatchScriptDomainRequestMappingResult mappingResult,
        ScriptMappedSensorRequestApplicationResult sensorApplicationResult,
        ScriptMappedTurretRequestApplicationResult turretApplicationResult,
        ScriptMappedFireRequestConstructionPipelineResult fireConstructionPipelineResult,
        ScriptMappedFireRequestApplicationResult fireApplicationResult,
        ScriptMappedMovementRequestApplicationResult movementApplicationResult,
        MatchSensorRuntimeState finalRuntime,
        ProjectileIdSequence finalProjectileIdSequence)
    {
        ArgumentNullException.ThrowIfNull(initialRuntime);
        ArgumentNullException.ThrowIfNull(integrationResult);
        ArgumentNullException.ThrowIfNull(mappingResult);
        ArgumentNullException.ThrowIfNull(sensorApplicationResult);
        ArgumentNullException.ThrowIfNull(turretApplicationResult);
        ArgumentNullException.ThrowIfNull(fireConstructionPipelineResult);
        ArgumentNullException.ThrowIfNull(fireApplicationResult);
        ArgumentNullException.ThrowIfNull(movementApplicationResult);
        ArgumentNullException.ThrowIfNull(finalRuntime);

        ValidateReferenceChain(
            initialRuntime,
            integrationResult,
            mappingResult,
            sensorApplicationResult,
            turretApplicationResult,
            fireConstructionPipelineResult,
            fireApplicationResult,
            movementApplicationResult);

        ValidateOptionAFinalComposition(
            finalRuntime,
            sensorApplicationResult,
            movementApplicationResult,
            fireConstructionPipelineResult,
            finalProjectileIdSequence);

        InitialRuntime = initialRuntime;
        IntegrationResult = integrationResult;
        MappingResult = mappingResult;
        SensorApplicationResult = sensorApplicationResult;
        TurretApplicationResult = turretApplicationResult;
        FireConstructionPipelineResult = fireConstructionPipelineResult;
        FireApplicationResult = fireApplicationResult;
        MovementApplicationResult = movementApplicationResult;
        FinalRuntime = finalRuntime;
        FinalProjectileIdSequence = finalProjectileIdSequence;
    }

    private static void ValidateReferenceChain(
        MatchSensorRuntimeState initialRuntime,
        MatchScriptIntentIntegrationResult integrationResult,
        MatchScriptDomainRequestMappingResult mappingResult,
        ScriptMappedSensorRequestApplicationResult sensorApplicationResult,
        ScriptMappedTurretRequestApplicationResult turretApplicationResult,
        ScriptMappedFireRequestConstructionPipelineResult fireConstructionPipelineResult,
        ScriptMappedFireRequestApplicationResult fireApplicationResult,
        ScriptMappedMovementRequestApplicationResult movementApplicationResult)
    {
        if (!ReferenceEquals(initialRuntime, integrationResult.Runtime))
        {
            throw new ArgumentException(
                "Initial runtime must be the same reference as integrationResult.Runtime.",
                nameof(initialRuntime));
        }

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

        if (!ReferenceEquals(turretApplicationResult.MappingResult, mappingResult))
        {
            throw new ArgumentException(
                "Turret application result must reference the same mapping result instance.",
                nameof(turretApplicationResult));
        }

        if (!ReferenceEquals(
                fireConstructionPipelineResult.ConstructionResult.MappingResult,
                mappingResult))
        {
            throw new ArgumentException(
                "Fire construction pipeline result must reference the same mapping result instance.",
                nameof(fireConstructionPipelineResult));
        }

        if (!ReferenceEquals(
                fireApplicationResult.ConstructionPipelineResult,
                fireConstructionPipelineResult))
        {
            throw new ArgumentException(
                "Fire application result must reference the same fire construction pipeline result instance.",
                nameof(fireApplicationResult));
        }

        if (!ReferenceEquals(movementApplicationResult.MappingResult, mappingResult))
        {
            throw new ArgumentException(
                "Movement application result must reference the same mapping result instance.",
                nameof(movementApplicationResult));
        }
    }

    private static void ValidateOptionAFinalComposition(
        MatchSensorRuntimeState finalRuntime,
        ScriptMappedSensorRequestApplicationResult sensorApplicationResult,
        ScriptMappedMovementRequestApplicationResult movementApplicationResult,
        ScriptMappedFireRequestConstructionPipelineResult fireConstructionPipelineResult,
        ProjectileIdSequence finalProjectileIdSequence)
    {
        if (!ReferenceEquals(
                finalRuntime.State,
                movementApplicationResult.FinalRuntime.State))
        {
            throw new ArgumentException(
                "Final runtime state must be the same reference as movement application final runtime state.",
                nameof(finalRuntime));
        }

        if (!ReferenceEquals(
                finalRuntime.SensorLoadouts,
                sensorApplicationResult.FinalRuntime.SensorLoadouts))
        {
            throw new ArgumentException(
                "Final runtime sensor loadouts must be the same reference as sensor application final sensor loadouts.",
                nameof(finalRuntime));
        }

        if (finalProjectileIdSequence != fireConstructionPipelineResult.FinalProjectileIdSequence)
        {
            throw new ArgumentException(
                "Final projectile id sequence must match fire construction pipeline final sequence.",
                nameof(finalProjectileIdSequence));
        }
    }
}
