using System;
using System.Linq;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Applies script-mapped movement requests to a <see cref="MatchSensorRuntimeState"/> snapshot by
/// setting each tank's <see cref="Movement.MovementState.VelocityPerTick"/> in deterministic record order.
/// </summary>
/// <remarks>
/// Only <see cref="Movement.MovementState.VelocityPerTick"/> is updated; position is unchanged.
/// Does not require <paramref name="runtime"/> to match
/// <c>mappingResult.IntegrationResult.Runtime</c> so callers (5.105) can supply a post-fire snapshot.
/// Does not call <see cref="Movement.MovementIntegrator"/>, run <see cref="Match.MatchTickPipeline"/>
/// tank movement, integrate MatchRunner, log, replay, or Godot.
/// </remarks>
public static class ScriptMappedMovementRequestApplicationPipeline
{
    /// <summary>
    /// Sequentially applies eligible movement mappings from <paramref name="mappingResult"/> to
    /// <paramref name="runtime"/>, returning the final runtime and per-record outcomes.
    /// </summary>
    /// <param name="runtime">
    /// Non-null runtime snapshot to mutate. May differ from
    /// <c>mappingResult.IntegrationResult.Runtime</c> when applying after fire (5.105).
    /// </param>
    public static ScriptMappedMovementRequestApplicationResult Apply(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mappingResult)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(mappingResult);

        MatchSensorRuntimeState currentRuntime = runtime;
        MatchState currentState = currentRuntime.State;

        var applicationRecords =
            new ScriptMappedMovementRequestApplicationRecord[mappingResult.Count];

        for (int recordIndex = 0; recordIndex < mappingResult.Count; recordIndex++)
        {
            ScriptDomainRequestMappingRecord mappingRecord =
                mappingResult.GetRecordAtIndex(recordIndex);

            if (mappingRecord.Category != ScriptDomainRequestCategory.Movement)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedMovementRequestApplicationRecord.SkippedNotMovement(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            int tankIndex = mappingRecord.TankIndex;

            if (tankIndex < 0 || tankIndex >= currentState.Tanks.Count)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedMovementRequestApplicationRecord.TankIndexOutOfRange(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            TankState tank = currentState.Tanks[tankIndex];

            if (tank.IsDestroyed)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedMovementRequestApplicationRecord.TankDestroyed(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            ScriptTranslatedCommandRequestKind kind =
                mappingRecord.TranslationOutput.Request.Kind;

            if (!ScriptMovementVelocityResolver.TryResolve(tank, kind, out FixedVec2 velocity))
            {
                applicationRecords[recordIndex] =
                    ScriptMappedMovementRequestApplicationRecord.RejectedInvalidMovement(
                        recordIndex,
                        mappingRecord);
                continue;
            }

            TankState updatedTank = tank.WithMovement(
                tank.Movement.WithVelocityPerTick(velocity));

            TankState[] tanks = currentState.Tanks.ToArray();
            tanks[tankIndex] = updatedTank;
            currentState = currentState.WithTanks(tanks);
            currentRuntime = currentRuntime.WithState(currentState);

            applicationRecords[recordIndex] =
                ScriptMappedMovementRequestApplicationRecord.Applied(
                    recordIndex,
                    mappingRecord,
                    velocity);
        }

        return new ScriptMappedMovementRequestApplicationResult(
            mappingResult,
            currentRuntime,
            applicationRecords);
    }
}
