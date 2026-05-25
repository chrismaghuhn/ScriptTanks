using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable ordered sequence of <see cref="MatchScriptRuntimeEvaluationFrame"/> values for future
/// match-level script runtime evaluation recording.
/// </summary>
/// <remarks>
/// Defensively copies the supplied frames and requires at least one entry. This type does not
/// validate frame index continuity, tick ordering, record or play back frames, execute commands,
/// generate match, sensor, weapon, or movement requests, mutate match state, log, diagnose, or
/// integrate Godot. <see cref="GetFrameAtIndex"/> uses the collection index, not
/// <see cref="MatchScriptRuntimeEvaluationFrame.FrameIndex"/>.
/// </remarks>
public sealed class MatchScriptRuntimeEvaluationRecording
{
    private readonly MatchScriptRuntimeEvaluationFrame[] _frames;

    public IReadOnlyList<MatchScriptRuntimeEvaluationFrame> Frames =>
        Array.AsReadOnly(_frames);

    public int Count => Frames.Count;

    public MatchScriptRuntimeEvaluationRecording(
        IEnumerable<MatchScriptRuntimeEvaluationFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        MatchScriptRuntimeEvaluationFrame[] copy = frames.ToArray();

        if (copy.Length == 0)
        {
            throw new ArgumentException(
                "Frame sequence must contain at least one frame.",
                nameof(frames));
        }

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Frame sequence must not contain null elements.",
                    nameof(frames));
            }
        }

        _frames = copy;
    }

    public MatchScriptRuntimeEvaluationFrame GetFrameAtIndex(
        int frameIndex)
    {
        ValidateIndex(frameIndex);

        return _frames[frameIndex];
    }

    private void ValidateIndex(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _frames.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                frameIndex,
                "Frame index is outside the match script runtime evaluation recording range.");
        }
    }
}
