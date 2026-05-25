using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure match-level composer that evaluates all script runtime requests in a
/// <see cref="MatchScriptRuntimeState"/> for one <see cref="SimTick"/>.
/// </summary>
/// <remarks>
/// Delegates to <see cref="ScriptRuntimeEvaluationBatchPipeline.Evaluate"/>. This type does not
/// validate against match state, execute commands, generate match, sensor, weapon, or movement
/// requests, mutate match state or the supplied runtime state, log, run diagnostics, record replays,
/// or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationPipeline
{
    public static ScriptRuntimeEvaluationTrace Evaluate(
        SimTick tick,
        MatchScriptRuntimeState runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        return ScriptRuntimeEvaluationBatchPipeline.Evaluate(
            tick,
            runtime.Requests);
    }
}
