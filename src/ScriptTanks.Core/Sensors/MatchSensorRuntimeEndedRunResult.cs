using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable snapshot summarizing the outcome of a future sensor-runtime run that stops
/// on match end conditions: final runtime, tick count, next scheduled scan index, and the
/// evaluated end condition.
/// </summary>
/// <remarks>
/// This is a pure result container for future sensor-runtime run-until-ended loops.
/// <see cref="FinalRuntime"/> is the final runtime snapshot;
/// <see cref="TicksExecuted"/> is supplied by future runner logic;
/// <see cref="NextScheduledScanIndex"/> is supplied by future scheduled-tick pipeline or
/// runner logic; <see cref="EndCondition"/> is supplied by future end-condition evaluation
/// logic. This type does not execute ticks, evaluate end conditions, or validate against a
/// schedule. It does not log, replay, diagnose, execute AI or scripts, or integrate Godot.
/// </remarks>
public sealed class MatchSensorRuntimeEndedRunResult
{
    public MatchSensorRuntimeState FinalRuntime { get; }

    public int TicksExecuted { get; }

    public int NextScheduledScanIndex { get; }

    public MatchEndConditionResult EndCondition { get; }

    public MatchSensorRuntimeEndedRunResult(
        MatchSensorRuntimeState finalRuntime,
        int ticksExecuted,
        int nextScheduledScanIndex,
        MatchEndConditionResult endCondition)
    {
        ArgumentNullException.ThrowIfNull(finalRuntime);
        ArgumentNullException.ThrowIfNull(endCondition);

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
        EndCondition = endCondition;
    }
}
