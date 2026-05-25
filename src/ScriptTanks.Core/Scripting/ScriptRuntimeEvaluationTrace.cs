using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable ordered sequence of <see cref="ScriptRuntimeEvaluationRecord"/> values for future script
/// runtime evaluation tracing.
/// </summary>
/// <remarks>
/// Defensively copies the supplied record sequence and preserves order exactly. This type does not
/// validate tick ordering, tank index uniqueness, or record consistency, and does not log, diagnose,
/// format output, execute commands, generate match, sensor, weapon, or movement requests, mutate
/// match state, or integrate Godot.
/// </remarks>
public sealed class ScriptRuntimeEvaluationTrace
{
    private readonly ScriptRuntimeEvaluationRecord[] _records;

    public IReadOnlyList<ScriptRuntimeEvaluationRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => Records.Count;

    public ScriptRuntimeEvaluationTrace(
        IEnumerable<ScriptRuntimeEvaluationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        ScriptRuntimeEvaluationRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        _records = copy;
    }

    public ScriptRuntimeEvaluationRecord GetRecordAtIndex(
        int recordIndex)
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
                "Record index is outside the script runtime evaluation trace range.");
        }
    }
}
