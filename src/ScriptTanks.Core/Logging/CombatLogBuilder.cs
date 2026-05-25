using System;
using System.Collections.Generic;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Mutable helper for constructing an immutable <see cref="CombatLog"/>.
/// </summary>
/// <remarks>
/// This type is only a construction helper. It is not a logger service,
/// sink, event bus, persistence layer, serialization layer, replay
/// integration, runner integration, UI, or Godot-facing behavior.
/// Append-only; same-tick entries are allowed; only strictly decreasing
/// tick order is rejected. Equality is reference-based.
/// </remarks>
public sealed class CombatLogBuilder
{
    private readonly List<CombatLogEntry> _entries = new();

    public int Count => _entries.Count;

    public CombatLogBuilder Add(CombatLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateNonDecreasingTick(entry.Tick);

        _entries.Add(entry);
        return this;
    }

    public CombatLogBuilder Add(
        SimTick tick,
        CombatLogCategory category,
        string eventType,
        string message)
    {
        return Add(new CombatLogEntry(tick, category, eventType, message));
    }

    public CombatLog Build()
    {
        return new CombatLog(_entries);
    }

    private void ValidateNonDecreasingTick(SimTick tick)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        int previousTick = _entries[^1].Tick.Value;
        if (tick.Value < previousTick)
        {
            throw new ArgumentException(
                "Combat log entries must be added in non-decreasing tick order.",
                nameof(tick));
        }
    }
}
