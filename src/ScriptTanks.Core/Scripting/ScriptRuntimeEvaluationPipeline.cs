using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure composer that evaluates one <see cref="ScriptProgram"/> against a precomputed
/// <see cref="ScriptEvaluationContext"/> and packages the outcome as a <see cref="ScriptRuntimeEvaluationRecord"/>.
/// </summary>
/// <remarks>
/// Delegates decision logic to <see cref="ScriptRuntimeDecisionPipeline.Evaluate"/> and associates the
/// supplied <see cref="SimTick"/> and tank index with the result. This type does not execute commands,
/// generate match, sensor, weapon, or movement requests, mutate match state, log, run diagnostics,
/// record replays, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeEvaluationPipeline
{
    public static ScriptRuntimeEvaluationRecord Evaluate(
        SimTick tick,
        int tankIndex,
        ScriptProgram program,
        ScriptEvaluationContext context)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(program);

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                context);

        return new ScriptRuntimeEvaluationRecord(
            tick,
            tankIndex,
            program,
            context,
            result);
    }
}
