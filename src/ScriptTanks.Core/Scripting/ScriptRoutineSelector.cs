using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Selects the first script routine in a <see cref="ScriptProgram"/> whose condition holds for a
/// <see cref="ScriptEvaluationContext"/>.
/// </summary>
/// <remarks>
/// Routines are considered in program order; the first whose condition evaluates to true is
/// returned. Condition evaluation is delegated to <see cref="ScriptConditionEvaluator"/>. This
/// type does not execute commands, mutate match state, call sensors, fire weapons, move tanks,
/// log, record replays, or integrate Godot.
/// </remarks>
public static class ScriptRoutineSelector
{
    public static ScriptRoutineSelectionResult SelectFirstValid(
        ScriptProgram program,
        ScriptEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(program);

        for (int i = 0; i < program.Count; i++)
        {
            ScriptRoutine routine = program.GetRoutineAtIndex(i);

            bool conditionResult = ScriptConditionEvaluator.Evaluate(
                routine.Condition,
                context);

            if (conditionResult)
            {
                return ScriptRoutineSelectionResult.Selected(i, routine);
            }
        }

        return ScriptRoutineSelectionResult.None();
    }
}
