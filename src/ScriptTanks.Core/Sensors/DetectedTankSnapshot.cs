using System;
using System.Globalization;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable sensor-visible snapshot of a detected tank.
/// </summary>
/// <remarks>
/// Pure snapshot data only. This type does not perform scanning, distance
/// calculation, visibility checks, line-of-sight checks, target memory,
/// scripting, match integration, replay integration, logging, serialization,
/// UI, or Godot behavior.
/// </remarks>
public readonly struct DetectedTankSnapshot : IEquatable<DetectedTankSnapshot>
{
    public TankId TankId { get; }

    public PlayerSlot OwnerSlot { get; }

    public FixedVec2 Position { get; }

    public Fixed Distance { get; }

    public DetectedTankSnapshot(
        TankId tankId,
        PlayerSlot ownerSlot,
        FixedVec2 position,
        Fixed distance)
    {
        if (distance < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distance),
                distance,
                "Detected tank distance must not be negative.");
        }

        TankId = tankId;
        OwnerSlot = ownerSlot;
        Position = position;
        Distance = distance;
    }

    public bool Equals(DetectedTankSnapshot other)
    {
        return TankId == other.TankId
            && OwnerSlot == other.OwnerSlot
            && Position == other.Position
            && Distance == other.Distance;
    }

    public override bool Equals(object? obj)
    {
        return obj is DetectedTankSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TankId, OwnerSlot, Position, Distance);
    }

    public static bool operator ==(
        DetectedTankSnapshot left,
        DetectedTankSnapshot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        DetectedTankSnapshot left,
        DetectedTankSnapshot right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "DetectedTankSnapshot(TankId={0}, OwnerSlot={1}, Position={2}, Distance={3})",
            TankId,
            OwnerSlot,
            Position,
            Distance);
    }
}
