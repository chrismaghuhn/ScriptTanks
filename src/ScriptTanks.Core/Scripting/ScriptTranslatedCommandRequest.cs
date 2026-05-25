using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable carrier for a future translated script command request after translation, without execution.
/// </summary>
/// <remarks>
/// <see cref="None"/> represents no translated request. <see cref="Create"/> builds a classified request with an
/// opaque <see cref="Payload"/> for later layers; it does not parse payloads, dispatch requests, mutate match
/// state, execute sensors, weapons, or movement, or integrate logging, diagnostics, or Godot.
/// </remarks>
public sealed class ScriptTranslatedCommandRequest
{
    public ScriptTranslatedCommandRequestKind Kind { get; }

    public int RoutineIndex { get; }

    public ScriptCommand? Command { get; }

    public string Payload { get; }

    public bool HasRequest => Kind != ScriptTranslatedCommandRequestKind.None;

    private ScriptTranslatedCommandRequest(
        ScriptTranslatedCommandRequestKind kind,
        int routineIndex,
        ScriptCommand? command,
        string payload)
    {
        if (!Enum.IsDefined(typeof(ScriptTranslatedCommandRequestKind), kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Translated command request kind must be a defined enum value.");
        }

        if (kind == ScriptTranslatedCommandRequestKind.None)
        {
            if (routineIndex != -1)
            {
                throw new ArgumentException(
                    "None request must use routine index -1.",
                    nameof(routineIndex));
            }

            if (command != null)
            {
                throw new ArgumentException(
                    "None request must not carry a command.",
                    nameof(command));
            }

            if (payload != string.Empty)
            {
                throw new ArgumentException(
                    "None request must use an empty payload.",
                    nameof(payload));
            }
        }
        else
        {
            if (routineIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(routineIndex),
                    routineIndex,
                    "Routine index must not be negative.");
            }

            if (command == null)
            {
                throw new ArgumentException(
                    "Translated request must carry a command.",
                    nameof(command));
            }

            ArgumentNullException.ThrowIfNull(payload);
        }

        Kind = kind;
        RoutineIndex = routineIndex;
        Command = command;
        Payload = payload;
    }

    public static ScriptTranslatedCommandRequest None()
    {
        return new ScriptTranslatedCommandRequest(
            ScriptTranslatedCommandRequestKind.None,
            -1,
            null,
            string.Empty);
    }

    public static ScriptTranslatedCommandRequest Create(
        ScriptTranslatedCommandRequestKind kind,
        int routineIndex,
        ScriptCommand command,
        string payload)
    {
        if (kind == ScriptTranslatedCommandRequestKind.None)
        {
            throw new ArgumentException(
                "Use None() for an empty translated command request.",
                nameof(kind));
        }

        if (!Enum.IsDefined(typeof(ScriptTranslatedCommandRequestKind), kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Translated command request kind must be a defined enum value.");
        }

        if (routineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(routineIndex),
                routineIndex,
                "Routine index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(payload);

        return new ScriptTranslatedCommandRequest(
            kind,
            routineIndex,
            command,
            payload);
    }
}
