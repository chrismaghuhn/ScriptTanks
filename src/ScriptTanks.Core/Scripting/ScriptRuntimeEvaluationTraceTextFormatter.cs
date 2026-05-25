using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces debug text for a <see cref="ScriptRuntimeEvaluationTrace"/> by formatting each record.
/// </summary>
/// <remarks>
/// <see cref="FormatLines"/> yields one line per record via
/// <see cref="ScriptRuntimeEvaluationRecordTextFormatter.Format"/>.
/// <see cref="Format"/> joins those lines with <see cref="Environment.NewLine"/>.
/// This type does not log, integrate diagnostics or UI, execute commands, generate match, sensor,
/// weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeEvaluationTraceTextFormatter
{
    public static IReadOnlyList<string> FormatLines(
        ScriptRuntimeEvaluationTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        string[] lines = trace.Records
            .Select(ScriptRuntimeEvaluationRecordTextFormatter.Format)
            .ToArray();

        return Array.AsReadOnly(lines);
    }

    public static string Format(
        ScriptRuntimeEvaluationTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        return string.Join(Environment.NewLine, FormatLines(trace));
    }
}
