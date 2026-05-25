using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Pure basic-radius sensor scan resolver.
/// </summary>
/// <remarks>
/// This resolver checks cooldown and detects enemy tanks within sensor range
/// using deterministic fixed-point 2D distance. It does not apply cone angles,
/// facing, turret direction, line-of-sight, wall occlusion, raycasts, target
/// memory, AI/script behavior, match pipeline integration, replay integration,
/// logging, serialization, UI, or Godot behavior.
/// </remarks>
public static class SensorScanResolver
{
    public static SensorScanOutcome Scan(
        MatchState state,
        TankId scannerTankId,
        SensorState sensorState)
    {
        ArgumentNullException.ThrowIfNull(state);

        TankState scanner = FindScannerTank(state, scannerTankId);

        if (!sensorState.IsReady(state.CurrentTick))
        {
            SensorScanResult cooldownResult = SensorScanResult.OnCooldown(
                state.CurrentTick,
                sensorState.Definition,
                scannerTankId);
            return new SensorScanOutcome(cooldownResult, sensorState);
        }

        List<DetectedTankSnapshot> detected = new List<DetectedTankSnapshot>();
        Fixed rangeSquared =
            sensorState.Definition.Range * sensorState.Definition.Range;

        foreach (TankState candidate in state.Tanks)
        {
            if (candidate.Id == scanner.Id)
            {
                continue;
            }

            if (candidate.OwnerSlot == scanner.OwnerSlot)
            {
                continue;
            }

            if (candidate.CurrentHitPoints <= 0)
            {
                continue;
            }

            Fixed distanceSquared =
                scanner.Movement.Position.DistanceSquaredTo(candidate.Movement.Position);

            if (distanceSquared > rangeSquared)
            {
                continue;
            }

            detected.Add(new DetectedTankSnapshot(
                candidate.Id,
                candidate.OwnerSlot,
                candidate.Movement.Position,
                FloorSqrt(distanceSquared)));
        }

        SensorScanResult result = detected.Count == 0
            ? SensorScanResult.NoDetection(
                state.CurrentTick,
                sensorState.Definition,
                scannerTankId)
            : SensorScanResult.Detected(
                state.CurrentTick,
                sensorState.Definition,
                scannerTankId,
                detected);

        SensorState updatedSensorState = sensorState.MarkScanned(
            state.CurrentTick);

        return new SensorScanOutcome(result, updatedSensorState);
    }

    private static TankState FindScannerTank(
        MatchState state,
        TankId scannerTankId)
    {
        foreach (TankState tank in state.Tanks)
        {
            if (tank.Id == scannerTankId)
            {
                return tank;
            }
        }

        throw new InvalidOperationException(
            $"No scanner tank with id '{scannerTankId}' exists in the match state.");
    }

    private static Fixed FloorSqrt(Fixed value)
    {
        if (value <= Fixed.Zero)
        {
            return Fixed.Zero;
        }

        long low = 0;
        long high = System.Math.Max(value.Raw, Fixed.FromInt(1).Raw);
        long best = 0;

        while (low <= high)
        {
            long mid = low + ((high - low) / 2);
            Fixed midFixed = Fixed.FromRaw(mid);
            Fixed squared = midFixed * midFixed;

            if (squared <= value)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Fixed.FromRaw(best);
    }
}
