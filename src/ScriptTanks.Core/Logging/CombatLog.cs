using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Immutable container for combat-log entries ordered by simulation tick.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not perform logging, routing,
/// persistence, serialization, replay integration, runner integration,
/// UI, or Godot-facing behavior. It does not own a sink or a builder.
/// Empty logs are valid. Multiple entries can share the same simulation
/// tick (only strictly decreasing tick order is rejected). Entry
/// references are preserved verbatim. Equality is reference-based.
/// </remarks>
public sealed class CombatLog
{
    public IReadOnlyList<CombatLogEntry> Entries { get; }

    public CombatLog(IEnumerable<CombatLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        CombatLogEntry[] copiedEntries = entries.ToArray();

        int previousTick = -1;
        for (int i = 0; i < copiedEntries.Length; i++)
        {
            CombatLogEntry? entry = copiedEntries[i];
            if (entry is null)
            {
                throw new ArgumentException(
                    "Combat logs must not contain null entries.",
                    nameof(entries));
            }

            int tick = entry.Tick.Value;
            if (tick < previousTick)
            {
                throw new ArgumentException(
                    "Combat log entries must be ordered by non-decreasing tick.",
                    nameof(entries));
            }

            previousTick = tick;
        }

        Entries = Array.AsReadOnly(copiedEntries);
    }
}
