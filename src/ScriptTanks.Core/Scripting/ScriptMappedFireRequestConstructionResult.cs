using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Batch container for per-record fire request construction outcomes aligned with a
/// <see cref="MatchScriptDomainRequestMappingResult"/>.
/// </summary>
/// <remarks>
/// Defensively copies records. Does not construct fire requests, execute gameplay, mutate
/// match state, log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedFireRequestConstructionResult
{
    private readonly ScriptMappedFireRequestConstructionRecord[] _records;

    public MatchScriptDomainRequestMappingResult MappingResult { get; }

    public IReadOnlyList<ScriptMappedFireRequestConstructionRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public ScriptMappedFireRequestConstructionResult(
        MatchScriptDomainRequestMappingResult mappingResult,
        IEnumerable<ScriptMappedFireRequestConstructionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(mappingResult);
        ArgumentNullException.ThrowIfNull(records);

        ScriptMappedFireRequestConstructionRecord[] copy = records.ToArray();

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
                "Construction record count must match mapping result record count.",
                nameof(records));
        }

        MappingResult = mappingResult;
        _records = copy;
    }

    public ScriptMappedFireRequestConstructionRecord GetRecordAtIndex(int recordIndex)
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
                "Record index is outside the fire request construction result range.");
        }
    }
}
