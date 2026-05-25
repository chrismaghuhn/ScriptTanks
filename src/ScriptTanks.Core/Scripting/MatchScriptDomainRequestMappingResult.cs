using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable batch container pairing a <see cref="MatchScriptIntentIntegrationResult"/> with
/// one domain mapping record per integration record, in the same order.
/// </summary>
/// <remarks>
/// Defensively copies the supplied mapping records. This type does not map commands, execute
/// gameplay, dispatch requests, mutate match state, log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchScriptDomainRequestMappingResult
{
    private readonly ScriptDomainRequestMappingRecord[] _records;

    public MatchScriptIntentIntegrationResult IntegrationResult { get; }

    public IReadOnlyList<ScriptDomainRequestMappingRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public MatchScriptDomainRequestMappingResult(
        MatchScriptIntentIntegrationResult integrationResult,
        IEnumerable<ScriptDomainRequestMappingRecord> records)
    {
        ArgumentNullException.ThrowIfNull(integrationResult);
        ArgumentNullException.ThrowIfNull(records);

        ScriptDomainRequestMappingRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        if (copy.Length != integrationResult.Count)
        {
            throw new ArgumentException(
                "Mapping record count must match integration result record count.",
                nameof(records));
        }

        IntegrationResult = integrationResult;
        _records = copy;
    }

    public ScriptDomainRequestMappingRecord GetRecordAtIndex(int recordIndex)
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
                "Record index is outside the domain request mapping result range.");
        }
    }
}
