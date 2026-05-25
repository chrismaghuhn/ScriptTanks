using System;
using System.Globalization;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Arena;

/// <summary>
/// Deterministic spawn description that pairs a <see cref="PlayerSlot"/>
/// with a <see cref="FixedVec2"/> world position. The struct is purely a
/// data carrier: <see cref="PlayerSlot"/> already validates non-negative
/// slot values, and validation of the <see cref="Position"/> against an
/// arena happens in <see cref="ArenaDefinition"/>.
/// </summary>
public readonly struct ArenaStartPosition : IEquatable<ArenaStartPosition>
{
    public PlayerSlot Slot { get; }

    public FixedVec2 Position { get; }

    public ArenaStartPosition(PlayerSlot slot, FixedVec2 position)
    {
        Slot = slot;
        Position = position;
    }

    public bool Equals(ArenaStartPosition other)
        => Slot.Equals(other.Slot) && Position.Equals(other.Position);

    public override bool Equals(object? obj)
        => obj is ArenaStartPosition other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Slot, Position);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "ArenaStartPosition(slot={0}, position={1})",
            Slot,
            Position);
}
