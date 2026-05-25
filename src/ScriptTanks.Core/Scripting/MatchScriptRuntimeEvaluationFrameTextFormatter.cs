using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces a stable debug representation of a <see cref="MatchScriptRuntimeEvaluationFrame"/>.
/// </summary>
/// <remarks>
/// Includes frame index, tick, trace count, and the formatted <see cref="ScriptRuntimeEvaluationTrace"/>
/// via <see cref="ScriptRuntimeEvaluationTraceTextFormatter.Format"/>. When the trace contains multiple
/// records, the embedded trace text may include line breaks from that formatter. This type does not
/// log, integrate diagnostics or UI, execute commands, generate match, sensor, weapon, or movement
/// requests, mutate match state, or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationFrameTextFormatter
{
    public static string Format(
        MatchScriptRuntimeEvaluationFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        string traceText = ScriptRuntimeEvaluationTraceTextFormatter.Format(
            frame.Trace);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"MatchScriptRuntimeEvaluationFrame {{ FrameIndex = {frame.FrameIndex}, Tick = {frame.Tick}, Trace.Count = {frame.Trace.Count}, Trace = {traceText} }}");
    }
}
