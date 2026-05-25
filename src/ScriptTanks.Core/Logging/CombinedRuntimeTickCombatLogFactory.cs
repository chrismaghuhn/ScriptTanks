using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Pure factory that converts one <see cref="CombinedRuntimeTickResult"/> into an
/// immutable <see cref="CombatLog"/> snapshot for the MVP script-tick event subset.
/// </summary>
/// <remarks>
/// Emits <c>script_tick</c>, <c>fire_request_applied</c>, and <c>fire_rejected</c>
/// only. Uses <see cref="CombatLogCategory.Match"/>; a dedicated script category
/// may be added later without changing event type strings. Does not run pipelines,
/// mutate inputs, integrate with runners, replay recorders, UI, or Godot.
/// </remarks>
public static class CombinedRuntimeTickCombatLogFactory
{
    public static CombatLog CreateTickLog(CombinedRuntimeTickResult tickResult)
    {
        ArgumentNullException.ThrowIfNull(tickResult);

        SimTick tick = tickResult.InitialRuntime.State.CurrentTick;
        CombinedScriptRuntimeComposerResult scriptResult = tickResult.ScriptResult;
        ScriptMappedFireRequestConstructionResult constructionResult =
            scriptResult.FireConstructionPipelineResult.ConstructionResult;
        ScriptMappedFireRequestApplicationResult applicationResult =
            scriptResult.FireApplicationResult;

        int firesConstructed = 0;
        for (int i = 0; i < constructionResult.Count; i++)
        {
            if (constructionResult.GetRecordAtIndex(i).DidConstruct)
            {
                firesConstructed++;
            }
        }

        int firesApplied = 0;
        int firesRejected = 0;
        for (int i = 0; i < applicationResult.Count; i++)
        {
            ScriptMappedFireRequestApplicationStatus status =
                applicationResult.GetRecordAtIndex(i).Status;

            if (status == ScriptMappedFireRequestApplicationStatus.Applied)
            {
                firesApplied++;
            }
            else if (status == ScriptMappedFireRequestApplicationStatus.FireRejected)
            {
                firesRejected++;
            }
        }

        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Match,
            CombatLogEventTypes.ScriptTick,
            FormattableString.Invariant(
                $"Script tick: tick={tick.Value} tanks={tickResult.InitialRuntime.State.Tanks.Count} fires_constructed={firesConstructed} fires_applied={firesApplied} fires_rejected={firesRejected}."));

        for (int recordIndex = 0; recordIndex < applicationResult.Count; recordIndex++)
        {
            ScriptMappedFireRequestApplicationRecord record =
                applicationResult.GetRecordAtIndex(recordIndex);

            switch (record.Status)
            {
                case ScriptMappedFireRequestApplicationStatus.Applied:
                    AppendAppliedFireLog(builder, tick, record);
                    break;

                case ScriptMappedFireRequestApplicationStatus.FireRejected:
                    AppendRejectedFireLog(builder, tick, record);
                    break;
            }
        }

        return builder.Build();
    }

    private static void AppendAppliedFireLog(
        CombatLogBuilder builder,
        SimTick tick,
        ScriptMappedFireRequestApplicationRecord record)
    {
        MatchFireRequest fireRequest = record.ConstructionRecord.FireRequest!.Value;
        int projectileId = record.FireOutcome!.SpawnedProjectile!.Value.Id.Value;

        builder.Add(
            tick,
            CombatLogCategory.Match,
            CombatLogEventTypes.FireRequestApplied,
            FormattableString.Invariant(
                $"Script fire applied: tick={tick.Value} record_index={record.RecordIndex} tank_id={fireRequest.ShooterTankId.Value} projectile_id={projectileId} weapon_slot={fireRequest.WeaponSlot.Value}."));
    }

    private static void AppendRejectedFireLog(
        CombatLogBuilder builder,
        SimTick tick,
        ScriptMappedFireRequestApplicationRecord record)
    {
        int tankId = record.ConstructionRecord.MappingRecord.TankId.Value;

        builder.Add(
            tick,
            CombatLogCategory.Match,
            CombatLogEventTypes.FireRejected,
            FormattableString.Invariant(
                $"Script fire rejected: tick={tick.Value} record_index={record.RecordIndex} tank_id={tankId} reason=not_ready."));
    }
}
