using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Immutable combat-log event for future replay/debug explanations.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not perform logging, routing,
/// persistence, serialization, replay integration, runner integration,
/// UI, or Godot-facing behavior. It does not own a sink or a collection.
/// Strings are stored verbatim - no trimming or normalization. Equality
/// is reference-based; two entries with identical fields are not equal
/// unless they are the same reference.
/// </remarks>
public sealed class CombatLogEntry
{
    public SimTick Tick { get; }

    public CombatLogCategory Category { get; }

    public string EventType { get; }

    public string Message { get; }

    public CombatLogEntry(
        SimTick tick,
        CombatLogCategory category,
        string eventType,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Tick = tick;
        Category = category;
        EventType = eventType;
        Message = message;
    }
}
