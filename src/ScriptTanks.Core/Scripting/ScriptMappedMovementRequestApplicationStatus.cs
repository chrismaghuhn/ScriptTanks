namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcome classification for a future script-mapped movement request application attempt.
/// </summary>
/// <remarks>
/// Pure status labels only; does not derive velocity, call movement integrators,
/// mutate <see cref="Tanks.TankState.Movement"/>, log, replay, or integrate Godot.
/// </remarks>
public enum ScriptMappedMovementRequestApplicationStatus
{
    SkippedNotMovement = 0,

    Applied = 1,

    RejectedInvalidMovement = 2,

    TankIndexOutOfRange = 3,

    TankDestroyed = 4,
}
