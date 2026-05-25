using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable result of a future script routine selection: whether a routine was chosen, its
/// index, and the routine value.
/// </summary>
/// <remarks>
/// <see cref="None"/> indicates no selection (<see cref="RoutineIndex"/> == -1,
/// <see cref="Routine"/> == null). <see cref="Selected"/> carries a non-negative index and the
/// chosen <see cref="ScriptRoutine"/>. This type does not evaluate conditions, implement selection,
/// execute commands, mutate match state, call sensors, fire weapons, move tanks, log, record
/// replays, or integrate Godot.
/// </remarks>
public sealed class ScriptRoutineSelectionResult
{
    public bool HasSelection { get; }

    public int RoutineIndex { get; }

    public ScriptRoutine? Routine { get; }

    private ScriptRoutineSelectionResult(
        bool hasSelection,
        int routineIndex,
        ScriptRoutine? routine)
    {
        HasSelection = hasSelection;
        RoutineIndex = routineIndex;
        Routine = routine;
    }

    public static ScriptRoutineSelectionResult None()
    {
        return new ScriptRoutineSelectionResult(
            hasSelection: false,
            routineIndex: -1,
            routine: null);
    }

    public static ScriptRoutineSelectionResult Selected(
        int routineIndex,
        ScriptRoutine routine)
    {
        if (routineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index must not be negative.");
        }

        return new ScriptRoutineSelectionResult(
            hasSelection: true,
            routineIndex: routineIndex,
            routine: routine);
    }
}
