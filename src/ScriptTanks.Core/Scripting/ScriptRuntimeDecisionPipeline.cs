using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure script runtime decision composer that bundles routine selection, command intent, and
/// status-only translation into a <see cref="ScriptRuntimeDecisionResult"/>.
/// </summary>
/// <remarks>
/// Delegates to <see cref="ScriptDecisionPipeline.Decide"/>, projects through
/// <see cref="ScriptCommandIntent.FromDecision"/>, translates via <see cref="ScriptCommandTranslator"/>,
/// and packages all three stages in <see cref="ScriptRuntimeDecisionResult"/>. This type does not
/// execute commands, generate match, sensor, weapon, or movement requests, mutate match state, log,
/// record replays, run diagnostics, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeDecisionPipeline
{
    public static ScriptRuntimeDecisionResult Evaluate(
        ScriptProgram program,
        ScriptEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(program);

        ScriptRoutineDecision decision =
            ScriptDecisionPipeline.Decide(
                program,
                context);

        ScriptCommandIntent intent =
            ScriptCommandIntent.FromDecision(decision);

        ScriptCommandTranslationResult translation =
            ScriptCommandTranslator.Translate(intent);

        return new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);
    }
}
