using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Composes routine selection and command-intent projection into a single <see cref="ScriptRoutineDecision"/>.
/// </summary>
/// <remarks>
/// Delegates to <see cref="ScriptRoutineSelector.SelectFirstValid"/> then
/// <see cref="ScriptRoutineDecision.FromSelection"/>. This type does not execute commands, mutate
/// match state, call sensors, fire weapons, move tanks, log, record replays, or integrate Godot.
/// </remarks>
public static class ScriptDecisionPipeline
{
    public static ScriptRoutineDecision Decide(
        ScriptProgram program,
        ScriptEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(program);

        ScriptRoutineSelectionResult selection =
            ScriptRoutineSelector.SelectFirstValid(
                program,
                context);

        return ScriptRoutineDecision.FromSelection(selection);
    }
}
