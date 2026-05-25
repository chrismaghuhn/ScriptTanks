using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable snapshot summarizing a future sensor-runtime run together with recorded replay
/// frames.
/// </summary>
/// <remarks>
/// <see cref="FinalRuntime"/>, <see cref="TicksExecuted"/>,
/// <see cref="NextScheduledScanIndex"/>, and <see cref="EndCondition"/> are supplied by future
/// runner logic; <see cref="ReplayFrames"/> is supplied by future recorder logic. The replay
/// frame sequence is defensively copied. This type does not record frames, play them back,
/// serialize or compress data, validate frame continuity against ticks or final runtime, log,
/// diagnose, execute AI or scripts, or integrate Godot.
/// </remarks>
public sealed class MatchSensorRuntimeRunWithReplayResult
{
    public MatchSensorRuntimeState FinalRuntime { get; }

    public int TicksExecuted { get; }

    public int NextScheduledScanIndex { get; }

    public MatchEndConditionResult EndCondition { get; }

    public IReadOnlyList<MatchSensorRuntimeReplayFrame> ReplayFrames { get; }

    public MatchSensorRuntimeRunWithReplayResult(
        MatchSensorRuntimeState finalRuntime,
        int ticksExecuted,
        int nextScheduledScanIndex,
        MatchEndConditionResult endCondition,
        IEnumerable<MatchSensorRuntimeReplayFrame> replayFrames)
    {
        ArgumentNullException.ThrowIfNull(finalRuntime);

        if (ticksExecuted < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticksExecuted),
                ticksExecuted,
                "Ticks executed must not be negative.");
        }

        if (nextScheduledScanIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextScheduledScanIndex),
                nextScheduledScanIndex,
                "Next scheduled sensor scan index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(endCondition);
        ArgumentNullException.ThrowIfNull(replayFrames);

        MatchSensorRuntimeReplayFrame[] frames = replayFrames.ToArray();

        if (frames.Length == 0)
        {
            throw new ArgumentException(
                "Replay frames must not be empty.",
                nameof(replayFrames));
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] is null)
            {
                throw new ArgumentException(
                    "Replay frames must not contain null elements.",
                    nameof(replayFrames));
            }
        }

        FinalRuntime = finalRuntime;
        TicksExecuted = ticksExecuted;
        NextScheduledScanIndex = nextScheduledScanIndex;
        EndCondition = endCondition;
        ReplayFrames = Array.AsReadOnly(frames);
    }
}
