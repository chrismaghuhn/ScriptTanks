using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Pure adapter that scans with one sensor selected from a tank sensor loadout.
/// </summary>
/// <remarks>
/// This adapter only composes existing sensor primitives. It reads a
/// <see cref="SensorState"/> from the supplied <see cref="TankSensorLoadout"/>,
/// delegates execution to <see cref="SensorScanResolver.Scan(MatchState, TankId, SensorState)"/>,
/// and writes the returned updated sensor back into a new loadout via
/// <see cref="TankSensorLoadout.WithSensor(SensorSlot, SensorState)"/>.
///
/// Slot validation is delegated to the loadout. Missing scanner tanks,
/// cooldown handling, detection filtering, and distance math remain owned
/// by <see cref="SensorScanResolver"/>. The adapter does not perform any
/// custom scan logic, target selection, prioritization, cone-angle, or
/// line-of-sight work, and does not integrate with match pipelines, runners,
/// replay systems, logging, diagnostics, AI/script runtimes, command queues,
/// CPU schedulers, serialization, UI, or Godot.
/// </remarks>
public static class LoadoutSensorScanResolver
{
    public static LoadoutSensorScanOutcome Scan(
        MatchState state,
        TankId scannerTankId,
        TankSensorLoadout loadout,
        SensorSlot sensorSlot)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(loadout);

        SensorState sensor = loadout.GetSensor(sensorSlot);
        SensorScanOutcome scanOutcome = SensorScanResolver.Scan(
            state,
            scannerTankId,
            sensor);
        TankSensorLoadout updatedLoadout = loadout.WithSensor(
            sensorSlot,
            scanOutcome.UpdatedSensorState);

        return new LoadoutSensorScanOutcome(scanOutcome.Result, updatedLoadout);
    }
}
