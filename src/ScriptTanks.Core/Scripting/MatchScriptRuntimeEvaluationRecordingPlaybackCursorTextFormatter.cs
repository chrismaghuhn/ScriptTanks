using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure text formatter for <see cref="MatchScriptRuntimeEvaluationRecordingPlaybackCursor"/> values.
/// </summary>
/// <remarks>
/// Includes cursor position, recording count, first/last flags, current frame index and tick, and formatted
/// current frame text by delegating frame layout to <see cref="MatchScriptRuntimeEvaluationFrameTextFormatter.Format"/>.
/// Does not run playback, log, integrate diagnostics or UI, execute commands, generate match, sensor, weapon, or
/// movement requests, mutate match state, or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationRecordingPlaybackCursorTextFormatter
{
    /// <summary>
    /// Returns an invariant-culture description of the cursor, including the formatted current frame.
    /// </summary>
    public static string Format(
        MatchScriptRuntimeEvaluationRecordingPlaybackCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        string frameText =
            MatchScriptRuntimeEvaluationFrameTextFormatter.Format(
                cursor.CurrentFrame);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"MatchScriptRuntimeEvaluationRecordingPlaybackCursor {{ Position = {cursor.Position}, Recording.Count = {cursor.Recording.Count}, IsFirst = {cursor.IsFirst}, IsLast = {cursor.IsLast}, CurrentFrame.FrameIndex = {cursor.CurrentFrame.FrameIndex}, CurrentFrame.Tick = {cursor.CurrentFrame.Tick}, CurrentFrame = {frameText} }}");
    }
}
