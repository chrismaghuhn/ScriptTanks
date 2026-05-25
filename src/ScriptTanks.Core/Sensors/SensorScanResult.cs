using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable result of an already-computed sensor scan.
/// </summary>
/// <remarks>
/// Pure result data only. This type does not perform scanning, target
/// selection, distance calculation, visibility checks, line-of-sight checks,
/// target memory, CPU scheduling, scripting, match integration, replay
/// integration, logging, serialization, UI, or Godot behavior.
/// </remarks>
public sealed class SensorScanResult
{
    public SensorScanStatus Status { get; }

    public SimTick Tick { get; }

    public SensorDefinition Sensor { get; }

    public TankId ScannerTankId { get; }

    public IReadOnlyList<DetectedTankSnapshot> DetectedTanks { get; }

    public bool HasDetections => DetectedTanks.Count > 0;

    public SensorScanResult(
        SensorScanStatus status,
        SimTick tick,
        SensorDefinition sensor,
        TankId scannerTankId,
        IEnumerable<DetectedTankSnapshot> detectedTanks)
    {
        ArgumentNullException.ThrowIfNull(detectedTanks);

        DetectedTankSnapshot[] copiedDetectedTanks = detectedTanks.ToArray();

        ValidateStatus(status, copiedDetectedTanks.Length);

        Status = status;
        Tick = tick;
        Sensor = sensor;
        ScannerTankId = scannerTankId;
        DetectedTanks = Array.AsReadOnly(copiedDetectedTanks);
    }

    public static SensorScanResult NoDetection(
        SimTick tick,
        SensorDefinition sensor,
        TankId scannerTankId)
    {
        return new SensorScanResult(
            SensorScanStatus.NoDetection,
            tick,
            sensor,
            scannerTankId,
            Array.Empty<DetectedTankSnapshot>());
    }

    public static SensorScanResult OnCooldown(
        SimTick tick,
        SensorDefinition sensor,
        TankId scannerTankId)
    {
        return new SensorScanResult(
            SensorScanStatus.SensorOnCooldown,
            tick,
            sensor,
            scannerTankId,
            Array.Empty<DetectedTankSnapshot>());
    }

    public static SensorScanResult Detected(
        SimTick tick,
        SensorDefinition sensor,
        TankId scannerTankId,
        IEnumerable<DetectedTankSnapshot> detectedTanks)
    {
        return new SensorScanResult(
            SensorScanStatus.Detected,
            tick,
            sensor,
            scannerTankId,
            detectedTanks);
    }

    private static void ValidateStatus(
        SensorScanStatus status,
        int detectedTankCount)
    {
        switch (status)
        {
            case SensorScanStatus.NoDetection:
            case SensorScanStatus.SensorOnCooldown:
                if (detectedTankCount != 0)
                {
                    throw new ArgumentException(
                        "This sensor scan status must not contain detected tanks.",
                        nameof(status));
                }

                return;

            case SensorScanStatus.Detected:
                if (detectedTankCount == 0)
                {
                    throw new ArgumentException(
                        "Detected sensor scan results must contain at least one detected tank.",
                        nameof(status));
                }

                return;

            case SensorScanStatus.None:
            default:
                throw new ArgumentException(
                    "Sensor scan status must be a valid non-none result status.",
                    nameof(status));
        }
    }
}
