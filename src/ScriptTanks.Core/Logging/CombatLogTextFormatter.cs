using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Pure formatter for immutable combat-log snapshots.
/// </summary>
/// <remarks>
/// This type only converts existing <see cref="CombatLog"/> entries into
/// deterministic human-readable text. It does not emit logs, store logs
/// globally, write files, write to the console, serialize data, integrate
/// with runners, replay recorders, pipelines, UI, or Godot. Output uses
/// <see cref="CultureInfo.InvariantCulture"/> for stable cross-locale
/// behavior and preserves entry order verbatim.
/// </remarks>
public static class CombatLogTextFormatter
{
    public static IReadOnlyList<string> FormatLines(CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        string[] lines = log.Entries
            .Select(FormatEntry)
            .ToArray();

        return Array.AsReadOnly(lines);
    }

    public static string Format(CombatLog log)
    {
        return string.Join(Environment.NewLine, FormatLines(log));
    }

    private static string FormatEntry(CombatLogEntry entry)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "[{0:D4}] {1}/{2}: {3}",
            entry.Tick.Value,
            entry.Category,
            entry.EventType,
            entry.Message);
    }
}
