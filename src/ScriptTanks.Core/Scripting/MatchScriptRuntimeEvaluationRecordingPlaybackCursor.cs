using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable playback cursor over the frames of a <see cref="MatchScriptRuntimeEvaluationRecording"/>.
/// </summary>
/// <remarks>
/// Points at one frame by collection index (same index as <see cref="MatchScriptRuntimeEvaluationRecording.GetFrameAtIndex"/>),
/// not by <see cref="MatchScriptRuntimeEvaluationFrame.FrameIndex"/>. Does not mutate the recording or frames,
/// <see cref="MoveNext"/> and <see cref="MovePrevious"/> clamp at the ends by returning the same cursor instance,
/// does not run playback timing, execute commands, generate match, sensor, weapon, or movement requests,
/// mutate match state, log, diagnose, or integrate Godot.
/// </remarks>
public sealed class MatchScriptRuntimeEvaluationRecordingPlaybackCursor
{
    public MatchScriptRuntimeEvaluationRecording Recording { get; }

    public int Position { get; }

    public MatchScriptRuntimeEvaluationFrame CurrentFrame =>
        Recording.GetFrameAtIndex(Position);

    public bool IsFirst => Position == 0;

    public bool IsLast => Position == Recording.Count - 1;

    public MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
        MatchScriptRuntimeEvaluationRecording recording,
        int position)
    {
        ArgumentNullException.ThrowIfNull(recording);

        if (position < 0 || position >= recording.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                position,
                "Position must be within the recording frame index range.");
        }

        Recording = recording;
        Position = position;
    }

    public MatchScriptRuntimeEvaluationRecordingPlaybackCursor MoveTo(
        int position)
    {
        return new MatchScriptRuntimeEvaluationRecordingPlaybackCursor(
            Recording,
            position);
    }

    public MatchScriptRuntimeEvaluationRecordingPlaybackCursor MoveNext()
    {
        if (IsLast)
        {
            return this;
        }

        return MoveTo(Position + 1);
    }

    public MatchScriptRuntimeEvaluationRecordingPlaybackCursor MovePrevious()
    {
        if (IsFirst)
        {
            return this;
        }

        return MoveTo(Position - 1);
    }
}
