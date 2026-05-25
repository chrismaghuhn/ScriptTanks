using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Pure factory that projects a <see cref="MatchSensorRuntimeRunWithReplayResult"/> to a
/// <see cref="MatchSensorRuntimeReplaySummary"/>.
/// </summary>
/// <remarks>
/// Uses the first replay frame for <see cref="MatchSensorRuntimeReplaySummary.InitialTick"/>,
/// <see cref="MatchSensorRuntimeRunWithReplayResult.FinalRuntime"/> for
/// <see cref="MatchSensorRuntimeReplaySummary.FinalTick"/>, and
/// <see cref="MatchSensorRuntimeRunWithReplayResult.EndCondition"/> for
/// <see cref="MatchSensorRuntimeReplaySummary.DidEnd"/> and
/// <see cref="MatchSensorRuntimeReplaySummary.EndReason"/>. This type does not record or play back
/// replays, export data, serialize, validate frame continuity, log, diagnose, execute AI or
/// scripts, or integrate Godot.
/// </remarks>
public static class MatchSensorRuntimeReplaySummaryFactory
{
    public static MatchSensorRuntimeReplaySummary Create(
        MatchSensorRuntimeRunWithReplayResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        int frameCount = result.ReplayFrames.Count;
        int ticksExecuted = result.TicksExecuted;
        SimTick initialTick = result.ReplayFrames[0].State.State.CurrentTick;
        SimTick finalTick = result.FinalRuntime.State.CurrentTick;
        bool didEnd = result.EndCondition.IsEnded;
        MatchEndReason endReason = result.EndCondition.Reason;

        return new MatchSensorRuntimeReplaySummary(
            frameCount,
            ticksExecuted,
            initialTick,
            finalTick,
            didEnd,
            endReason);
    }
}
