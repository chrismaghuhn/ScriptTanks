using System;
using System.Globalization;
using ScriptTanks.Core.Geometry;

namespace ScriptTanks.Core.Arena;

/// <summary>
/// Deterministic axis-aligned wall block defined by a stable identifier and
/// a <see cref="FixedRect"/> footprint. The struct is purely a data carrier:
/// it does not perform collision response, movement blocking, or
/// interaction with tanks, projectiles, or arenas. Validation of the
/// <see cref="Bounds"/> against an arena happens in <see cref="ArenaDefinition"/>.
/// </summary>
public readonly struct WallBlock : IEquatable<WallBlock>
{
    public string Id { get; }

    public FixedRect Bounds { get; }

    public WallBlock(string id, FixedRect bounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        Bounds = bounds;
    }

    public bool Equals(WallBlock other)
        => string.Equals(Id, other.Id, StringComparison.Ordinal)
           && Bounds.Equals(other.Bounds);

    public override bool Equals(object? obj) => obj is WallBlock other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Bounds);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "WallBlock(id={0}, bounds={1})",
            Id,
            Bounds);
}
