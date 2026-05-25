using System;
using System.Collections.Generic;
using System.Globalization;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Diagnostics;

/// <summary>
/// Pure formatter for immutable match debug snapshots.
/// </summary>
/// <remarks>
/// This type only converts existing <see cref="MatchDebugSnapshot"/> data into
/// deterministic human-readable text. It does not emit logs, store logs
/// globally, write files, write to the console, serialize data, integrate with
/// runners, replay recorders, pipelines, UI, or Godot. Optional combat-log
/// lines are formatted via <see cref="CombatLogTextFormatter"/> and indented
/// by two spaces; entry order is preserved verbatim. Output uses
/// <see cref="CultureInfo.InvariantCulture"/> for stable cross-locale behavior.
/// </remarks>
public static class MatchDebugSnapshotTextFormatter
{
    public static IReadOnlyList<string> FormatLines(MatchDebugSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<string> lines = new List<string>
        {
            "Debug Snapshot",
            string.Format(
                CultureInfo.InvariantCulture,
                "Tick: {0}",
                snapshot.State.CurrentTick.Value),
            string.Format(
                CultureInfo.InvariantCulture,
                "Arena: {0}",
                snapshot.State.Arena.Id),
            string.Format(
                CultureInfo.InvariantCulture,
                "ReplayFrame: {0}",
                snapshot.ReplayFrame is null
                    ? "none"
                    : snapshot.ReplayFrame.FrameIndex.ToString(CultureInfo.InvariantCulture)),
            string.Format(
                CultureInfo.InvariantCulture,
                "Tanks: {0}",
                snapshot.State.Tanks.Count),
            string.Format(
                CultureInfo.InvariantCulture,
                "Projectiles: {0}",
                snapshot.State.Projectiles.Count),
            string.Format(
                CultureInfo.InvariantCulture,
                "LogEntries: {0}",
                snapshot.Log?.Entries.Count ?? 0),
        };

        if (snapshot.Log is not null)
        {
            lines.Add("Log:");

            foreach (string logLine in CombatLogTextFormatter.FormatLines(snapshot.Log))
            {
                lines.Add("  " + logLine);
            }
        }

        return Array.AsReadOnly(lines.ToArray());
    }

    public static string Format(MatchDebugSnapshot snapshot)
    {
        return string.Join(Environment.NewLine, FormatLines(snapshot));
    }
}
