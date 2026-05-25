using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Batch outcome of applying script-mapped movement requests to a sensor runtime snapshot.
/// </summary>
/// <remarks>
/// Defensively copies application records. Does not derive velocity, mutate match state,
/// call <see cref="Movement.MovementIntegrator"/>, run <see cref="Match.MatchTickPipeline"/>,
/// log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedMovementRequestApplicationResult
{
    private readonly ScriptMappedMovementRequestApplicationRecord[] _records;

    public MatchScriptDomainRequestMappingResult MappingResult { get; }

    public MatchSensorRuntimeState FinalRuntime { get; }

    public IReadOnlyList<ScriptMappedMovementRequestApplicationRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public ScriptMappedMovementRequestApplicationResult(
        MatchScriptDomainRequestMappingResult mappingResult,
        MatchSensorRuntimeState finalRuntime,
        IEnumerable<ScriptMappedMovementRequestApplicationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(mappingResult);
        ArgumentNullException.ThrowIfNull(finalRuntime);
        ArgumentNullException.ThrowIfNull(records);

        ScriptMappedMovementRequestApplicationRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        if (copy.Length != mappingResult.Count)
        {
            throw new ArgumentException(
                "Application record count must match mapping result record count.",
                nameof(records));
        }

        MappingResult = mappingResult;
        FinalRuntime = finalRuntime;
        _records = copy;
    }

    public ScriptMappedMovementRequestApplicationRecord GetRecordAtIndex(int recordIndex)
    {
        ValidateIndex(recordIndex);

        return _records[recordIndex];
    }

    private void ValidateIndex(int recordIndex)
    {
        if (recordIndex < 0 || recordIndex >= _records.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index is outside the script mapped movement request application result range.");
        }
    }
}
