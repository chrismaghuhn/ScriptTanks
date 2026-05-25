using System;
using System.Globalization;

namespace ScriptTanks.Core.Ids;

/// <summary>
/// Result of a single <see cref="ProjectileIdSequence.AllocateNext"/> operation: the allocated
/// <see cref="ProjectileId"/> and the successor sequence state.
/// </summary>
public readonly struct ProjectileIdAllocation : IEquatable<ProjectileIdAllocation>
{
    public ProjectileId ProjectileId { get; }

    public ProjectileIdSequence NextSequence { get; }

    public ProjectileIdAllocation(ProjectileId projectileId, ProjectileIdSequence nextSequence)
    {
        ProjectileId = projectileId;
        NextSequence = nextSequence;
    }

    public bool Equals(ProjectileIdAllocation other)
        => ProjectileId == other.ProjectileId && NextSequence == other.NextSequence;

    public override bool Equals(object? obj) => obj is ProjectileIdAllocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ProjectileId, NextSequence);

    public static bool operator ==(ProjectileIdAllocation left, ProjectileIdAllocation right)
        => left.Equals(right);

    public static bool operator !=(ProjectileIdAllocation left, ProjectileIdAllocation right)
        => !left.Equals(right);

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"ProjectileIdAllocation {{ ProjectileId = {ProjectileId}, NextSequence = {NextSequence} }}");
    }
}
