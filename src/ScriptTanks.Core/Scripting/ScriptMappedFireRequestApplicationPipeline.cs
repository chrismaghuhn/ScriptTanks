using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Applies constructed <see cref="MatchFireRequest"/> values from script-mapped fire request
/// construction to a <see cref="MatchSensorRuntimeState"/> snapshot in deterministic record order.
/// </summary>
/// <remarks>
/// Only records with <see cref="ScriptMappedFireRequestConstructionRecord.DidConstruct"/> invoke
/// <see cref="MatchStateFireSystem.ResolveFire(MatchState, MatchFireRequest, Simulation.SimTick)"/>.
/// Does not construct fire requests, allocate projectile ids, spawn projectiles manually,
/// mutate weapon cooldown outside the fire system, integrate MatchRunner, log, replay, or Godot.
/// The <paramref name="runtime"/> snapshot may differ from the integration runtime when an
/// earlier pass updated match state in the same script tick.
/// </remarks>
public static class ScriptMappedFireRequestApplicationPipeline
{
    /// <summary>
    /// Sequentially applies constructed fire requests from <paramref name="constructionPipelineResult"/>
    /// to <paramref name="runtime"/>, returning the final runtime and per-record outcomes.
    /// </summary>
    /// <param name="runtime">
    /// Non-null runtime snapshot whose <see cref="MatchSensorRuntimeState.State"/> is mutated by
    /// fire resolution. May differ from
    /// <c>constructionPipelineResult.ConstructionResult.MappingResult.IntegrationResult.Runtime</c>
    /// when state was updated earlier in the same composition pass.
    /// </param>
    public static ScriptMappedFireRequestApplicationResult Apply(
        MatchSensorRuntimeState runtime,
        ScriptMappedFireRequestConstructionPipelineResult constructionPipelineResult)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(constructionPipelineResult);

        ScriptMappedFireRequestConstructionResult constructionResult =
            constructionPipelineResult.ConstructionResult;

        MatchSensorRuntimeState currentRuntime = runtime;
        MatchState currentState = currentRuntime.State;

        var applicationRecords =
            new ScriptMappedFireRequestApplicationRecord[constructionResult.Count];

        for (int recordIndex = 0; recordIndex < constructionResult.Count; recordIndex++)
        {
            ScriptMappedFireRequestConstructionRecord constructionRecord =
                constructionResult.GetRecordAtIndex(recordIndex);

            if (!constructionRecord.DidConstruct)
            {
                applicationRecords[recordIndex] =
                    ScriptMappedFireRequestApplicationRecord.SkippedNotConstructed(
                        recordIndex,
                        constructionRecord);
                continue;
            }

            MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
                currentState,
                constructionRecord.FireRequest!.Value,
                currentState.CurrentTick);

            currentState = outcome.UpdatedState;
            currentRuntime = currentRuntime.WithState(currentState);

            applicationRecords[recordIndex] = outcome.DidFire
                ? ScriptMappedFireRequestApplicationRecord.Applied(
                    recordIndex,
                    constructionRecord,
                    outcome)
                : ScriptMappedFireRequestApplicationRecord.FireRejected(
                    recordIndex,
                    constructionRecord,
                    outcome);
        }

        return new ScriptMappedFireRequestApplicationResult(
            constructionPipelineResult,
            currentRuntime,
            applicationRecords);
    }
}
