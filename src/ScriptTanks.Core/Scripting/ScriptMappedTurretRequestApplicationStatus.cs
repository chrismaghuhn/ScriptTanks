namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcome classification for a future script-mapped turret request application attempt.
/// </summary>
/// <remarks>
/// Pure status labels only; does not resolve aim targets, compute turret rotation,
/// mutate <see cref="Tanks.TankState.TurretRotation"/>, construct or apply fire requests,
/// log, replay, or integrate Godot.
/// </remarks>
public enum ScriptMappedTurretRequestApplicationStatus
{
    SkippedNotTurret = 0,

    Applied = 1,

    RejectedInvalidTurretRequest = 2,

    TankIndexOutOfRange = 3,

    TankDestroyed = 4,

    NoTarget = 5,

    MissingAimSolution = 6,
}
