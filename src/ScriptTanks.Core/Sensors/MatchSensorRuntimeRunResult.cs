using System;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable snapshot summarizing the outcome of a future sensor-runtime match run:
/// final runtime state, how many ticks were executed, and the next scheduled scan index.
/// </summary>
/// <remarks>
/// This type is a pure data carrier for planned runner layers.
/// <see cref="FinalRuntime"/> is the closing snapshot of the run;
/// <see cref="TicksExecuted"/> and <see cref="NextScheduledScanIndex"/> are supplied by
/// future runner or scheduled-tick composition logic. This type does not execute ticks,
/// evaluate end conditions, validate schedule indices against list length, record replay,
/// log, diagnose, execute AI or scripts, or integrate with Godot.
/// </remarks>
public sealed class MatchSensorRuntimeRunResult
{
    public MatchSensorRuntimeState FinalRuntime { get; }

    public int TicksExecuted { get; }

    public int NextScheduledScanIndex { get; }

    public MatchSensorRuntimeRunResult(
        MatchSensorRuntimeState finalRuntime,
        int ticksExecuted,
        int nextScheduledScanIndex)
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

        FinalRuntime = finalRuntime;
        TicksExecuted = ticksExecuted;
        NextScheduledScanIndex = nextScheduledScanIndex;
    }
}
