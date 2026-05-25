using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable definition of a tank sensor module.
/// </summary>
/// <remarks>
/// Pure definition data only. This type does not perform scanning, visibility
/// checks, line-of-sight checks, target memory, CPU scheduling, scripting,
/// match integration, replay integration, logging, serialization, UI, or Godot
/// behavior.
/// </remarks>
public readonly struct SensorDefinition : IEquatable<SensorDefinition>
{
    public string Id { get; }

    public string DisplayName { get; }

    public Fixed Range { get; }

    public Fixed ConeAngleDegrees { get; }

    public int CooldownTicks { get; }

    public int CpuCost { get; }

    public SensorDefinition(
        string id,
        string displayName,
        Fixed range,
        Fixed coneAngleDegrees,
        int cooldownTicks,
        int cpuCost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (range <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range),
                range,
                "Sensor range must be greater than zero.");
        }

        if (coneAngleDegrees <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coneAngleDegrees),
                coneAngleDegrees,
                "Sensor cone angle must be greater than zero.");
        }

        if (coneAngleDegrees > Fixed.FromInt(360))
        {
            throw new ArgumentOutOfRangeException(
                nameof(coneAngleDegrees),
                coneAngleDegrees,
                "Sensor cone angle must not exceed 360 degrees.");
        }

        if (cooldownTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cooldownTicks),
                cooldownTicks,
                "Sensor cooldown ticks must not be negative.");
        }

        if (cpuCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cpuCost),
                cpuCost,
                "Sensor CPU cost must not be negative.");
        }

        Id = id;
        DisplayName = displayName;
        Range = range;
        ConeAngleDegrees = coneAngleDegrees;
        CooldownTicks = cooldownTicks;
        CpuCost = cpuCost;
    }

    public bool Equals(SensorDefinition other)
    {
        return string.Equals(Id, other.Id, StringComparison.Ordinal)
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && Range == other.Range
            && ConeAngleDegrees == other.ConeAngleDegrees
            && CooldownTicks == other.CooldownTicks
            && CpuCost == other.CpuCost;
    }

    public override bool Equals(object? obj)
    {
        return obj is SensorDefinition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(Id),
            StringComparer.Ordinal.GetHashCode(DisplayName),
            Range,
            ConeAngleDegrees,
            CooldownTicks,
            CpuCost);
    }

    public static bool operator ==(SensorDefinition left, SensorDefinition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SensorDefinition left, SensorDefinition right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "SensorDefinition(Id={0}, DisplayName={1}, Range={2}, ConeAngleDegrees={3}, CooldownTicks={4}, CpuCost={5})",
            Id,
            DisplayName,
            Range,
            ConeAngleDegrees,
            CooldownTicks,
            CpuCost);
    }
}
