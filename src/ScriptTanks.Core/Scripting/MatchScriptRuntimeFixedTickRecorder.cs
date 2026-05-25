using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure fixed-tick script runtime evaluation recorder for match-level frames across a contiguous tick range.
/// </summary>
/// <remarks>
/// Produces one <see cref="MatchScriptRuntimeEvaluationFrame"/> per tick via
/// <see cref="MatchScriptRuntimeEvaluationRecorder.RecordFrame"/>, then builds a
/// <see cref="MatchScriptRuntimeEvaluationRecording"/>, summary, and
/// <see cref="MatchScriptRuntimeEvaluationRecordedRunResult"/>. Does not advance or inspect match state,
/// schedule script work, execute commands, generate match, sensor, weapon, or movement requests, mutate the
/// supplied <see cref="MatchScriptRuntimeState"/>, log, run diagnostics, integrate replay playback, or integrate
/// Godot.
/// </remarks>
public static class MatchScriptRuntimeFixedTickRecorder
{
    /// <summary>
    /// Records <paramref name="tickCount"/> frames at ticks <c>initialTick + i</c> for <c>i</c> from 0 to
    /// <c>tickCount - 1</c>, using frame indexes matching <c>i</c>.
    /// </summary>
    public static MatchScriptRuntimeEvaluationRecordedRunResult RecordForTicks(
        SimTick initialTick,
        int tickCount,
        MatchScriptRuntimeState runtime)
    {
        if (tickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "Tick count must be positive.");
        }

        ArgumentNullException.ThrowIfNull(runtime);

        var frames = new MatchScriptRuntimeEvaluationFrame[tickCount];
        for (int i = 0; i < tickCount; i++)
        {
            SimTick tick = new SimTick(initialTick.Value + i);
            MatchScriptRuntimeEvaluationFrame frame =
                MatchScriptRuntimeEvaluationRecorder.RecordFrame(
                    frameIndex: i,
                    tick,
                    runtime);

            frames[i] = frame;
        }

        MatchScriptRuntimeEvaluationRecording recording =
            new MatchScriptRuntimeEvaluationRecording(frames);

        MatchScriptRuntimeEvaluationRecordingSummary summary =
            MatchScriptRuntimeEvaluationRecordingSummaryFactory.Create(recording);

        return new MatchScriptRuntimeEvaluationRecordedRunResult(
            recording,
            summary);
    }
}
