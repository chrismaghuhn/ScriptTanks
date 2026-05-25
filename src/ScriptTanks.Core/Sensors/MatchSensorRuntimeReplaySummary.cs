using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable summary of metadata for a sensor-runtime replay recording.
/// </summary>
/// <remarks>
/// Pure summary data carrier for future sensor-runtime replay tooling: frame count, ticks
/// executed, initial and final simulation ticks, whether the match ended, and end reason.
/// This type does not inspect replay frames, generate summaries from recordings, record or play
/// back replays, serialize or compress data, log, diagnose, execute AI or scripts, or integrate
/// Godot.
/// </remarks>
public sealed class MatchSensorRuntimeReplaySummary
{
    public int FrameCount { get; }

    public int TicksExecuted { get; }

    public SimTick InitialTick { get; }

    public SimTick FinalTick { get; }

    public bool DidEnd { get; }

    public MatchEndReason EndReason { get; }

    public MatchSensorRuntimeReplaySummary(
        int frameCount,
        int ticksExecuted,
        SimTick initialTick,
        SimTick finalTick,
        bool didEnd,
        MatchEndReason endReason)
    {
        if (frameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameCount),
                frameCount,
                "Frame count must be positive.");
        }

        if (ticksExecuted < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticksExecuted),
                ticksExecuted,
                "Ticks executed must not be negative.");
        }

        if (finalTick.Value < initialTick.Value)
        {
            throw new ArgumentException(
                "Final tick must not be earlier than the initial tick.",
                nameof(finalTick));
        }

        if (!didEnd && endReason != MatchEndReason.None)
        {
            throw new ArgumentException(
                "A not-ended summary must use EndReason.None.",
                nameof(endReason));
        }

        if (didEnd && endReason == MatchEndReason.None)
        {
            throw new ArgumentException(
                "An ended summary must not use EndReason.None.",
                nameof(endReason));
        }

        FrameCount = frameCount;
        TicksExecuted = ticksExecuted;
        InitialTick = initialTick;
        FinalTick = finalTick;
        DidEnd = didEnd;
        EndReason = endReason;
    }
}
