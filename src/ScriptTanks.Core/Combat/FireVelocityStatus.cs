namespace ScriptTanks.Core.Combat;

/// <summary>
/// Outcome classification for a future deterministic fire velocity resolution attempt.
/// </summary>
/// <remarks>
/// Pure status labels only. This enum does not read tank state, compute aim direction,
/// scale projectile speed, execute fire, or mutate match state.
/// </remarks>
public enum FireVelocityStatus
{
    /// <summary>
    /// A deterministic fire velocity was resolved.
    /// </summary>
    Resolved = 0,

    /// <summary>
    /// The requested tank index cannot be paired with tanks or loadouts.
    /// </summary>
    TankIndexOutOfRange = 1,

    /// <summary>
    /// The tank exists but cannot fire because it is destroyed.
    /// </summary>
    TankDestroyed = 2,

    /// <summary>
    /// The requested weapon slot does not exist for this tank loadout.
    /// </summary>
    WeaponSlotMissing = 3,

    /// <summary>
    /// A deterministic forward direction could not be derived from turret aim state.
    /// </summary>
    MissingAimDirection = 4,

    /// <summary>
    /// Forward direction is available but weapon projectile speed is missing or invalid.
    /// </summary>
    MissingWeaponSpeed = 5,
}
