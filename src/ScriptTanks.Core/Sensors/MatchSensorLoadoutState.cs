using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable per-tank container of <see cref="TankSensorLoadout"/> instances.
/// Pairs with <c>MatchState.Tanks</c> by index, but does not validate against
/// <c>MatchState</c> yet.
/// </summary>
/// <remarks>
/// Pure data + replacement helper only. This type does not execute scans,
/// inspect tanks, integrate with <c>MatchState</c>, perform tank-to-sensor
/// binding by <c>TankId</c>, run AI/script logic, schedule CPU work, integrate
/// with command queues, runners, replay systems, logging, diagnostics, Godot,
/// serialization, JSON loading, or UI. Equality is reference-based.
/// </remarks>
public sealed class MatchSensorLoadoutState
{
    public IReadOnlyList<TankSensorLoadout> SensorLoadouts { get; }

    public int Count => SensorLoadouts.Count;

    public MatchSensorLoadoutState(IEnumerable<TankSensorLoadout> sensorLoadouts)
    {
        ArgumentNullException.ThrowIfNull(sensorLoadouts);

        TankSensorLoadout[] copiedLoadouts = sensorLoadouts.ToArray();

        if (copiedLoadouts.Length == 0)
        {
            throw new ArgumentException(
                "A match sensor loadout state must contain at least one tank sensor loadout.",
                nameof(sensorLoadouts));
        }

        for (int i = 0; i < copiedLoadouts.Length; i++)
        {
            if (copiedLoadouts[i] is null)
            {
                throw new ArgumentException(
                    "Match sensor loadout state must not contain null tank sensor loadouts.",
                    nameof(sensorLoadouts));
            }
        }

        SensorLoadouts = Array.AsReadOnly(copiedLoadouts);
    }

    public TankSensorLoadout GetLoadoutAtIndex(int tankIndex)
    {
        ValidateIndex(tankIndex);
        return SensorLoadouts[tankIndex];
    }

    public MatchSensorLoadoutState WithLoadoutAtIndex(
        int tankIndex,
        TankSensorLoadout sensorLoadout)
    {
        ArgumentNullException.ThrowIfNull(sensorLoadout);
        ValidateIndex(tankIndex);

        TankSensorLoadout[] copiedLoadouts = SensorLoadouts.ToArray();
        copiedLoadouts[tankIndex] = sensorLoadout;

        return new MatchSensorLoadoutState(copiedLoadouts);
    }

    private void ValidateIndex(int tankIndex)
    {
        if (tankIndex < 0 || tankIndex >= SensorLoadouts.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index is outside the sensor loadout state range.");
        }
    }
}
