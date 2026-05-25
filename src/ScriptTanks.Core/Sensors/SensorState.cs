using System;
using System.Globalization;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable runtime state for a tank sensor module.
/// </summary>
/// <remarks>
/// Pure state data only. This type does not perform scanning, visibility
/// checks, line-of-sight checks, target memory, CPU scheduling, scripting,
/// match integration, replay integration, logging, serialization, UI, or Godot
/// behavior.
/// </remarks>
public readonly struct SensorState : IEquatable<SensorState>
{
    public SensorDefinition Definition { get; }

    public SimTick LastScanTick { get; }

    public SensorState(SensorDefinition definition, SimTick lastScanTick)
    {
        Definition = definition;
        LastScanTick = lastScanTick;
    }

    public static SensorState Ready(SensorDefinition definition)
    {
        return new SensorState(definition, SimTick.Zero);
    }

    public bool IsReady(SimTick currentTick)
    {
        return currentTick.Value - LastScanTick.Value >= Definition.CooldownTicks;
    }

    public SensorState MarkScanned(SimTick tick)
    {
        return new SensorState(Definition, tick);
    }

    public bool Equals(SensorState other)
    {
        return Definition == other.Definition
            && LastScanTick == other.LastScanTick;
    }

    public override bool Equals(object? obj)
    {
        return obj is SensorState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Definition, LastScanTick);
    }

    public static bool operator ==(SensorState left, SensorState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SensorState left, SensorState right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "SensorState(Definition={0}, LastScanTick={1})",
            Definition,
            LastScanTick);
    }
}
