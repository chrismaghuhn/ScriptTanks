using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Immutable container for a deterministic sequence of full-state replay frames.
/// </summary>
/// <remarks>
/// MVP replay recordings intentionally store full simulation-state frames for
/// clarity and reliability. This type is a pure data carrier; it does not
/// perform recording, playback, compression, delta encoding, serialization,
/// logging, runner integration, or Godot-facing behavior.
/// </remarks>
public sealed class MatchReplayRecording
{
    public IReadOnlyList<MatchReplayFrame> Frames { get; }

    public MatchReplayRecording(IEnumerable<MatchReplayFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        MatchReplayFrame[] copiedFrames = frames.ToArray();

        if (copiedFrames.Length == 0)
        {
            throw new ArgumentException(
                "A replay recording must contain at least one frame.",
                nameof(frames));
        }

        int previousFrameIndex = -1;
        for (int i = 0; i < copiedFrames.Length; i++)
        {
            MatchReplayFrame? frame = copiedFrames[i];
            if (frame is null)
            {
                throw new ArgumentException(
                    "Replay recordings must not contain null frames.",
                    nameof(frames));
            }

            if (frame.FrameIndex <= previousFrameIndex)
            {
                throw new ArgumentException(
                    "Replay frame indices must be strictly increasing and unique.",
                    nameof(frames));
            }

            previousFrameIndex = frame.FrameIndex;
        }

        Frames = Array.AsReadOnly(copiedFrames);
    }
}
