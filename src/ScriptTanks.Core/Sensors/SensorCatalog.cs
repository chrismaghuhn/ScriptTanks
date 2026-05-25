using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Built-in sensor definitions for deterministic tests and MVP combat setup.
/// </summary>
/// <remarks>
/// Pure catalog only. This type does not perform scanning, load external files,
/// serialize data, integrate with scripting, match systems, replay systems,
/// logging, UI, or Godot.
/// </remarks>
public static class SensorCatalog
{
    public static SensorDefinition BasicRadar { get; } = new SensorDefinition(
        id: "basic_radar",
        displayName: "Basic Radar",
        range: Fixed.FromInt(35),
        coneAngleDegrees: Fixed.FromInt(90),
        cooldownTicks: 5,
        cpuCost: 3);

    public static SensorDefinition WideScanner { get; } = new SensorDefinition(
        id: "wide_scanner",
        displayName: "Wide Scanner",
        range: Fixed.FromInt(25),
        coneAngleDegrees: Fixed.FromInt(180),
        cooldownTicks: 8,
        cpuCost: 5);

    public static IReadOnlyList<SensorDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            BasicRadar,
            WideScanner,
        });

    public static SensorDefinition GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        foreach (SensorDefinition definition in All)
        {
            if (string.Equals(definition.Id, id, StringComparison.Ordinal))
            {
                return definition;
            }
        }

        throw new KeyNotFoundException(
            $"No built-in sensor definition with id '{id}' exists.");
    }
}
