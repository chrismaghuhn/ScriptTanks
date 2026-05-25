namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Status of a sensor scan result.
/// </summary>
/// <remarks>
/// Pure status value only. This enum does not perform scanning, visibility
/// checks, line-of-sight checks, target memory, CPU scheduling, scripting,
/// match integration, replay integration, logging, serialization, UI, or Godot
/// behavior.
/// </remarks>
public enum SensorScanStatus
{
    None = 0,
    NoDetection = 1,
    Detected = 2,
    SensorOnCooldown = 3,
}
