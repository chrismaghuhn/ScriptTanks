using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Batch outcome of applying script-mapped fire request construction records to a sensor runtime snapshot.
/// </summary>
/// <remarks>
/// Defensively copies application records. Does not call <see cref="Match.MatchStateFireSystem"/>,
/// execute fire, mutate state, log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedFireRequestApplicationResult
{
    private readonly ScriptMappedFireRequestApplicationRecord[] _records;

    public ScriptMappedFireRequestConstructionPipelineResult ConstructionPipelineResult { get; }

    public MatchSensorRuntimeState FinalRuntime { get; }

    public IReadOnlyList<ScriptMappedFireRequestApplicationRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public ScriptMappedFireRequestApplicationResult(
        ScriptMappedFireRequestConstructionPipelineResult constructionPipelineResult,
        MatchSensorRuntimeState finalRuntime,
        IEnumerable<ScriptMappedFireRequestApplicationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(constructionPipelineResult);
        ArgumentNullException.ThrowIfNull(finalRuntime);
        ArgumentNullException.ThrowIfNull(records);

        ScriptMappedFireRequestApplicationRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        if (copy.Length != constructionPipelineResult.ConstructionResult.Count)
        {
            throw new ArgumentException(
                "Application record count must match construction result record count.",
                nameof(records));
        }

        ConstructionPipelineResult = constructionPipelineResult;
        FinalRuntime = finalRuntime;
        _records = copy;
    }

    public ScriptMappedFireRequestApplicationRecord GetRecordAtIndex(int recordIndex)
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
                "Record index is outside the script mapped fire request application result range.");
        }
    }
}
