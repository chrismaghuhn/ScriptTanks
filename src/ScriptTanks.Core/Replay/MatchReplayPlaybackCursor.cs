using System;
using System.Collections.Generic;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Pure immutable cursor for navigating a <see cref="MatchReplayRecording"/>
/// by list position and frame index.
/// </summary>
/// <remarks>
/// MVP playback navigation only. This type does not perform timing,
/// autoplay, interpolation, UI, serialization, logging, recorder
/// integration, runner integration, or Godot-facing behavior. It does not
/// mutate or clone the wrapped <see cref="MatchReplayRecording"/> or its
/// <see cref="MatchReplayFrame"/> entries.
/// </remarks>
public sealed class MatchReplayPlaybackCursor
{
    public MatchReplayRecording Recording { get; }

    public int Position { get; }

    public MatchReplayFrame CurrentFrame => Recording.Frames[Position];

    public bool IsFirst => Position == 0;

    public bool IsLast => Position == Recording.Frames.Count - 1;

    public MatchReplayPlaybackCursor(MatchReplayRecording recording, int position)
    {
        ArgumentNullException.ThrowIfNull(recording);

        if (position < 0 || position >= recording.Frames.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                position,
                "Position must be within the recording frame range.");
        }

        Recording = recording;
        Position = position;
    }

    public static MatchReplayPlaybackCursor Start(MatchReplayRecording recording)
        => new MatchReplayPlaybackCursor(recording, 0);

    public MatchReplayPlaybackCursor MoveNext()
        => IsLast ? this : new MatchReplayPlaybackCursor(Recording, Position + 1);

    public MatchReplayPlaybackCursor MovePrevious()
        => IsFirst ? this : new MatchReplayPlaybackCursor(Recording, Position - 1);

    public MatchReplayPlaybackCursor MoveLast()
        => IsLast ? this : new MatchReplayPlaybackCursor(Recording, Recording.Frames.Count - 1);

    public MatchReplayPlaybackCursor SeekToPosition(int position)
        => new MatchReplayPlaybackCursor(Recording, position);

    public MatchReplayPlaybackCursor SeekToFrameIndex(int frameIndex)
    {
        for (int i = 0; i < Recording.Frames.Count; i++)
        {
            if (Recording.Frames[i].FrameIndex == frameIndex)
            {
                return new MatchReplayPlaybackCursor(Recording, i);
            }
        }

        throw new KeyNotFoundException(
            $"No replay frame with FrameIndex '{frameIndex}' exists in this recording.");
    }
}
