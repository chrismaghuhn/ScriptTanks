using System;
using System.Globalization;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable value object describing the intent to perform a single match-level
/// sensor scan: a tank index and a sensor slot.
/// </summary>
/// <remarks>
/// <para>
/// This type identifies a tank by zero-based index, consistent with
/// <see cref="MatchSensorRuntimeState"/> and <see cref="MatchSensorScanSystem"/>
/// index-based composition. It identifies a sensor via <see cref="SensorSlot"/>.
/// </para>
/// <para>
/// This model does not validate indices against <see cref="MatchState"/>, tank
/// counts, or sensor loadout sizes. It does not execute scans, queue work, or
/// interact with runtime scheduling.
/// </para>
/// <para>
/// Out of scope: AI or script execution, match runner or replay integration,
/// logging, diagnostics, Godot integration, and serialization.
/// </para>
/// </remarks>
public readonly struct MatchSensorScanRequest : IEquatable<MatchSensorScanRequest>
{
    public int TankIndex { get; }

    public SensorSlot SensorSlot { get; }

    public MatchSensorScanRequest(int tankIndex, SensorSlot sensorSlot)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tankIndex), tankIndex, "Tank index must not be negative.");
        }

        TankIndex = tankIndex;
        SensorSlot = sensorSlot;
    }

    public bool Equals(MatchSensorScanRequest other)
        => TankIndex == other.TankIndex && SensorSlot == other.SensorSlot;

    public override bool Equals(object? obj)
        => obj is MatchSensorScanRequest other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(TankIndex, SensorSlot);

    public static bool operator ==(MatchSensorScanRequest left, MatchSensorScanRequest right)
        => left.Equals(right);

    public static bool operator !=(MatchSensorScanRequest left, MatchSensorScanRequest right)
        => !left.Equals(right);

    public override string ToString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"MatchSensorScanRequest {{ TankIndex = {TankIndex}, SensorSlot = {SensorSlot} }}");
}
