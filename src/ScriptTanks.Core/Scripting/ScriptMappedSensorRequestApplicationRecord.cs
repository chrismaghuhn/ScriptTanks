using System;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcome of attempting to apply one script domain mapping record to the sensor runtime.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not execute scans, mutate runtime, log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedSensorRequestApplicationRecord
{
    public int RecordIndex { get; }

    public ScriptDomainRequestMappingRecord MappingRecord { get; }

    public bool DidApply { get; }

    public SensorScanResult? ScanResult { get; }

    public ScriptMappedSensorRequestApplicationRecord(
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        bool didApply,
        SensorScanResult? scanResult)
    {
        if (recordIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(mappingRecord);

        if (didApply && scanResult is null)
        {
            throw new ArgumentException(
                "Applied sensor scan requires a non-null scan result.",
                nameof(scanResult));
        }

        if (!didApply && scanResult is not null)
        {
            throw new ArgumentException(
                "Skipped application must not carry a scan result.",
                nameof(scanResult));
        }

        RecordIndex = recordIndex;
        MappingRecord = mappingRecord;
        DidApply = didApply;
        ScanResult = scanResult;
    }
}
