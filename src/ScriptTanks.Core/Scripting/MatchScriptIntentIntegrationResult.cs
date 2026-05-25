using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable container for match-level script intent integration output: the unchanged
/// sensor runtime snapshot and one record per tank in deterministic index order.
/// </summary>
/// <remarks>
/// Defensively copies the supplied record sequence. This type does not evaluate scripts,
/// execute commands, mutate match state, dispatch requests, log, diagnose, replay, or
/// integrate Godot.
/// </remarks>
public sealed class MatchScriptIntentIntegrationResult
{
    private readonly MatchScriptIntentIntegrationRecord[] _records;

    public MatchSensorRuntimeState Runtime { get; }

    public IReadOnlyList<MatchScriptIntentIntegrationRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public MatchScriptIntentIntegrationResult(
        MatchSensorRuntimeState runtime,
        IEnumerable<MatchScriptIntentIntegrationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(records);

        MatchScriptIntentIntegrationRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        Runtime = runtime;
        _records = copy;
    }

    public MatchScriptIntentIntegrationRecord GetRecordAtIndex(int recordIndex)
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
                "Record index is outside the match script intent integration result range.");
        }
    }
}
