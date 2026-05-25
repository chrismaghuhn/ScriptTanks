using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable ordered sequence of <see cref="ScriptRuntimeDecisionSnapshot"/> values for future
/// script runtime tracing.
/// </summary>
/// <remarks>
/// Defensively copies the supplied snapshot sequence and preserves order exactly. This type does
/// not validate tick ordering, tank uniqueness, log, diagnose, format output, execute commands,
/// generate match, sensor, weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public sealed class ScriptRuntimeDecisionTrace
{
    private readonly ScriptRuntimeDecisionSnapshot[] _snapshots;

    public IReadOnlyList<ScriptRuntimeDecisionSnapshot> Snapshots =>
        Array.AsReadOnly(_snapshots);

    public int Count => Snapshots.Count;

    public ScriptRuntimeDecisionTrace(
        IEnumerable<ScriptRuntimeDecisionSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        ScriptRuntimeDecisionSnapshot[] copy = snapshots.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Snapshot sequence must not contain null elements.",
                    nameof(snapshots));
            }
        }

        _snapshots = copy;
    }

    public ScriptRuntimeDecisionSnapshot GetSnapshotAtIndex(
        int snapshotIndex)
    {
        ValidateIndex(snapshotIndex);

        return _snapshots[snapshotIndex];
    }

    private void ValidateIndex(int snapshotIndex)
    {
        if (snapshotIndex < 0 || snapshotIndex >= _snapshots.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(snapshotIndex),
                snapshotIndex,
                "Snapshot index is outside the script runtime decision trace range.");
        }
    }
}
