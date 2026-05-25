using System;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Applies concrete <see cref="MatchSensorScanRequest"/> values from script domain mapping to a
/// <see cref="MatchSensorRuntimeState"/> snapshot in deterministic record order.
/// </summary>
/// <remarks>
/// Only records with <see cref="ScriptDomainRequestCategory.Sensor"/> and a non-null
/// <see cref="ScriptDomainRequestMappingRecord.SensorRequest"/> invoke the sensor scan pipeline.
/// Does not execute weapons, movement, or turret logic; does not advance simulation ticks,
/// integrate MatchRunner, log, replay, or Godot.
/// </remarks>
public static class ScriptMappedSensorRequestApplicationPipeline
{
    /// <summary>
    /// Sequentially applies eligible sensor scans from <paramref name="mappingResult"/> to
    /// <paramref name="runtime"/>, returning the final runtime and per-record outcomes.
    /// </summary>
    /// <param name="runtime">
    /// Must be the same object reference as <c>mappingResult.IntegrationResult.Runtime</c>.
    /// </param>
    public static ScriptMappedSensorRequestApplicationResult Apply(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mappingResult)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(mappingResult);

        if (!ReferenceEquals(runtime, mappingResult.IntegrationResult.Runtime))
        {
            throw new ArgumentException(
                "Runtime must be the same reference as mappingResult.IntegrationResult.Runtime.",
                nameof(mappingResult));
        }

        MatchSensorRuntimeState current = runtime;

        var applicationRecords =
            new ScriptMappedSensorRequestApplicationRecord[mappingResult.Count];

        for (int recordIndex = 0; recordIndex < mappingResult.Count; recordIndex++)
        {
            ScriptDomainRequestMappingRecord mappingRecord =
                mappingResult.GetRecordAtIndex(recordIndex);

            if (mappingRecord.Category == ScriptDomainRequestCategory.Sensor
                && mappingRecord.SensorRequest.HasValue)
            {
                MatchSensorRuntimeTickScanOutcome outcome =
                    MatchSensorRuntimeTickScanPipeline.ApplyOptionalScan(
                        current,
                        mappingRecord.SensorRequest);

                current = outcome.UpdatedRuntime;

                applicationRecords[recordIndex] =
                    new ScriptMappedSensorRequestApplicationRecord(
                        recordIndex,
                        mappingRecord,
                        outcome.DidScan,
                        outcome.Result);
            }
            else
            {
                applicationRecords[recordIndex] =
                    new ScriptMappedSensorRequestApplicationRecord(
                        recordIndex,
                        mappingRecord,
                        didApply: false,
                        scanResult: null);
            }
        }

        return new ScriptMappedSensorRequestApplicationResult(
            mappingResult,
            current,
            applicationRecords);
    }
}
