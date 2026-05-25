using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable ordered container of <see cref="ScriptRoutine"/> entries for a tank script.
/// </summary>
/// <remarks>
/// Pure data container: routines are stored in deterministic order exactly as supplied.
/// This type does not parse source text, select routines, evaluate conditions, execute commands,
/// mutate match state, call sensors, fire weapons, move tanks, log, record replays, or integrate
/// Godot.
/// </remarks>
public sealed class ScriptProgram
{
    public IReadOnlyList<ScriptRoutine> Routines { get; }

    public int Count => Routines.Count;

    public ScriptProgram(IEnumerable<ScriptRoutine> routines)
    {
        ArgumentNullException.ThrowIfNull(routines);

        ScriptRoutine[] copied = routines.ToArray();

        if (copied.Length == 0)
        {
            throw new ArgumentException(
                "Script program must contain at least one routine.",
                nameof(routines));
        }

        Routines = Array.AsReadOnly(copied);
    }

    public ScriptRoutine GetRoutineAtIndex(int routineIndex)
    {
        ValidateIndex(routineIndex);
        return Routines[routineIndex];
    }

    private void ValidateIndex(int routineIndex)
    {
        if (routineIndex < 0 || routineIndex >= Routines.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index is outside the script program range.");
        }
    }
}
