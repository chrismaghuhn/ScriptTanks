using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure v2 script command translator that maps <see cref="ScriptCommandIntent"/> to
/// <see cref="ScriptCommandTranslationOutput"/>.
/// </summary>
/// <remarks>
/// Produces a <see cref="ScriptCommandTranslationResult"/> plus <see cref="ScriptTranslatedCommandRequest"/> with
/// deterministic opaque payloads for future integration layers. Does not execute commands, dispatch requests, access or
/// mutate match state, run sensors, fire weapons, move tanks, log, diagnose, or integrate Godot.
/// </remarks>
public static class ScriptCommandTranslatorV2
{
    public static ScriptCommandTranslationOutput Translate(
        ScriptCommandIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (!intent.HasIntent)
        {
            return new ScriptCommandTranslationOutput(
                ScriptCommandTranslationResult.NoIntent(),
                ScriptTranslatedCommandRequest.None());
        }

        ScriptCommand command = intent.Command!.Value;

        return command.Type switch
        {
            ScriptCommandType.NoOp => CreateTranslatedOutput(
                intent.RoutineIndex,
                command,
                ScriptTranslatedCommandRequestKind.NoOp,
                string.Empty,
                "No operation command translated."),
            ScriptCommandType.ScanEnemy => CreateTranslatedOutput(
                intent.RoutineIndex,
                command,
                ScriptTranslatedCommandRequestKind.ScanEnemy,
                "default",
                "Scan enemy command translated."),
            ScriptCommandType.AimAtEnemy => CreateTranslatedOutput(
                intent.RoutineIndex,
                command,
                ScriptTranslatedCommandRequestKind.AimAtEnemy,
                "nearest_visible",
                "Aim at enemy command translated."),
            ScriptCommandType.Fire => CreateTranslatedOutput(
                intent.RoutineIndex,
                command,
                ScriptTranslatedCommandRequestKind.Fire,
                "default",
                "Fire command translated."),
            ScriptCommandType.MoveToPatrolPoint => CreateTranslatedOutput(
                intent.RoutineIndex,
                command,
                ScriptTranslatedCommandRequestKind.MoveToPatrolPoint,
                ResolveMoveToPatrolPointPayload(command),
                "Move to patrol point command translated."),
            ScriptCommandType.Retreat => CreateTranslatedOutput(
                intent.RoutineIndex,
                command,
                ScriptTranslatedCommandRequestKind.Retreat,
                "away_from_nearest_visible",
                "Retreat command translated."),
            _ => new ScriptCommandTranslationOutput(
                ScriptCommandTranslationResult.UnsupportedCommand(
                    intent.RoutineIndex,
                    command,
                    "Unsupported script command."),
                ScriptTranslatedCommandRequest.None()),
        };
    }

    private static string ResolveMoveToPatrolPointPayload(
        ScriptCommand command)
    {
        return command.Argument.Length == 0
            ? "next"
            : command.Argument;
    }

    private static ScriptCommandTranslationOutput CreateTranslatedOutput(
        int routineIndex,
        ScriptCommand command,
        ScriptTranslatedCommandRequestKind kind,
        string payload,
        string message)
    {
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex,
                command,
                message);

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                kind,
                routineIndex,
                command,
                payload);

        return new ScriptCommandTranslationOutput(
            result,
            request);
    }
}
