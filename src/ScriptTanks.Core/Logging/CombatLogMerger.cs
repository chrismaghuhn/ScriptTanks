using System;
using System.Collections.Generic;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Pure helper for merging immutable combat-log snapshots.
/// </summary>
/// <remarks>
/// This type only combines existing <see cref="CombatLog"/> instances into a
/// new immutable snapshot. It does not emit logs, store logs globally,
/// integrate with runners, replay recorders, pipelines, serialization, UI, or
/// Godot. Ordering validation is delegated to <see cref="CombatLog"/>.
/// </remarks>
public static class CombatLogMerger
{
    public static CombatLog Merge(params CombatLog[] logs)
    {
        return Merge((IEnumerable<CombatLog>)logs);
    }

    public static CombatLog Merge(IEnumerable<CombatLog> logs)
    {
        ArgumentNullException.ThrowIfNull(logs);

        List<CombatLogEntry> entries = new List<CombatLogEntry>();
        foreach (CombatLog? log in logs)
        {
            if (log is null)
            {
                throw new ArgumentException(
                    "Combat log sequences must not contain null logs.",
                    nameof(logs));
            }

            entries.AddRange(log.Entries);
        }

        return new CombatLog(entries);
    }
}
