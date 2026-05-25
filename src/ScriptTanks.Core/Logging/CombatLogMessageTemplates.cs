namespace ScriptTanks.Core.Logging;

/// <summary>
/// Canonical human-readable message templates for combat-log entries.
/// </summary>
/// <remarks>
/// Pure compile-time constants only. This type does not format, emit,
/// collect, serialize, persist, replay, or render combat-log entries;
/// it does not integrate with the runner, recorder, pipeline, UI, or
/// Godot. Placeholder values are intentionally named with braces (for
/// example <c>{TankId}</c>) and are resolved by future logging
/// integrations - this task does not provide a formatter.
/// </remarks>
public static class CombatLogMessageTemplates
{
    public const string MatchStarted = "Match started.";
    public const string MatchEnded = "Match ended.";

    public const string FireRequested = "Tank {TankId} requested fire from weapon slot {WeaponSlot}.";
    public const string FireSucceeded = "Tank {TankId} fired weapon slot {WeaponSlot}.";
    public const string FireNotReady = "Tank {TankId} could not fire weapon slot {WeaponSlot}: weapon not ready.";
    public const string FireFailed = "Tank {TankId} failed to fire weapon slot {WeaponSlot}.";

    public const string ProjectileSpawned = "Projectile {ProjectileId} spawned from tank {TankId}.";
    public const string ProjectileHit = "Projectile {ProjectileId} hit tank {TankId}.";
    public const string ProjectileExpired = "Projectile {ProjectileId} expired.";
    public const string ProjectileCleanedUp = "Projectile {ProjectileId} was cleaned up.";

    public const string DamageDealt = "Tank {TankId} took {Damage} damage.";
    public const string TankDestroyed = "Tank {TankId} was destroyed.";
}
