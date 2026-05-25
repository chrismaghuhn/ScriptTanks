namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcome classification for a future script-mapped fire request construction attempt.
/// </summary>
/// <remarks>
/// Pure status labels only; does not allocate projectile ids, resolve muzzle or velocity,
/// execute fire, mutate match state, log, replay, or integrate Godot.
/// </remarks>
public enum ScriptMappedFireRequestConstructionStatus
{
    NotWeapon = 0,

    NoFireCommand = 1,

    MissingWeaponSlot = 2,

    WeaponNotReady = 3,

    TurretNotAligned = 4,

    MissingProjectileId = 5,

    MissingMuzzleResolver = 6,

    MissingVelocityResolver = 7,

    Constructed = 8,

    Unsupported = 9,
}
