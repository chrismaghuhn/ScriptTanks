namespace ScriptTanks.Core.Logging;

/// <summary>
/// Coarse classification for combat-log events used by future replay and
/// debug tooling. Explicit integer values keep ordering stable for any
/// later persistence layer; this type itself is pure data.
/// </summary>
public enum CombatLogCategory
{
    General = 0,
    Match = 1,
    Tank = 2,
    Weapon = 3,
    Projectile = 4,
    Damage = 5,
    System = 6,
}
