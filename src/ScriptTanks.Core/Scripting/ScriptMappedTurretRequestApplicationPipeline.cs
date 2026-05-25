using System;
using System.Linq;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Applies script-mapped turret requests to a <see cref="MatchSensorRuntimeState"/> snapshot by
/// setting each tank's <see cref="TankState.TurretRotation"/> in deterministic record order.
/// </summary>
/// <remarks>
/// Uses nearest alive enemy selection from <see cref="MatchState"/> tank positions (exclude self,
/// minimum squared distance, lower <c>TankIndex</c> tie-break). Full-angle aim via
/// <see cref="FixedRotationAimResolver"/>; zero delta yields
/// <see cref="ScriptMappedTurretRequestApplicationStatus.MissingAimSolution"/>. Applies at most
/// <see cref="Tanks.BasicTankStats.TurretTurnRatePerTick"/> per tick toward the desired rotation
/// via <see cref="FixedRotationTurnStepResolver"/>.
/// Only <see cref="TankState.TurretRotation"/> is updated. Does not require <paramref name="runtime"/>
/// to match <c>mappingResult.IntegrationResult.Runtime</c> so callers (5.110) can supply post-sensor
/// snapshots. Does not construct or apply fire requests, integrate
/// <see cref="CombinedScriptRuntimeComposer"/>, change fire reference guards, log, replay, or Godot.
/// </remarks>
public static class ScriptMappedTurretRequestApplicationPipeline
{
    /// <summary>
    /// Sequentially applies eligible turret mappings from <paramref name="mappingResult"/> to
    /// <paramref name="runtime"/>, returning the final runtime and per-record outcomes.
    /// </summary>
    /// <param name="runtime">
    /// Non-null runtime snapshot to mutate. May differ from
    /// <c>mappingResult.IntegrationResult.Runtime</c> when applying after sensor (5.110).
    /// </param>
    public static ScriptMappedTurretRequestApplicationResult Apply(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mappingResult)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(mappingResult);

        MatchSensorRuntimeState currentRuntime = runtime;
        MatchState currentState = currentRuntime.State;

        var applicationRecords =
            new ScriptMappedTurretRequestApplicationRecord[mappingResult.Count];

        for (int recordIndex = 0; recordIndex < mappingResult.Count; recordIndex++)
        {
            ScriptDomainRequestMappingRecord mappingRecord =
                mappingResult.GetRecordAtIndex(recordIndex);

            if (mappingRecord.Category != ScriptDomainRequestCategory.Turret)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedTurretRequestApplicationRecord.SkippedNotTurret(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            int tankIndex = mappingRecord.TankIndex;

            if (tankIndex < 0 || tankIndex >= currentState.Tanks.Count)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedTurretRequestApplicationRecord.TankIndexOutOfRange(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            TankState tank = currentState.Tanks[tankIndex];

            if (tank.IsDestroyed)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedTurretRequestApplicationRecord.TankDestroyed(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            ScriptTranslatedCommandRequestKind kind =
                mappingRecord.TranslationOutput.Request.Kind;

            if (kind != ScriptTranslatedCommandRequestKind.AimAtEnemy)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedTurretRequestApplicationRecord.RejectedInvalidTurretRequest(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            if (!TrySelectNearestAliveEnemy(currentState, tankIndex, out int targetTankIndex))
            {
                applicationRecords[recordIndex] =
                    ScriptMappedTurretRequestApplicationRecord.NoTarget(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            TankState target = currentState.Tanks[targetTankIndex];
            FixedVec2 delta = target.Movement.Position - tank.Movement.Position;
            FixedRotationAimResolution resolution =
                FixedRotationAimResolver.ResolveFromDirection(delta);

            if (!resolution.IsResolved)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedTurretRequestApplicationRecord.MissingAimSolution(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            Fixed desiredRotation = resolution.Rotation;
            FixedRotationTurnStepResult turnStep = FixedRotationTurnStepResolver.ResolveStep(
                tank.TurretRotation,
                desiredRotation,
                tank.Definition.Stats.TurretTurnRatePerTick);
            Fixed appliedRotation = turnStep.FinalRotation;

            TankState updatedTank = tank.WithTurretRotation(appliedRotation);

            TankState[] tanks = currentState.Tanks.ToArray();
            tanks[tankIndex] = updatedTank;
            currentState = currentState.WithTanks(tanks);
            currentRuntime = currentRuntime.WithState(currentState);

            applicationRecords[recordIndex] =
                ScriptMappedTurretRequestApplicationRecord.Applied(
                    recordIndex,
                    mappingRecord,
                    appliedRotation);
        }

        return new ScriptMappedTurretRequestApplicationResult(
            mappingResult,
            currentRuntime,
            applicationRecords);
    }

    private static bool TrySelectNearestAliveEnemy(
        MatchState state,
        int shooterTankIndex,
        out int targetTankIndex)
    {
        targetTankIndex = -1;
        Fixed? bestDistanceSquared = null;

        TankState shooter = state.Tanks[shooterTankIndex];
        FixedVec2 shooterPosition = shooter.Movement.Position;

        for (int candidateIndex = 0; candidateIndex < state.Tanks.Count; candidateIndex++)
        {
            if (candidateIndex == shooterTankIndex)
            {
                continue;
            }

            TankState candidate = state.Tanks[candidateIndex];

            if (candidate.IsDestroyed)
            {
                continue;
            }

            Fixed distanceSquared =
                shooterPosition.DistanceSquaredTo(candidate.Movement.Position);

            if (!bestDistanceSquared.HasValue
                || distanceSquared < bestDistanceSquared.Value
                || (distanceSquared == bestDistanceSquared.Value
                    && candidateIndex < targetTankIndex))
            {
                bestDistanceSquared = distanceSquared;
                targetTankIndex = candidateIndex;
            }
        }

        return targetTankIndex >= 0;
    }
}
