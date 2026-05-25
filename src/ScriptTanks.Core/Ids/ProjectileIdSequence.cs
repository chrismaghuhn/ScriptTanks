using System;
using System.Globalization;

namespace ScriptTanks.Core.Ids;

/// <summary>
/// Immutable deterministic state for allocating <see cref="ProjectileId"/> values from a
/// monotonic integer sequence. Allocation returns a new sequence value object; the original
/// instance is never mutated.
/// </summary>
/// <remarks>
/// Pure allocation bookkeeping only. This type does not construct fire requests, spawn
/// projectiles, mutate match state, or use randomness, wall-clock time, or global counters.
/// </remarks>
public readonly struct ProjectileIdSequence : IEquatable<ProjectileIdSequence>
{
    public int NextValue { get; }

    public ProjectileIdSequence(int nextValue)
    {
        if (nextValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextValue),
                nextValue,
                "ProjectileId sequence next value must not be negative.");
        }

        NextValue = nextValue;
    }

    /// <summary>
    /// Returns the <see cref="ProjectileId"/> that the next allocation would use, without
    /// advancing the sequence.
    /// </summary>
    public ProjectileId Peek()
    {
        return new ProjectileId(NextValue);
    }

    /// <summary>
    /// Allocates the current <see cref="ProjectileId"/> and returns the next sequence state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The sequence is at <see cref="int.MaxValue"/> and cannot advance safely.
    /// </exception>
    public ProjectileIdAllocation AllocateNext()
    {
        if (NextValue == int.MaxValue)
        {
            throw new InvalidOperationException(
                "Cannot allocate another ProjectileId: the sequence has reached int.MaxValue.");
        }

        return new ProjectileIdAllocation(
            new ProjectileId(NextValue),
            new ProjectileIdSequence(NextValue + 1));
    }

    public bool Equals(ProjectileIdSequence other) => NextValue == other.NextValue;

    public override bool Equals(object? obj) => obj is ProjectileIdSequence other && Equals(other);

    public override int GetHashCode() => NextValue.GetHashCode();

    public static bool operator ==(ProjectileIdSequence left, ProjectileIdSequence right)
        => left.Equals(right);

    public static bool operator !=(ProjectileIdSequence left, ProjectileIdSequence right)
        => !left.Equals(right);

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"ProjectileIdSequence {{ NextValue = {NextValue.ToString("D", CultureInfo.InvariantCulture)} }}");
    }
}
