using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable result of future script command translation from <see cref="ScriptCommandIntent"/> to
/// concrete kernel-style requests.
/// </summary>
/// <remarks>
/// <see cref="NoIntent"/> indicates no command intent from script decision logic.
/// <see cref="Translated"/> means a future translator recognized the command; this model does not
/// yet store a concrete kernel request. Failure statuses represent future translation failures
/// without executing anything. This type does not execute commands, translate commands into match,
/// sensor, weapon, or movement requests, mutate match state, log, record replays, run diagnostics,
/// or integrate Godot.
/// </remarks>
public sealed class ScriptCommandTranslationResult
{
    public ScriptCommandTranslationStatus Status { get; }

    public int RoutineIndex { get; }

    public ScriptCommand? Command { get; }

    public string Message { get; }

    public bool HasTranslatedRequest =>
        Status == ScriptCommandTranslationStatus.Translated;

    private ScriptCommandTranslationResult(
        ScriptCommandTranslationStatus status,
        int routineIndex,
        ScriptCommand? command,
        string message)
    {
        if (!Enum.IsDefined(typeof(ScriptCommandTranslationStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Script command translation status must be a defined enum value.");
        }

        if (status == ScriptCommandTranslationStatus.NoIntent)
        {
            if (routineIndex != -1)
            {
                throw new ArgumentException(
                    "No-intent result requires routine index -1.",
                    nameof(routineIndex));
            }

            if (command != null)
            {
                throw new ArgumentException(
                    "No-intent result requires a null command.",
                    nameof(command));
            }

            if (message != string.Empty)
            {
                throw new ArgumentException(
                    "No-intent result requires an empty message.",
                    nameof(message));
            }
        }
        else
        {
            if (routineIndex < 0)
            {
                throw new ArgumentException(
                    "Routine index must not be negative.",
                    nameof(routineIndex));
            }

            if (command == null)
            {
                throw new ArgumentException(
                    "A non-null command is required.",
                    nameof(command));
            }

            if (message is null)
            {
                throw new ArgumentException(
                    "Message must not be null.",
                    nameof(message));
            }
        }

        Status = status;
        RoutineIndex = routineIndex;
        Command = command;
        Message = message;
    }

    public static ScriptCommandTranslationResult NoIntent()
    {
        return new ScriptCommandTranslationResult(
            ScriptCommandTranslationStatus.NoIntent,
            -1,
            null,
            string.Empty);
    }

    public static ScriptCommandTranslationResult Translated(
        int routineIndex,
        ScriptCommand command,
        string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (routineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index must not be negative.");
        }

        return new ScriptCommandTranslationResult(
            ScriptCommandTranslationStatus.Translated,
            routineIndex,
            command,
            message);
    }

    public static ScriptCommandTranslationResult UnsupportedCommand(
        int routineIndex,
        ScriptCommand command,
        string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (routineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index must not be negative.");
        }

        return new ScriptCommandTranslationResult(
            ScriptCommandTranslationStatus.UnsupportedCommand,
            routineIndex,
            command,
            message);
    }

    public static ScriptCommandTranslationResult InvalidCommandArgument(
        int routineIndex,
        ScriptCommand command,
        string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (routineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index must not be negative.");
        }

        return new ScriptCommandTranslationResult(
            ScriptCommandTranslationStatus.InvalidCommandArgument,
            routineIndex,
            command,
            message);
    }

    public static ScriptCommandTranslationResult MissingHardware(
        int routineIndex,
        ScriptCommand command,
        string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (routineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index must not be negative.");
        }

        return new ScriptCommandTranslationResult(
            ScriptCommandTranslationStatus.MissingHardware,
            routineIndex,
            command,
            message);
    }
}
