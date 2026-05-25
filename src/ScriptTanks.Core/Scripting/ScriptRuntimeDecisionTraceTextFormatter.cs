using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces debug text for a <see cref="ScriptRuntimeDecisionTrace"/> by formatting each snapshot.
/// </summary>
/// <remarks>
/// <see cref="FormatLines"/> yields one line per snapshot via
/// <see cref="ScriptRuntimeDecisionSnapshotTextFormatter.Format"/>.
/// <see cref="Format"/> joins those lines with <see cref="Environment.NewLine"/>.
/// This type does not log, integrate diagnostics or UI, execute commands, generate match, sensor,
/// weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeDecisionTraceTextFormatter
{
    public static IReadOnlyList<string> FormatLines(
        ScriptRuntimeDecisionTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        string[] lines = trace.Snapshots
            .Select(ScriptRuntimeDecisionSnapshotTextFormatter.Format)
            .ToArray();

        return Array.AsReadOnly(lines);
    }

    public static string Format(
        ScriptRuntimeDecisionTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        return string.Join(Environment.NewLine, FormatLines(trace));
    }
}
