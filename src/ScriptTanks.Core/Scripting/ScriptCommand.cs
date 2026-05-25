using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable script command: a resolved command type and opaque argument payload.
/// </summary>
/// <remarks>
/// Pure data model for an already-identified command, not raw source text.
/// <see cref="Argument"/> is an opaque string for future parser, compiler, or runtime layers.
/// This type does not parse, validate command semantics, execute behavior, call sensors,
/// fire weapons, move tanks, mutate match state, log, record replays, or integrate Godot.
/// </remarks>
public readonly struct ScriptCommand : IEquatable<ScriptCommand>
{
    public ScriptCommandType Type { get; }

    public string Argument { get; }

    public ScriptCommand(ScriptCommandType type, string argument)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Script command type must be a defined enum value.");
        }

        ArgumentNullException.ThrowIfNull(argument);

        Type = type;
        Argument = argument;
    }

    public static ScriptCommand NoOp()
    {
        return new ScriptCommand(
            ScriptCommandType.NoOp,
            string.Empty);
    }

    public bool Equals(ScriptCommand other)
    {
        return Type == other.Type
            && string.Equals(Argument, other.Argument, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ScriptCommand other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, StringComparer.Ordinal.GetHashCode(Argument));
    }

    public static bool operator ==(ScriptCommand left, ScriptCommand right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScriptCommand left, ScriptCommand right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptCommand {{ Type = {Type}, Argument = {Argument} }}");
    }
}
