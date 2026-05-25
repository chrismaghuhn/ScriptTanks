using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure helper that evaluates a <see cref="MatchScriptRuntimeState"/> for one tick and wraps the
/// result in a <see cref="MatchScriptRuntimeEvaluationFrame"/>.
/// </summary>
/// <remarks>
/// Composes <see cref="MatchScriptRuntimeEvaluationPipeline.Evaluate"/> with
/// <see cref="MatchScriptRuntimeEvaluationFrame"/>. This type does not run a recording loop, play back
/// frames, integrate with match state, execute commands, generate match, sensor, weapon, or movement
/// requests, mutate match state or the supplied runtime state, log, run diagnostics, record replays,
/// or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationRecorder
{
    public static MatchScriptRuntimeEvaluationFrame RecordFrame(
        int frameIndex,
        SimTick tick,
        MatchScriptRuntimeState runtime)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                frameIndex,
                "Frame index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(runtime);

        ScriptRuntimeEvaluationTrace trace =
            MatchScriptRuntimeEvaluationPipeline.Evaluate(
                tick,
                runtime);

        return new MatchScriptRuntimeEvaluationFrame(
            frameIndex,
            tick,
            trace);
    }
}
