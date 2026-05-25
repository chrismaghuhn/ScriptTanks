using System;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Pure match-level sensor scan composer.
/// </summary>
/// <remarks>
/// Scans exactly one tank index with exactly one sensor slot, pairing
/// <c>runtime.State.Tanks[tankIndex]</c> with
/// <c>runtime.SensorLoadouts.GetLoadoutAtIndex(tankIndex)</c>, delegating
/// scan execution to <see cref="LoadoutSensorScanResolver.Scan(Match.MatchState, Ids.TankId, TankSensorLoadout, SensorSlot)"/>,
/// and threading the updated loadout back through
/// <see cref="MatchSensorRuntimeState.WithTankSensorLoadoutAtIndex(int, TankSensorLoadout)"/>.
///
/// Tank-index validation is owned here. Sensor-slot validation is delegated
/// to <see cref="TankSensorLoadout.GetSensor(SensorSlot)"/>. Cooldown,
/// detection filtering, and distance math remain owned by
/// <see cref="SensorScanResolver"/>. The system does not mutate
/// <see cref="Match.MatchState"/>, the original
/// <see cref="MatchSensorRuntimeState"/>, weapon loadouts, projectiles, ticks,
/// HP, movement, or arena data. It does not run AI/script logic, schedule
/// scans, integrate with command queues, CPU schedulers, runners, replay
/// systems, logging, diagnostics, serialization, UI, or Godot.
/// </remarks>
public static class MatchSensorScanSystem
{
    /// <summary>
    /// Performs a match-level sensor scan described by <paramref name="request"/>.
    /// </summary>
    /// <remarks>
    /// Convenience overload that delegates to
    /// <see cref="ScanSensorAtIndex(MatchSensorRuntimeState, int, SensorSlot)"/>
    /// using <see cref="MatchSensorScanRequest.TankIndex"/> and
    /// <see cref="MatchSensorScanRequest.SensorSlot"/>. Does not add scan
    /// scheduling, request queues, AI or script integration, logging, replay,
    /// diagnostics, or Godot integration.
    /// </remarks>
    public static MatchSensorScanOutcome Scan(
        MatchSensorRuntimeState runtime,
        MatchSensorScanRequest request)
    {
        return ScanSensorAtIndex(
            runtime,
            request.TankIndex,
            request.SensorSlot);
    }

    public static MatchSensorScanOutcome ScanSensorAtIndex(
        MatchSensorRuntimeState runtime,
        int tankIndex,
        SensorSlot sensorSlot)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ValidateTankIndex(runtime, tankIndex);

        TankState scanner = runtime.State.Tanks[tankIndex];
        TankSensorLoadout loadout = runtime.SensorLoadouts.GetLoadoutAtIndex(
            tankIndex);

        LoadoutSensorScanOutcome scanOutcome = LoadoutSensorScanResolver.Scan(
            runtime.State,
            scanner.Id,
            loadout,
            sensorSlot);

        MatchSensorRuntimeState updatedRuntime = runtime.WithTankSensorLoadoutAtIndex(
            tankIndex,
            scanOutcome.UpdatedLoadout);

        return new MatchSensorScanOutcome(scanOutcome.Result, updatedRuntime);
    }

    private static void ValidateTankIndex(
        MatchSensorRuntimeState runtime,
        int tankIndex)
    {
        if (tankIndex < 0 || tankIndex >= runtime.State.Tanks.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index is outside the match state tank range.");
        }
    }
}
