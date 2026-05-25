using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Movement;

/// <summary>
/// Minimal deterministic movement state. Holds a world position and a
/// tick-native velocity. Velocity is intentionally expressed per tick
/// (not per second) so that integration is drift-free regardless of
/// <see cref="ScriptTanks.Core.Simulation.SimConstants.FixedDeltaTime"/>
/// truncation.
/// </summary>
public readonly struct MovementState : IEquatable<MovementState>
{
    public FixedVec2 Position { get; }

    public FixedVec2 VelocityPerTick { get; }

    public MovementState(FixedVec2 position, FixedVec2 velocityPerTick)
    {
        Position = position;
        VelocityPerTick = velocityPerTick;
    }

    public MovementState WithPosition(FixedVec2 position)
        => new MovementState(position, VelocityPerTick);

    public MovementState WithVelocityPerTick(FixedVec2 velocityPerTick)
        => new MovementState(Position, velocityPerTick);

    public bool Equals(MovementState other)
        => Position.Equals(other.Position)
           && VelocityPerTick.Equals(other.VelocityPerTick);

    public override bool Equals(object? obj)
        => obj is MovementState other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Position, VelocityPerTick);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "MovementState(pos={0}, vel/tick={1})",
            Position,
            VelocityPerTick);
}
