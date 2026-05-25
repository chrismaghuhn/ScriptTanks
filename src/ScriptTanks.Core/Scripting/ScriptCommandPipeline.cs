using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure script command pipeline that combines routine decision, command-intent projection, and
/// status-only translation into a <see cref="ScriptCommandTranslationResult"/>.
/// </summary>
/// <remarks>
/// Selects intent via <see cref="ScriptDecisionPipeline.Decide"/>, projects through
/// <see cref="ScriptCommandIntent.FromDecision"/>, then translates with the status-only
/// <see cref="ScriptCommandTranslator"/>. This type does not execute commands, generate match,
/// sensor, weapon, or movement requests, mutate match state, call sensors, fire weapons, move tanks,
/// log, record replays, or integrate Godot.
/// </remarks>
public static class ScriptCommandPipeline
{
    public static ScriptCommandTranslationResult DecideAndTranslate(
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

        return ScriptCommandTranslator.Translate(intent);
    }
}
