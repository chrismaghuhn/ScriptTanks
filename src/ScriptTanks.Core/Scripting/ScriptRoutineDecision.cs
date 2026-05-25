using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable decision describing whether a script command should run and from which routine it
/// originates.
/// </summary>
/// <remarks>
/// <see cref="None"/> indicates no command (<see cref="RoutineIndex"/> == -1, null routine and
/// command). <see cref="FromSelection"/> maps a <see cref="ScriptRoutineSelectionResult"/> to
/// command intent via the selected routine's <see cref="ScriptRoutine.Command"/>. This type does
/// not execute commands, select routines, evaluate conditions, mutate match state, call sensors,
/// fire weapons, move tanks, log, record replays, or integrate Godot.
/// </remarks>
public sealed class ScriptRoutineDecision
{
    public bool HasCommand { get; }

    public int RoutineIndex { get; }

    public ScriptRoutine? Routine { get; }

    public ScriptCommand? Command { get; }

    private ScriptRoutineDecision(
        bool hasCommand,
        int routineIndex,
        ScriptRoutine? routine,
        ScriptCommand? command)
    {
        HasCommand = hasCommand;
        RoutineIndex = routineIndex;
        Routine = routine;
        Command = command;
    }

    public static ScriptRoutineDecision None()
    {
        return new ScriptRoutineDecision(
            hasCommand: false,
            routineIndex: -1,
            routine: null,
            command: null);
    }

    public static ScriptRoutineDecision FromSelection(
        ScriptRoutineSelectionResult selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (!selection.HasSelection)
        {
            return None();
        }

        ScriptRoutine routine = selection.Routine!.Value;

        return new ScriptRoutineDecision(
            hasCommand: true,
            routineIndex: selection.RoutineIndex,
            routine: routine,
            command: routine.Command);
    }
}
