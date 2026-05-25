using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces debug text for a <see cref="MatchScriptRuntimeEvaluationRecording"/> by formatting each frame.
/// </summary>
/// <remarks>
/// <see cref="FormatLines"/> yields one line per frame via
/// <see cref="MatchScriptRuntimeEvaluationFrameTextFormatter.Format"/>.
/// <see cref="Format"/> joins those lines with <see cref="Environment.NewLine"/>.
/// This type does not record or play back frames, log, integrate diagnostics or UI, execute commands,
/// generate match, sensor, weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationRecordingTextFormatter
{
    public static IReadOnlyList<string> FormatLines(
        MatchScriptRuntimeEvaluationRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);

        string[] lines = recording.Frames
            .Select(MatchScriptRuntimeEvaluationFrameTextFormatter.Format)
            .ToArray();

        return Array.AsReadOnly(lines);
    }

    public static string Format(
        MatchScriptRuntimeEvaluationRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);

        return string.Join(Environment.NewLine, FormatLines(recording));
    }
}
