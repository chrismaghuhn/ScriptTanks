using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Status-only script command translator skeleton that maps <see cref="ScriptCommandIntent"/> to
/// <see cref="ScriptCommandTranslationResult"/>.
/// </summary>
/// <remarks>
/// Recognizes current MVP <see cref="ScriptCommandType"/> values and returns a translation status.
/// <see cref="ScriptCommandTranslationStatus.Translated"/> only means the command was recognized by
/// this translator, not executed. This type does not generate concrete match, sensor, weapon, or
/// movement requests, execute commands, mutate match state, call sensors, fire weapons, move tanks,
/// log, record replays, or integrate Godot.
/// </remarks>
public static class ScriptCommandTranslator
{
    public static ScriptCommandTranslationResult Translate(
        ScriptCommandIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (!intent.HasIntent)
        {
            return ScriptCommandTranslationResult.NoIntent();
        }

        ScriptCommand command = intent.Command!.Value;

        return command.Type switch
        {
            ScriptCommandType.NoOp => ScriptCommandTranslationResult.Translated(
                intent.RoutineIndex,
                command,
                "No operation command translated."),
            ScriptCommandType.ScanEnemy => ScriptCommandTranslationResult.Translated(
                intent.RoutineIndex,
                command,
                "Scan enemy command recognized."),
            ScriptCommandType.AimAtEnemy => ScriptCommandTranslationResult.Translated(
                intent.RoutineIndex,
                command,
                "Aim at enemy command recognized."),
            ScriptCommandType.Fire => ScriptCommandTranslationResult.Translated(
                intent.RoutineIndex,
                command,
                "Fire command recognized."),
            ScriptCommandType.MoveToPatrolPoint => ScriptCommandTranslationResult.Translated(
                intent.RoutineIndex,
                command,
                "Move to patrol point command recognized."),
            ScriptCommandType.Retreat => ScriptCommandTranslationResult.Translated(
                intent.RoutineIndex,
                command,
                "Retreat command recognized."),
            _ => ScriptCommandTranslationResult.UnsupportedCommand(
                intent.RoutineIndex,
                command,
                "Unsupported script command."),
        };
    }
}
