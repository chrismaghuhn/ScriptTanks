using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure text formatter for <see cref="MatchScriptRuntimeEvaluationRecordedRunResult"/> values.
/// </summary>
/// <remarks>
/// Includes summary metadata and formatted recording text by delegating recording layout to
/// <see cref="MatchScriptRuntimeEvaluationRecordingTextFormatter.Format"/>. Does not record or play back frames,
/// log, integrate diagnostics or UI, execute commands, generate match, sensor, weapon, or movement requests,
/// mutate match state, or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationRecordedRunResultTextFormatter
{
    /// <summary>
    /// Returns an invariant-culture single-line description of the result, including the formatted recording.
    /// </summary>
    public static string Format(
        MatchScriptRuntimeEvaluationRecordedRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string recordingText =
            MatchScriptRuntimeEvaluationRecordingTextFormatter.Format(
                result.Recording);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"MatchScriptRuntimeEvaluationRecordedRunResult {{ Summary.FrameCount = {result.Summary.FrameCount}, Summary.InitialTick = {result.Summary.InitialTick}, Summary.FinalTick = {result.Summary.FinalTick}, Summary.TotalEvaluationRecords = {result.Summary.TotalEvaluationRecords}, Recording = {recordingText} }}");
    }
}
