using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable replay-frame data carrier for a sensor-runtime snapshot.
/// </summary>
/// <remarks>
/// Each frame stores a <see cref="FrameIndex"/> and a full
/// <see cref="MatchSensorRuntimeState"/> in <see cref="State"/>. This type does not
/// record frames, play them back, serialize or compress data, log, diagnose, execute
/// AI or scripts, or integrate Godot.
/// </remarks>
public sealed class MatchSensorRuntimeReplayFrame
{
    public int FrameIndex { get; }

    public MatchSensorRuntimeState State { get; }

    public MatchSensorRuntimeReplayFrame(int frameIndex, MatchSensorRuntimeState state)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                frameIndex,
                "Frame index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(state);

        FrameIndex = frameIndex;
        State = state;
    }
}
