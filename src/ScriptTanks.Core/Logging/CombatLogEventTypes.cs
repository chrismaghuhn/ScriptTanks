namespace ScriptTanks.Core.Logging;

/// <summary>
/// Canonical event type names for combat-log entries.
/// </summary>
/// <remarks>
/// Pure compile-time constants only. This type does not emit, collect,
/// serialize, persist, replay, or render combat-log entries; it does not
/// integrate with the runner, recorder, pipeline, UI, or Godot. Values
/// are lowercase snake_case and stable - changing one is a breaking
/// change for future logging consumers.
/// </remarks>
public static class CombatLogEventTypes
{
    public const string MatchStarted = "match_started";
    public const string MatchEnded = "match_ended";

    public const string FireRequested = "fire_requested";
    public const string FireSucceeded = "fire_succeeded";
    public const string FireNotReady = "fire_not_ready";
    public const string FireFailed = "fire_failed";

    public const string ProjectileSpawned = "projectile_spawned";
    public const string ProjectileHit = "projectile_hit";
    public const string ProjectileExpired = "projectile_expired";
    public const string ProjectileCleanedUp = "projectile_cleaned_up";

    public const string DamageDealt = "damage_dealt";
    public const string TankDestroyed = "tank_destroyed";

    public const string ScriptTick = "script_tick";
    public const string FireRequestApplied = "fire_request_applied";
    public const string FireRejected = "fire_rejected";
}
