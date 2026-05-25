using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable script condition: a resolved condition type and opaque argument payload.
/// </summary>
/// <remarks>
/// Pure data model for an already-identified condition, not raw source text.
/// <see cref="Argument"/> is an opaque string for future parser, compiler, or runtime layers.
/// This type does not parse, validate condition semantics, evaluate behavior, call sensors,
/// inspect enemies, inspect HP, check weapons, mutate match state, log, record replays,
/// or integrate Godot.
/// </remarks>
public readonly struct ScriptCondition : IEquatable<ScriptCondition>
{
    public ScriptConditionType Type { get; }

    public string Argument { get; }

    public ScriptCondition(ScriptConditionType type, string argument)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Script condition type must be a defined enum value.");
        }

        ArgumentNullException.ThrowIfNull(argument);

        Type = type;
        Argument = argument;
    }

    public static ScriptCondition Always()
    {
        return new ScriptCondition(
            ScriptConditionType.Always,
            string.Empty);
    }

    public bool Equals(ScriptCondition other)
    {
        return Type == other.Type
            && string.Equals(Argument, other.Argument, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ScriptCondition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, StringComparer.Ordinal.GetHashCode(Argument));
    }

    public static bool operator ==(ScriptCondition left, ScriptCondition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScriptCondition left, ScriptCondition right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptCondition {{ Type = {Type}, Argument = {Argument} }}");
    }
}
