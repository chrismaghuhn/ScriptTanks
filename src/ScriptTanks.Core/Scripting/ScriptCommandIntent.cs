using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable command-intent result for future script command translation: either no intent or a
/// selected command with its routine index.
/// </summary>
/// <remarks>
/// <see cref="None"/> represents no command intent and uses <see cref="RoutineIndex"/> == -1.
/// <see cref="FromDecision"/> converts a <see cref="ScriptRoutineDecision"/> into intent by
/// exposing the selected command when present. This type does not execute commands, translate
/// commands into match, sensor, weapon, or movement requests, evaluate conditions, mutate match
/// state, call sensors, fire weapons, move tanks, log, record replays, or integrate Godot.
/// </remarks>
public sealed class ScriptCommandIntent
{
    public bool HasIntent { get; }

    public int RoutineIndex { get; }

    public ScriptCommand? Command { get; }

    private ScriptCommandIntent(
        bool hasIntent,
        int routineIndex,
        ScriptCommand? command)
    {
        HasIntent = hasIntent;
        RoutineIndex = routineIndex;
        Command = command;
    }

    public static ScriptCommandIntent None()
    {
        return new ScriptCommandIntent(
            hasIntent: false,
            routineIndex: -1,
            command: null);
    }

    public static ScriptCommandIntent FromDecision(
        ScriptRoutineDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        if (!decision.HasCommand)
        {
            return None();
        }

        return new ScriptCommandIntent(
            hasIntent: true,
            routineIndex: decision.RoutineIndex,
            command: decision.Command);
    }
}
