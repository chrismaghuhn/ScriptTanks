namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Script-visible classification of a tank's turret aim and alignment toward a selected target.
/// </summary>
/// <remarks>
/// Pure status labels only. Does not select targets, compute desired rotation, evaluate script
/// conditions, gate fire, mutate <see cref="Tanks.TankState"/>, log, replay, or integrate Godot.
/// </remarks>
public enum ScriptVisibleTurretAimStatus
{
    /// <summary>
    /// The owner can aim, but no valid target was found.
    /// </summary>
    NoTarget = 0,

    /// <summary>
    /// A target exists, but desired rotation cannot be resolved (for example zero direction delta).
    /// </summary>
    MissingAimSolution = 1,

    /// <summary>
    /// Current turret rotation equals desired rotation after normalization.
    /// </summary>
    Aligned = 2,

    /// <summary>
    /// A target exists, desired rotation is resolved, and the turret is not yet aligned.
    /// </summary>
    Turning = 3,

    /// <summary>
    /// The owner tank is destroyed and cannot meaningfully aim.
    /// </summary>
    OwnerDestroyed = 4,
}
