using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable script routine: a labeled condition-command pair.
/// </summary>
/// <remarks>
/// Pure data model: when <see cref="Condition"/> holds, <see cref="Command"/> should run (enforced
/// only by future runtime layers). <see cref="Name"/> is an identifier or label, not parsed source.
/// This type does not evaluate conditions, execute commands, select among routines, mutate match
/// state, call sensors, fire weapons, move tanks, log, record replays, or integrate Godot.
/// </remarks>
public readonly struct ScriptRoutine : IEquatable<ScriptRoutine>
{
    public string Name { get; }

    public ScriptCondition Condition { get; }

    public ScriptCommand Command { get; }

    public ScriptRoutine(string name, ScriptCondition condition, ScriptCommand command)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Routine name must not be empty or whitespace.",
                nameof(name));
        }

        Name = name;
        Condition = condition;
        Command = command;
    }

    public bool Equals(ScriptRoutine other)
    {
        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && Condition == other.Condition
            && Command == other.Command;
    }

    public override bool Equals(object? obj)
    {
        return obj is ScriptRoutine other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(Name),
            Condition,
            Command);
    }

    public static bool operator ==(ScriptRoutine left, ScriptRoutine right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScriptRoutine left, ScriptRoutine right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptRoutine {{ Name = {Name}, Condition = {Condition}, Command = {Command} }}");
    }
}
