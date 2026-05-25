using System;
using System.Collections.Generic;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure batch composer that evaluates many <see cref="ScriptRuntimeEvaluationRequest"/> values for one
/// shared <see cref="SimTick"/>.
/// </summary>
/// <remarks>
/// Preserves request list order in the returned <see cref="ScriptRuntimeEvaluationTrace"/> and delegates
/// each evaluation to <see cref="ScriptRuntimeEvaluationPipeline.Evaluate"/>. This type does not execute
/// commands, generate match, sensor, weapon, or movement requests, mutate match state, log, run
/// diagnostics, record replays, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeEvaluationBatchPipeline
{
    public static ScriptRuntimeEvaluationTrace Evaluate(
        SimTick tick,
        IReadOnlyList<ScriptRuntimeEvaluationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        ScriptRuntimeEvaluationRecord[] records =
            new ScriptRuntimeEvaluationRecord[requests.Count];

        for (int i = 0; i < requests.Count; i++)
        {
            ScriptRuntimeEvaluationRequest request = requests[i]
                ?? throw new ArgumentException(
                    "Script runtime evaluation request list must not contain null entries.",
                    nameof(requests));

            records[i] = ScriptRuntimeEvaluationPipeline.Evaluate(
                tick,
                request.TankIndex,
                request.Program,
                request.Context);
        }

        return new ScriptRuntimeEvaluationTrace(records);
    }
}
