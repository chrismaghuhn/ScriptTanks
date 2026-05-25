using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Stores a full match-state snapshot for one deterministic replay frame.
/// </summary>
/// <remarks>
/// MVP replay frames intentionally store the full simulation state per
/// frame for clarity and reliability. This type is a pure data carrier;
/// it does not perform recording, playback, compression, delta encoding,
/// serialization, logging, or Godot-facing behavior. It does not mutate
/// or clone the wrapped <see cref="MatchState"/> - that type is already
/// snapshot-style immutable.
/// </remarks>
public sealed class MatchReplayFrame
{
    public int FrameIndex { get; }

    public MatchState State { get; }

    public MatchReplayFrame(int frameIndex, MatchState state)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                frameIndex,
                "FrameIndex must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(state);

        FrameIndex = frameIndex;
        State = state;
    }
}
